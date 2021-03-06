using System;
using System.Collections.Generic;
using UnityEngine;
using BattleRoyalRhythm.Audio;
using BattleRoyalRhythm.Input;
using BattleRoyalRhythm.UI;
using BattleRoyalRhythm.Collections;

namespace BattleRoyalRhythm.GridActors.Player
{

    public sealed class BeatAction
    {
        public BeatAction(int duration)
        {
            Path = null;
            Duration = duration;
        }

        public BeatAction(ActorAnimationPath path, int duration)
        {
            Path = path;
            Duration = duration;
        }

        public int Duration { get; }
        public ActorAnimationPath Path { get; }
    }


    /// <summary>
    /// The actor that controls the main player character.
    /// </summary>
    public sealed class PlayerActor : GridActor, IDamageable, IKnockbackable
    {
        #region Inspector Data Structures
        // TODO remove tight coupling to Wwise here.
        [Serializable]
        private sealed class GenreAbilityPair
        {
            [SerializeField] public string wwiseGenreTarget = "Wwise Target";
            [SerializeField] public ActorAbility ability = null;
        }
        #endregion
        #region Player State Structure
        // TODO player state enums maybe should be abstracted
        // into a generic FSM or Behaviour Tree.
        private enum ActionContext
        {
            Standing,
            Ducking,
            Hanging,
            Airborne
        }
        private enum ActionState
        {
            Idle,
            Walking,
            Jumping,
            Falling,
            DroppingToHang,
            PullingUp,
            DuckingDown,
            UnDucking,
            Attacking,
            TakingDamage
        }
        #endregion

        [Tooltip("The tile height of the actor while ducking.")]
        [SerializeField][Min(1)] private int duckingTileHeight = 1;

        [Header("Actor Health")]
        [Tooltip("The maximum health of the player.")]
        [SerializeField][Min(0f)] private float maxHealth = 100f;
        [Tooltip("The current health of the player.")]
        [SerializeField][Min(0f)] private float health = 100f;

        [Header("Player Input")]
        [SerializeField] private PlayerController controller = null;
        [SerializeField][Min(0f)] private float inputTolerance = 0.1f;


        [Header("Player Camera")]
        [Tooltip("A transform that follows the player containing a child transform with the camera.")]
        [SerializeField] private Transform cameraPivot = null;
        [Tooltip("The speed that the camera rotates when rounding corners.")]
        [SerializeField][Min(0f)] private float cameraDegreesPerSecond = 5f;

        [Tooltip("Jump height apex in tiles.")]
        [SerializeField][Min(1)] private int jumpApex = 2;


        [SerializeField] private Transform meshContainer = null;
        [SerializeField] private Animator animator = null;
        [SerializeField] private EnumMap<ActionState, int> actionDurations = null;
        [SerializeField] private AnimatorState<ActionState> moveModeAnimator = null;
        [SerializeField] private AnimatorState<ActionContext> moveContextAnimator = null;
        [SerializeField][Min(0f)] private float pivotDegreesPerBeat = 180f;

        [SerializeField][SoundtrackID] private int soundtrackSet = 0;
        [SerializeField] private GenreAbilityPair[] genres = null;

        [Header("Automatic Actions")]
        [Tooltip("The minimum number of tiles the player can jump along the x-axis.")]
        [SerializeField][Min(2)] private int minJumpX = 2;
        [Tooltip("The maximum number of tiles the player can jump along the x-axis.")]
        [SerializeField][Min(2)] private int maxJumpX = 3;
        [Tooltip("The maximum number of tiles the player will drop from.")]
        [SerializeField][Min(0)] private int maxDropDistance = 4;
        [SerializeField][Min(0)] private int maxPullupHeight = 3;
        [SerializeField][Min(0)] private int autoStepHeight = 1;

        [SerializeField] private Transform animationSnapHintTransform = null;

        [SerializeField] private bool inBossMode = false;
        [SerializeField] private BeatTimelineControl beatTimeline = null;

        private Vector3 lastFrameHintPosition;
        private int unDuckedHeight;
        private float targetYAxisDegrees;
        private int activeGenre;
        private bool wasInputLastBeat;
        private bool abilityInUse;
        private Vector2 lastAnimationFrame;
        private Vector3 snapBeatLocation;
        private Queue<BeatAction> queuedActions;
        private ActionState nextState;
        private ActionState stateThisFrame;
        private int currentActionProgress;

        public event Action<float> BeatEarly;
        public event Action<float> BeatLate;
        public event Action<PlayerAction, int> ActionExecuted;

        private enum SnapState
        {
            None,
            AwaitingForwardSnap,
            AwaitingBackSnap
        }

        private SnapState animationSnapState;


        protected override sealed void OnDirectionChanged(Direction direction)
        {
            switch (direction)
            {
                case Direction.Left: targetYAxisDegrees = 180f; break;
                case Direction.Right: targetYAxisDegrees = 0f; break;
            }
        }

        public float Health => health;

        public float MaxHealth => maxHealth;

        public int[] GetCurrentActionGaps(int numberOfBeats)
        {
            // TODO this code is not optimized.
            List<int> gaps = new List<int>();
            bool isFirstAction = true;
            int beatsAccounted = 0;
            foreach (BeatAction action in queuedActions)
            {
                if (isFirstAction)
                {
                    beatsAccounted += action.Duration - currentActionProgress;
                    gaps.Add(action.Duration - currentActionProgress);
                    isFirstAction = false;
                }
                else
                {
                    beatsAccounted += action.Duration;
                    gaps.Add(action.Duration);
                }
            }
            while (beatsAccounted < numberOfBeats)
            {
                beatsAccounted += actionDurations[ActionState.Idle];
                gaps.Add(actionDurations[ActionState.Idle]);
            }
            return gaps.ToArray();
        }


        protected override void OnValidate()
        {
            base.OnValidate();

        }

        private void Start()
        {

            if (Application.isPlaying)
            {
                unDuckedHeight = TileHeight;

                activeGenre = 0;
                queuedActions = new Queue<BeatAction>();
                World.BeatService.BeatOffset = -inputTolerance * 0.5f;
                World.BeatService.BeatElapsed += OnBeatElapsed;
                SoundtrackSet levelSet = SoundtrackSettings.Load().GetSetByID(soundtrackSet);
                World.BeatService.SetBeatSoundtrack(levelSet);


                // Set the animator such that 60 frames (1 second)
                // elapses in one beat.
                animator.speed = levelSet.BeatsPerMinute / 60f;

                moveModeAnimator.State = ActionState.Idle;
                moveContextAnimator.State = ActionContext.Standing;
                nextState = ActionState.Idle;

                if (genres != null)
                    foreach (GenreAbilityPair pair in genres)
                        if (pair.ability != null)
                            pair.ability.UsingActor = this;
            }
        }

        protected override sealed void OnEnable()
        {
            base.OnEnable();
            Direction = Direction.Right;
        }

        private void OnBeatElapsed(float beatTime)
        {
            // Finalize the prior animation if there was one.
            if (queuedActions.Count > 0)
            {
                BeatAction currentAction = queuedActions.Peek();
                currentActionProgress++;
                if (currentActionProgress >= currentAction.Duration)
                {
                    if (currentAction.Path != null)
                    {
                        Vector2 toLocation = currentAction.Path(1f);
                        // Apply the translation to the actor.
                        World.TranslateActor(this, toLocation - lastAnimationFrame);
                        lastAnimationFrame = Vector2.zero;
                    }
                    queuedActions.Dequeue();
                    currentActionProgress = 0;
                }
            }
            // HOTFIX; this allows longer actions to take more beats.
            if (currentActionProgress > 0)
                return;

            animator.SetTrigger("Beat Elapsed");

            stateThisFrame = nextState;
            meshContainer.position = transform.position;


            // Query the world for the surrounding colliders.
            // These will be used for movement logic.
            NearbyColliderSet colliders = World.GetNearbyColliders(this, 9, 9);

            snapBeatLocation = transform.position;
            animationSnapState = SnapState.None;
            lastAnimationFrame = Vector2.zero;

            bool movementOverriden = false;
            if (abilityInUse)
            {
                // Ability use has completed.
                if (!genres[activeGenre].ability.InUse)
                    abilityInUse = false;
                else
                {
                    BeatAction action = genres[activeGenre].ability.ElapseBeat();
                    if (action != null)
                    {
                        queuedActions.Enqueue(action);
                        movementOverriden = true;
                    }
                }
            }

            int directionStep = Direction is Direction.Right ? 1 : -1;

            if (!movementOverriden)
            {

                // React to the latest input if it has
                // been timed well enough.
                float beatDelta = controller.LatestTimestamp - beatTime;
                if (inBossMode)
                {
                    if (Mathf.Abs(beatDelta) < inputTolerance)
                        ActionExecuted?.Invoke(controller.LatestAction, 1);
                    else
                        ActionExecuted?.Invoke(PlayerAction.None, 1);
                }
                else if (Mathf.Abs(beatDelta) < inputTolerance)
                {
                    switch (controller.LatestAction)
                    {
                        case PlayerAction.MoveLeft:
                            directionStep = -1;
                            ProcessHorizontalMove(); break;
                        case PlayerAction.MoveRight:
                            directionStep = 1;
                            ProcessHorizontalMove(); break;
                        case PlayerAction.Jump: HandleJumpStateChange(); break;
                        case PlayerAction.Duck: ProcessDuck(); break;
                        case PlayerAction.UseAbility: ProcessUseAbility(); break;
                        case PlayerAction.Attack: ExecuteAttack(); break;
                        case PlayerAction.SetGenre1: ProcessSetGenre(0); break;
                        case PlayerAction.SetGenre2: ProcessSetGenre(1); break;
                        case PlayerAction.SetGenre3: ProcessSetGenre(2); break;
                        case PlayerAction.SetGenre4: ProcessSetGenre(3); break;
                    }
                    if (queuedActions.Count != 0)
                    {
                        ActionExecuted?.Invoke(controller.LatestAction, queuedActions.Peek().Duration);
                    }
                    wasInputLastBeat = true;
                }
                else
                {
                    if (wasInputLastBeat)
                    {
                        if (beatDelta > 0f)
                            BeatLate?.Invoke(beatDelta);
                        else
                            BeatEarly?.Invoke(beatDelta);
                        wasInputLastBeat = false;
                    }
                }

            }

            moveModeAnimator.State = stateThisFrame;
            if (stateThisFrame is ActionState.Falling)
            {
                moveContextAnimator.State = ActionContext.Standing;
                nextState = ActionState.Idle;
            }

            void HandleJumpStateChange()
            {
                switch (moveContextAnimator.State)
                {
                    case ActionContext.Standing:
                        switch (stateThisFrame)
                        {
                            case ActionState.Idle:
                            case ActionState.Walking:
                            case ActionState.DuckingDown:
                                if (World.TryTurnForwards(this)) break;
                                if (CanJumpUp()) { ExecuteJumpUp(); break; }
                                break;
                        }
                        break;
                    case ActionContext.Hanging:
                        if (CanPullUp()) { ExecutePullUp(); break; }
                        break;
                }
            }
            void ProcessDuck()
            {
                switch (moveContextAnimator.State)
                {
                    case ActionContext.Ducking:
                        switch (stateThisFrame)
                        {
                            case ActionState.Idle:
                                if (CanUnDuck()) { ExecuteUnDuck(); break; }
                                break;
                        }
                        break;
                    case ActionContext.Standing:
                        switch (stateThisFrame)
                        {
                            case ActionState.Idle:
                                if (CanHangDown()) { ExecuteHangDown(); break; }
                                ExecuteDuck(); break;
                        }
                        break;
                    case ActionContext.Hanging:
                        if (CanDropFromGrab(out int height)) { ExecuteDropFromGrab(height); break; }
                        break;
                }
            }
            void ProcessHorizontalMove()
            {
                switch (moveContextAnimator.State)
                {
                    case ActionContext.Standing:
                        if (CanWalk()) { ExecuteWalk(); break; }
                        if (CanStep(out int height)) { ExecuteStep(height); break; }
                        if (TryJump()) { break; }
                        if (CanPullUp()) { ExecutePullUp(); break; }
                        if (CanJumpGrab(out int height2)) { ExecuteJumpGrab(height2); break; }
                        if (CanHangDown()) { ExecuteHangDown(); break; }
                        if (CanJumpUp()) { ExecuteJumpUp(); break; }
                        break;
                    case ActionContext.Airborne:
                        if (CanStep(out int height3)) { ExecuteStep(height3); break; }
                        if (CanPullUp()) { ExecutePullUp(); break; }
                        break;
                    case ActionContext.Hanging:
                        if (CanPullUp()) { ExecutePullUp(); break; }
                        if (CanDropFromGrab(out int height4)) { ExecuteDropFromGrab(height4); break; }
                        break;
                }
            }
            void ProcessUseAbility()
            {
                // Only use the ability if it is not
                // in use already.
                if (!genres[activeGenre].ability.InUse)
                {
                    if (genres[activeGenre].ability.IsUsable(World.BeatService.CurrentBeatCount))
                    {
                        genres[activeGenre].ability.StartUsing(World.BeatService.CurrentBeatCount);
                        BeatAction action = genres[activeGenre].ability.ElapseBeat();
                        if (action != null)
                            queuedActions.Enqueue(action);
                        abilityInUse = true;
                    }
                }
            }
            void ExecuteAttack()
            {
                queuedActions.Enqueue(new BeatAction(
                    null,
                    actionDurations[ActionState.Attacking]));
                stateThisFrame = ActionState.Attacking;
                animationSnapState = SnapState.None;
            }
            void ProcessSetGenre(int genre)
            {
                // If a different genre is selected;
                if (activeGenre != genre)
                {
                    queuedActions.Enqueue(new BeatAction(1));
                    animationSnapState = SnapState.None;
                    // Interrupt the current ability if
                    // it is in use.
                    if (genres[activeGenre].ability.InUse)
                        genres[activeGenre].ability.StopUsing();
                    // Update the audio switch in Wwise.
                    // TODO this should be abstracted to not
                    // be wwise specific.
                    AkSoundEngine.SetState("Level_1", genres[genre].wwiseGenreTarget);
                    activeGenre = genre;
                }
            }

            #region duck unduck
            bool CanUnDuck()
            {
                return
                    !colliders.AnyInside(0, 0, 0, TileHeight - 1);
            }
            void ExecuteUnDuck()
            {
                // Reset to the normal height.
                TileHeight = unDuckedHeight;
                // Update the animation state.
                queuedActions.Enqueue(new BeatAction(
                    actionDurations[ActionState.UnDucking]));

                stateThisFrame = ActionState.UnDucking;
                nextState = ActionState.Idle;

                moveContextAnimator.State = ActionContext.Standing;
                animationSnapState = SnapState.None;
            }
            void ExecuteDuck()
            {
                // Reset to the normal height.
                TileHeight = duckingTileHeight;
                // Update the animation state.
                stateThisFrame = ActionState.DuckingDown;
                queuedActions.Enqueue(new BeatAction(
                    actionDurations[ActionState.DuckingDown]));
                moveContextAnimator.State = ActionContext.Ducking;
                nextState = ActionState.Idle;
                animationSnapState = SnapState.None;
            }
            #endregion

            #region pull up
            bool CanPullUp()
            {
                return
                    colliders[directionStep, TileHeight - 1, CollisionDirectionMask.Down] &&
                    !colliders.AnyInside(directionStep, TileHeight, directionStep, 2 * TileHeight - 1) &&
                    !colliders.AnyInside(0, TileHeight - 1, 0, 2 * TileHeight - 1);
            }
            void ExecutePullUp()
            {
                bool facingRight = Direction is Direction.Right;
                stateThisFrame = ActionState.PullingUp;
                moveContextAnimator.State = ActionContext.Standing;
                nextState = ActionState.Idle;
                animationSnapState = SnapState.AwaitingForwardSnap;
                // Create the animations.
                queuedActions.Clear();
                queuedActions.Enqueue(new BeatAction(
                    ActorAnimationsGenerator.CreatePullUpPath(!facingRight, TileHeight),
                    actionDurations[ActionState.PullingUp]));

                lastFrameHintPosition = animationSnapHintTransform.localPosition;
            }
            #endregion

            #region jump up
            bool CanJumpUp()
            {
                // Is there room to jump up?
                return !colliders.AnyInside(0, 0, 0, TileHeight - 1 + jumpApex);
            }
            void ExecuteJumpUp()
            {
                // Set the current and next state.
                stateThisFrame = ActionState.Jumping;
                moveContextAnimator.State = ActionContext.Airborne;
                nextState = ActionState.Falling;
                animationSnapState = SnapState.None;
                // Generate the animations.
                queuedActions.Clear();
                queuedActions.Enqueue(new BeatAction(
                    ActorAnimationsGenerator.CreateJumpUpPath(jumpApex),
                    actionDurations[ActionState.Jumping]));
                queuedActions.Enqueue(new BeatAction(
                    ActorAnimationsGenerator.CreateJumpUpPath(-jumpApex),
                    actionDurations[ActionState.Falling]));
            }
            #endregion

            #region drop from grab
            bool CanDropFromGrab(out int dropHeight)
            {
                for (int y = -1; y >= -maxDropDistance; y--)
                {
                    if (colliders[0, y, CollisionDirectionMask.Down])
                    {
                        dropHeight = y;
                        return true;
                    }
                }
                dropHeight = 0;
                return false;
            }
            void ExecuteDropFromGrab(int dropHeight)
            {
                // Set the current state.
                stateThisFrame = ActionState.Falling;
                moveContextAnimator.State = ActionContext.Standing;
                nextState = ActionState.Idle;
                animationSnapState = SnapState.None;
                // Create the animations.
                queuedActions.Clear();
                queuedActions.Enqueue(new BeatAction(
                    ActorAnimationsGenerator.CreateDropDownPath(dropHeight + 1),
                    actionDurations[ActionState.Falling]));
            }
            #endregion

            #region hang down
            bool CanHangDown()
            {
                return
                    !colliders.AnyInside(directionStep, TileHeight - 1, directionStep, -TileHeight);
            }
            void ExecuteHangDown()
            {
                bool facingRight = directionStep == 1;
                // Flip the direction when going over ledge.
                Direction = facingRight ? Direction.Left : Direction.Right;
                // Set the current and upcoming state.
                stateThisFrame = ActionState.DroppingToHang;
                animationSnapState = SnapState.AwaitingBackSnap;
                moveContextAnimator.State = ActionContext.Hanging;
                nextState = ActionState.Idle;
                // Generate the animations.
                queuedActions.Clear();
                queuedActions.Enqueue(new BeatAction(
                    ActorAnimationsGenerator.CreateHangDownPath(facingRight, TileHeight),
                    actionDurations[ActionState.DroppingToHang]));

                Surfaces.Surface endSurface;
                Vector2 endLocation;
                World.SweepTranslate(CurrentSurface, Location,
                    queuedActions.Peek().Path(1f),
                    out endSurface,
                    out endLocation);
                snapBeatLocation = endSurface.GetLocation(endLocation - Vector2.one * 0.5f);


                lastFrameHintPosition = animationSnapHintTransform.localPosition;
            }
            #endregion

            #region walk
            bool CanWalk()
            {
                return
                    colliders[directionStep, -1, CollisionDirectionMask.Down] &&
                    !colliders.AnyInside(directionStep, 0, directionStep, TileHeight - 1);
            }
            void ExecuteWalk()
            {
                // Properly orient the direction.
                Direction = directionStep is 1 ? Direction.Right : Direction.Left;
                stateThisFrame = ActionState.Walking;
                moveContextAnimator.State = ActionContext.Standing;
                animationSnapState = SnapState.None;
                nextState = ActionState.Idle;
                // Create the animations.
                queuedActions.Clear();
                queuedActions.Enqueue(new BeatAction(
                    ActorAnimationsGenerator.CreateWalkPath(directionStep),
                    actionDurations[ActionState.Walking]));
            }
            #endregion

            #region step
            bool CanStep(out int atHeight)
            {
                // Organize step heights to prioritize upwards
                // steps and minimum effort steps. Each height
                // will be checked.
                List<int> heights = new List<int>();
                for (int step = 1; step <= autoStepHeight; step++)
                    heights.Add(step);
                for (int step = -1; step >= -autoStepHeight; step--)
                    heights.Add(step);
                // Check for a valid move at each height.
                foreach (int height in heights)
                {
                    if (colliders[directionStep, height - 1, CollisionDirectionMask.Down] &&
                        !colliders.AnyInside(directionStep, height, directionStep, height + TileHeight - 1))
                    {
                        atHeight = height;
                        return true;
                    }
                }
                // Otherwise there is no step.
                atHeight = 0;
                return false;
            }
            void ExecuteStep(int atHeight)
            {
                // Ensure the direction is updated
                // in case the player turned.
                Direction = Direction.Right;
                // Set current and next state.
                stateThisFrame = ActionState.Walking;
                animationSnapState = SnapState.None;
                moveContextAnimator.State = ActionContext.Standing;
                nextState = ActionState.Idle;
                // Create the animations.
                queuedActions.Clear();
                queuedActions.Enqueue(new BeatAction(
                    ActorAnimationsGenerator.CreateWalkPath(directionStep, atHeight),
                    actionDurations[ActionState.Walking]));
            }
            #endregion

            bool TryJump()
            {
                // Start by looking for a max height jump,
                // then progress downwards to a height one jump.
                for (int height = jumpApex; height > 0; height--)
                {
                    // Check for ceiling clearence.
                    int maxXDistance = maxJumpX;
                    for (int x = 0; Mathf.Abs(x) < maxJumpX; x += directionStep)
                    {
                        if (colliders.AnyInside(x, TileHeight, x, TileHeight - 1 + jumpApex))
                        {
                            maxXDistance = x;
                            break;
                        }
                    }
                    // Search for a jump in the clearence area
                    // from the top down.
                    for (int y = 0; y >= -maxDropDistance; y--)
                    {
                        // Sweep from left to right to look for landing spots.
                        for (int x = directionStep; Mathf.Abs(x) <= maxXDistance; x += directionStep)
                        {
                            // Is the sweep path blocked? If so stop checking this row.
                            if (colliders[x, y]) break;
                            // Is there a block to land on, and is it a far enough jump?
                            if (colliders[x, y - 1, CollisionDirectionMask.Down] && Mathf.Abs(x) >= minJumpX)
                            {
                                stateThisFrame = ActionState.Jumping;
                                animationSnapState = SnapState.None;
                                moveContextAnimator.State = ActionContext.Airborne;
                                nextState = ActionState.Falling;

                                queuedActions.Clear();
                                List<ActorAnimationPath> jumpArc = ActorAnimationsGenerator.CreateJumpPaths(x, y, height);
                                queuedActions.Enqueue(new BeatAction(jumpArc[0], actionDurations[ActionState.Jumping]));
                                for (int i = 1; i < jumpArc.Count; i++)
                                    queuedActions.Enqueue(new BeatAction(jumpArc[i], actionDurations[ActionState.Falling]));
                                return true;
                            }
                        }
                    }
                }
                return false;
            }

            #region jump grab
            bool CanJumpGrab(out int height)
            {
                for (int step = TileHeight + 1; step <= maxPullupHeight; step++)
                {
                    if (colliders[directionStep, step - 1, CollisionDirectionMask.Down]
                        && !colliders.AnyInside(directionStep, step, directionStep, step + TileHeight - 1)
                        && !colliders.AnyInside(0, TileHeight - 1, 0, step + TileHeight - 1))
                    {
                        height = step;
                        return true;
                    }
                }
                height = 0;
                return false;
            }
            void ExecuteJumpGrab(int height)
            {
                // Set the animation state.
                stateThisFrame = ActionState.Jumping;
                moveContextAnimator.State = ActionContext.Hanging;
                nextState = ActionState.Idle;
                animationSnapState = SnapState.None;
                // Create the animation paths.
                queuedActions.Clear();
                queuedActions.Enqueue(new BeatAction(
                    ActorAnimationsGenerator.CreateJumpUpPath(height - TileHeight),
                    actionDurations[ActionState.Jumping]));
            }
            #endregion
        }


        private void Update()
        {
            if (Application.isPlaying)
            {
                // Are there animations to execute
                // during this beat?
                if (queuedActions.Count > 0)
                {
                    BeatAction currentAction = queuedActions.Peek();
                    if (currentAction.Path != null)
                    {
                        // Request the next animation location.
                        Vector2 toLocation = currentAction.Path
                            ((currentActionProgress + World.BeatService.CurrentInterpolant)
                                / currentAction.Duration);
                        // Apply the translation to the actor.
                        World.TranslateActor(this, toLocation - lastAnimationFrame);
                        lastAnimationFrame = toLocation;
                    }
                }

                // Apply override for animation states that move the transform.
                float animationStep = (animationSnapHintTransform.localPosition - lastFrameHintPosition)
                    .magnitude / Time.deltaTime;
                switch (animationSnapState)
                {
                    case SnapState.AwaitingForwardSnap:
                        if (animationStep > 0.5f)
                            animationSnapState = SnapState.None;
                        else
                            meshContainer.position = snapBeatLocation;
                        break;
                    case SnapState.AwaitingBackSnap:
                        meshContainer.position = snapBeatLocation;
                        break;
                }
                lastFrameHintPosition = animationSnapHintTransform.localPosition;
                if (animationSnapState is SnapState.None)
                    meshContainer.position = transform.position;

                // Update the camera.
                cameraPivot.transform.position = transform.position;
                cameraPivot.rotation = Quaternion.RotateTowards(
                    cameraPivot.rotation, transform.rotation,
                    cameraDegreesPerSecond * Time.deltaTime);
                // Update the player rotation.
                meshContainer.localRotation = Quaternion.RotateTowards(
                    meshContainer.localRotation,
                    Quaternion.AngleAxis(targetYAxisDegrees, Vector3.up),
                    Time.deltaTime * pivotDegreesPerBeat * (1f / World.BeatService.SecondsPerBeat));
            }
        }

        public void ApplyDamage(float amount)
        {
            health -= amount;
        }

        public void ApplyKnockback(int knockbackX, int knockbackY)
        {
            //throw new NotImplementedException();
        }
    }

}
