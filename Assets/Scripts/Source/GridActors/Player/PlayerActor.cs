using BattleRoyalRhythm.Audio;
using BattleRoyalRhythm.GridActors;
using BattleRoyalRhythm.Input;
using System;
using System.Collections;
using System.Collections.Generic;
using Tools;
using UnityEngine;

namespace BattleRoyalRhythm.GridActors.Player
{
    /// <summary>
    /// The actor that controls the main player character.
    /// </summary>
    public sealed class PlayerActor : GridActor, IDamageable, IKnockbackable
    {
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
        [SerializeField] private Animator daAnimator = null;
        [SerializeField] private AnimatorState<ActionState> moveModeAnimator = null;
        [SerializeField] private AnimatorState<ActionContext> moveContextAnimator = null;
        [SerializeField][Min(0f)] private float pivotDegreesPerBeat = 180f;

        [SerializeField] private BeatService beatService = null;
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

        private int unDuckedHeight;

        public event Action<float> BeatEarly;
        public event Action<float> BeatLate;
        public event Action<PlayerAction> ActionExecuted;

        private float targetYAxisDegrees;

        protected override sealed void OnDirectionChanged(Direction direction)
        {
            switch (direction)
            {
                case Direction.Left: targetYAxisDegrees = 180f; break;
                case Direction.Right: targetYAxisDegrees = 0f; break;
            }
        }

        private int activeGenre;

        private Vector2 lastAnimationFrame;

        private Vector3 lastBeatLocation;

        private Queue<BeatAnimation> currentAnimations;

        private ActionState nextState;
        private ActionState stateThisFrame;

        private sealed class BeatAnimation
        {
            public BeatAnimation(ActorAnimationPath path, bool animatorOverrides = false)
            {
                Path = path;
                AnimatorOverrides = animatorOverrides;
            }

            public ActorAnimationPath Path { get; }
            public bool AnimatorOverrides { get; }
        }

        private bool abilityInUse = false;


        [Serializable]
        private sealed class GenreAbilityPair
        {
            [SerializeField] public string wwiseGenreTarget = "Wwise Target";
            [SerializeField] public ActorAbility ability = null;
        }


        public float Health => health;

        public float MaxHealth => maxHealth;


        protected override void OnValidate()
        {
            base.OnValidate();

        }

        private void Start()
        {

            if (Application.isPlaying)
            {
                unDuckedHeight = TileHeight;

                World.BeatService = beatService;
                activeGenre = 0;
                currentAnimations = new Queue<BeatAnimation>();
                beatService.BeatOffset = -inputTolerance * 0.5f;
                beatService.BeatElapsed += OnBeatElapsed;
                SoundtrackSet levelSet = SoundtrackSettings.Load().GetSetByID(soundtrackSet);
                beatService.SetBeatSoundtrack(levelSet);

                // Set the animator such that 60 frames (1 second)
                // elapses in one beat.
                daAnimator.speed = levelSet.BeatsPerMinute / 60f;

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

        private bool wasInputLastBeat;

        private void OnBeatElapsed(float beatTime)
        {

            stateThisFrame = nextState;
            meshContainer.position = transform.position;

            // Finalize the prior animation if there was one.
            if (currentAnimations.Count > 0)
            {
                Vector2 toLocation = currentAnimations.Peek().Path(1f);
                // Apply the translation to the actor.
                World.TranslateActor(this, toLocation - lastAnimationFrame);
                currentAnimations.Dequeue();
            }

            // Query the world for the surrounding colliders.
            // These will be used for movement logic.
            NearbyColliderSet colliders = World.GetNearbyColliders(this, 9, 9);

            lastBeatLocation = transform.position;
            lastAnimationFrame = Vector2.zero;

            bool movementOverriden = false;
            if (abilityInUse)
            {
                // Ability use has completed.
                if (!genres[activeGenre].ability.InUse)
                    abilityInUse = false;
                else
                {
                    ActorAnimationPath path = genres[activeGenre].ability.ElapseBeat();
                    if (path != null)
                    {
                        currentAnimations.Enqueue(new BeatAnimation(path));
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
                if (Mathf.Abs(beatDelta) < inputTolerance)
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
                    if (currentAnimations.Count != 0)
                    {
                        ActionExecuted?.Invoke(controller.LatestAction);
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
                    if (genres[activeGenre].ability.IsUsable(beatService.CurrentBeatCount))
                    {
                        genres[activeGenre].ability.StartUsing(beatService.CurrentBeatCount);
                        ActorAnimationPath path = genres[activeGenre].ability.ElapseBeat();
                        if (path != null)
                            currentAnimations.Enqueue(new BeatAnimation(path));
                        abilityInUse = true;
                    }
                }
            }
            void ExecuteAttack()
            {
                stateThisFrame = ActionState.Attacking;
            }
            void ProcessSetGenre(int genre)
            {
                // If a different genre is selected;
                if (activeGenre != genre)
                {
                    // Interrupt the current ability if
                    // it is in use.
                    if (genres[activeGenre].ability.InUse)
                        genres[activeGenre].ability.StopUsing();
                    // Update the audio switch in Wwise.
                    // TODO this should be abstracted to not
                    // be wwise specific.
                    AkSoundEngine.SetSwitch("Level_1", genres[genre].wwiseGenreTarget, beatService.gameObject);
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
                stateThisFrame = ActionState.UnDucking;
                moveContextAnimator.State = ActionContext.Standing;
                nextState = ActionState.Idle;
            }
            void ExecuteDuck()
            {
                // Reset to the normal height.
                TileHeight = duckingTileHeight;
                // Update the animation state.
                stateThisFrame = ActionState.DuckingDown;
                moveContextAnimator.State = ActionContext.Ducking;
                nextState = ActionState.Idle;
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
                // Create the animations.
                currentAnimations.Clear();
                currentAnimations.Enqueue(new BeatAnimation(
                    ActorAnimationsGenerator.CreatePullUpPath(!facingRight, TileHeight), true));
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
                // Generate the animations.
                currentAnimations.Clear();
                currentAnimations.Enqueue(new BeatAnimation(
                    ActorAnimationsGenerator.CreateJumpUpPath(jumpApex)));
                currentAnimations.Enqueue(new BeatAnimation(
                    ActorAnimationsGenerator.CreateJumpUpPath(-jumpApex)));
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
                // Create the animations.
                currentAnimations.Clear();
                currentAnimations.Enqueue(new BeatAnimation(
                    ActorAnimationsGenerator.CreateDropDownPath(dropHeight + 1)));
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
                bool facingRight = Direction is Direction.Right;
                // Flip the direction when going over ledge.
                Direction = Direction is Direction.Right ? Direction.Left : Direction.Right;
                // Set the current and upcoming state.
                stateThisFrame = ActionState.DroppingToHang;
                moveContextAnimator.State = ActionContext.Hanging;
                nextState = ActionState.Idle;
                // Generate the animations.
                currentAnimations.Clear();
                currentAnimations.Enqueue(new BeatAnimation(
                    ActorAnimationsGenerator.CreateHangDownPath(facingRight, TileHeight)));
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
                nextState = ActionState.Idle;
                // Create the animations.
                currentAnimations.Clear();
                currentAnimations.Enqueue(new BeatAnimation(
                    ActorAnimationsGenerator.CreateWalkPath(directionStep)));
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
                moveContextAnimator.State = ActionContext.Standing;
                nextState = ActionState.Idle;
                // Create the animations.
                currentAnimations.Clear();
                currentAnimations.Enqueue(new BeatAnimation(
                    ActorAnimationsGenerator.CreateWalkPath(directionStep, atHeight)));
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
                                moveContextAnimator.State = ActionContext.Airborne;
                                nextState = ActionState.Falling;

                                currentAnimations.Clear();
                                List<ActorAnimationPath> jumpArc = ActorAnimationsGenerator.CreateJumpPaths(x, y, height);
                                foreach (ActorAnimationPath path in jumpArc)
                                    currentAnimations.Enqueue(new BeatAnimation(path));
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
                // Create the animation paths.
                currentAnimations.Clear();
                currentAnimations.Enqueue(new BeatAnimation(
                    ActorAnimationsGenerator.CreateJumpUpPath(height - TileHeight)));
            }
            #endregion
        }

        private void Update()
        {
            if (Application.isPlaying)
            {
                // Are there animations to execute
                // during this beat?
                if (currentAnimations.Count > 0)
                {
                    // Request the next animation location.
                    Vector2 toLocation = currentAnimations.Peek().Path
                        (beatService.CurrentInterpolant);
                    // Apply the translation to the actor.
                    World.TranslateActor(this, toLocation - lastAnimationFrame);
                    lastAnimationFrame = toLocation;
                    // Lag the animator transform behind.
                    if (currentAnimations.Peek().AnimatorOverrides)
                        meshContainer.position = lastBeatLocation;
                }
                else
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
                    Time.deltaTime * pivotDegreesPerBeat * (1f / beatService.SecondsPerBeat));
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
