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
        [SerializeField] private string animatorMovementModeName = string.Empty;
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

        [Header("Player State (Debug)")]
        [Tooltip("The current movement mode of the player.")]
        [SerializeField][ReadonlyField] private MovementMode mode = MovementMode.Grounded;

        private MovementMode nextMode;

        private int animatorMovementMode;

        private void SetMode(MovementMode mode)
        {
            this.mode = mode;
            // TODO this should cast the enum directly to an int;
            // Alternatively there could be an animator controller
            // that observes the player (to decouple from animator).
            switch (mode)
            {
                case MovementMode.Grounded:
                    animator.SetInteger(animatorMovementModeName, 1);
                    break;
                case MovementMode.Airtime:
                    animator.SetInteger(animatorMovementMode, 2);
                    break;
                case MovementMode.HangingLeft:
                case MovementMode.HangingRight:
                    animator.SetInteger(animatorMovementMode, 3);
                    break;
            }
        }



        public event Action BeatEarly;
        public event Action BeatLate;

        protected override sealed void OnDirectionChanged(Direction direction)
        {
            /*
            meshContainer.localRotation = Quaternion.AngleAxis(
                direction is Direction.Right ? 0f : 180f,
                Vector3.up);
            */
        }

        private int activeGenre;

        private enum MovementMode : int
        {
            Grounded,
            Airtime,
            HangingLeft,
            HangingRight,
            Ducking
        }


        private Vector2 lastAnimationFrame;

        private Vector3 lastBeatLocation;

        private Queue<BeatAnimation> currentAnimations;


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
            [SerializeField] public string abilityUsedName = "Used";
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
                World.BeatService = beatService;
                activeGenre = 0;
                currentAnimations = new Queue<BeatAnimation>();
                beatService.BeatOffset = -inputTolerance * 0.5f;
                beatService.BeatElapsed += OnBeatElapsed;
                SoundtrackSet levelSet = SoundtrackSettings.Load().GetSetByID(soundtrackSet);
                beatService.SetBeatSoundtrack(levelSet);

                animatorMovementMode = Animator.StringToHash(animatorMovementModeName);
                // Set the animator such that 60 frames (1 second)
                // elapses in one beat.
                animator.speed = levelSet.BeatsPerMinute / 60f;
                SetMode(MovementMode.Grounded);
                nextMode = MovementMode.Grounded;

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
            meshContainer.position = transform.position;

            #region Finalize Last Beat Animations
            // Finalize the prior animation if there was one.
            if (currentAnimations.Count > 0)
            {
                Vector2 toLocation = currentAnimations.Peek().Path(1f);
                // Apply the translation to the actor.
                World.TranslateActor(this, toLocation - lastAnimationFrame);
                currentAnimations.Dequeue();
            }
            lastBeatLocation = transform.position;
            lastAnimationFrame = Vector2.zero;
            SetMode(nextMode);
            if (mode is MovementMode.Airtime && currentAnimations.Count == 0)
                SetMode(MovementMode.Grounded);
            #endregion
            #region Query World State
            // Query the world for the surrounding colliders.
            // These will be used for movement logic.
            NearbyColliderSet colliders = World.GetNearbyColliders(this, 9, 9, new List<GridActor>() { this });
            #endregion
            #region Process Input

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

            if (!movementOverriden)
            {


                // React to the latest input if it has
                // been timed well enough.
                if (Mathf.Abs(controller.LatestTimestamp - beatTime) < inputTolerance)
                {
                    switch (controller.LatestAction)
                    {
                        case PlayerAction.Jump: ProcessJump(); break;
                        case PlayerAction.Duck: ProcessDuck(); break;
                        case PlayerAction.MoveLeft:
                            ProcessMoveLeft(); break;
                        case PlayerAction.MoveRight:
                            ProcessMoveRight(); break;
                        case PlayerAction.UseAbility:
                            ProcessUseAbility(); break;
                        case PlayerAction.Attack:
                            ProcessAttack(); break;
                        case PlayerAction.SetGenre1:
                            ProcessSetGenre(0); break;
                        case PlayerAction.SetGenre2:
                            ProcessSetGenre(1); break;
                        case PlayerAction.SetGenre3:
                            ProcessSetGenre(2); break;
                        case PlayerAction.SetGenre4:
                            ProcessSetGenre(3); break;
                    }
                    wasInputLastBeat = true;
                }
                else
                {

                    if (wasInputLastBeat)
                    {
                        if (controller.LatestTimestamp - beatTime > 0f)
                            BeatLate?.Invoke();
                        else
                            BeatEarly?.Invoke();
                        wasInputLastBeat = false;
                    }
                }
                if (mode is MovementMode.Grounded && currentAnimations.Count == 0)
                    animator.SetBool("IsIdle", true);
                else
                    animator.SetBool("IsIdle", false);
            }
            void ProcessJump()
            {
                switch (mode)
                {
                    case MovementMode.Grounded:
                    case MovementMode.Ducking:
                        // Attempt to enter a door.
                        if (TryEnterDoor()) break;
                        // Attempt to jump up.
                        if (TryJumpUp()) break;
                        // Otherwise do nothing.
                        break;
                    // Pull up to the ledge.
                    case MovementMode.HangingLeft:
                        PullUpLeft(); break;
                    case MovementMode.HangingRight:
                        PullUpRight(); break;
                }
            }
            void ProcessDuck()
            {
                switch (mode)
                {
                    case MovementMode.Ducking:
                        // Exit the duck state.
                        mode = MovementMode.Grounded; break;
                    case MovementMode.Grounded:
                        // First try hanging from either edge.
                        if (TryHangRight()) break;
                        if (TryHangLeft()) break;
                        // Otherwise simply enter the duck state.
                        mode = MovementMode.Ducking; break;
                    case MovementMode.HangingLeft:
                    case MovementMode.HangingRight:
                        // Try to drop from the grab.
                        TryDropFromGrab(); break;
                }
            }
            void ProcessMoveLeft()
            {
                switch (mode)
                {
                    case MovementMode.Airtime:
                        // Try stepping up to the left.
                        if (TryStepLeft()) break;
                        // Try to grab up a ledge to the left.
                        if (TryGrabUpLeft()) break;
                        // Otherwise do nothing.
                        break;
                    case MovementMode.Grounded:
                    case MovementMode.Ducking:
                        // Try to step directly to the left.
                        if (TryWalk(Direction.Left)) break;
                        // Otherwise try to step up/down to the left.
                        if (TryStepLeft()) break;
                        // Otherwise try to jump a gap.
                        if (TryJump()) break;
                        // Otherwise try to climb up a nearby block.
                        if (TryGrabUpLeft()) break;
                        // Otherwise try to jump up to grab a ledge.
                        if (TryJumpGrabLeft()) break;
                        // Otherwise try to drop down from a ledge.
                        if (TryHangLeft()) break;
                        // Otherwise try to jump up.
                        if (TryJumpUp()) break;
                        // Otherwise do nothing.
                        break;
                    case MovementMode.HangingLeft:
                        // Pull up the ledge.
                        PullUpLeft(); break;
                    case MovementMode.HangingRight:
                        // Try falling from the grab.
                        if (TryDropFromGrab()) break;
                        // Otherwise do nothing.
                        break;
                }
            }
            void ProcessMoveRight()
            {
                switch (mode)
                {
                    case MovementMode.Airtime:
                        // Try stepping up to the right.
                        if (TryStepRight()) break;
                        // Try to grab up a ledge to the right.
                        if (TryGrabUpRight()) break;
                        // Otherwise do nothing.
                        break;
                    case MovementMode.Grounded:
                    case MovementMode.Ducking:
                        // Try to step directly to the right.
                        if (TryWalk(Direction.Right)) break;
                        // Otherwise try to step up/down to the right.
                        if (TryStepRight()) break;
                        // Otherwise try to jump a gap.
                        if (TryJump()) break;
                        // Otherwise try to grab up a nearby block.
                        if (TryGrabUpRight()) break;
                        // Otherwise try to jump up to grab a ledge.
                        if (TryJumpGrabRight()) break;
                        // Otherwise try to drop down from a ledge.
                        if (TryHangRight()) break;
                        // Otherwise try to jump up.
                        if (TryJumpUp()) break;
                        // Otherwise do nothing.
                        break;
                    case MovementMode.HangingRight:
                        // Pull up the ledge.
                        PullUpRight(); break;
                    case MovementMode.HangingLeft:
                        // Try falling from the grab.
                        if (TryDropFromGrab()) break;
                        // Otherwise do nothing.
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
                        animator.SetTrigger(genres[activeGenre].abilityUsedName);


                        genres[activeGenre].ability.StartUsing(beatService.CurrentBeatCount);
                        ActorAnimationPath path = genres[activeGenre].ability.ElapseBeat();
                        if (path != null)
                            currentAnimations.Enqueue(new BeatAnimation(path));
                        abilityInUse = true;
                    }
                }
            }
            void ProcessAttack()
            {
                animator.SetTrigger("Attacked");
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
            #endregion
            #region State Changes
            void PullUpRight()
            {
                currentAnimations.Clear();
                currentAnimations.Enqueue(new BeatAnimation(
                    ActorAnimationsGenerator.CreatePullUpPath(false, TileHeight), true));
                SetMode(MovementMode.HangingRight);
                animator.SetTrigger("ClimbedUp");
                nextMode = MovementMode.Grounded;
            }
            void PullUpLeft()
            {
                currentAnimations.Clear();
                currentAnimations.Enqueue(new BeatAnimation(
                    ActorAnimationsGenerator.CreatePullUpPath(true, TileHeight), true));
                SetMode(MovementMode.HangingLeft);
                animator.SetTrigger("ClimbedUp");
                nextMode = MovementMode.Grounded;
            }
            bool TryEnterDoor()
            {
                return World.TryTurnForwards(this);
            }
            bool TryJumpUp()
            {
                // Is there room to jump?
                if (!colliders.AnyInside(0, 0, 0, TileHeight - 1 + jumpApex))
                {
                    currentAnimations.Clear();
                    currentAnimations.Enqueue(new BeatAnimation(
                        ActorAnimationsGenerator.CreateJumpUpPath(jumpApex)));
                    currentAnimations.Enqueue(new BeatAnimation(
                        ActorAnimationsGenerator.CreateJumpUpPath(-jumpApex)));
                    SetMode(MovementMode.Airtime);
                    animator.SetTrigger("Jumped");
                    nextMode = MovementMode.Airtime;
                    return true;
                }
                return false;
            }
            bool TryDropFromGrab()
            {
                for (int y = -1; y >= -maxDropDistance; y--)
                {
                    if (colliders[0, y])
                    {
                        currentAnimations.Clear();
                        currentAnimations.Enqueue(new BeatAnimation(
                            ActorAnimationsGenerator.CreateDropDownPath(y + 1)));
                        SetMode(MovementMode.Airtime);
                        nextMode = MovementMode.Grounded;
                        return true;
                    }
                }
                return false;
            }
            bool TryHangRight()
            {
                if (Direction is Direction.Right && !colliders.AnyInside(1, TileHeight - 1, 1, -TileHeight))
                {
                    Direction = Direction.Left;
                    mode = MovementMode.HangingRight;
                    currentAnimations.Clear();
                    currentAnimations.Enqueue(new BeatAnimation(
                        ActorAnimationsGenerator.CreateHangDownPath(true, TileHeight)));
                    SetMode(MovementMode.HangingRight);
                    animator.SetTrigger("DroppedDown");
                    nextMode = MovementMode.HangingRight;
                    return true;
                }
                return false;
            }
            bool TryHangLeft()
            {
                if (Direction != Direction.Right && !colliders.AnyInside(-1, TileHeight - 1, -1, -TileHeight))
                {
                    mode = MovementMode.HangingLeft;
                    Direction = Direction.Right;
                    currentAnimations.Clear();
                    currentAnimations.Enqueue(new BeatAnimation(
                        ActorAnimationsGenerator.CreateHangDownPath(false, TileHeight)));
                    SetMode(MovementMode.HangingLeft);
                    animator.SetTrigger("DroppedDown");
                    nextMode = MovementMode.HangingLeft;
                    return true;
                }
                return false;
            }

            bool TryWalk(Direction direction)
            {
                int step = direction == Direction.Right ? 1 : -1;
                if (// Is there space to move one tile over?
                    !colliders.AnyInside(step, 0, step, TileHeight - 1) &&
                    // Is there a tile to move onto?
                    colliders[step, -1])
                {
                    // Apply movement.
                    Direction = direction;
                    nextMode = MovementMode.Grounded;
                    currentAnimations.Clear();
                    currentAnimations.Enqueue(new BeatAnimation(
                        ActorAnimationsGenerator.CreateWalkPath(step)));
                    animator.SetTrigger("Walked");
                    return true;
                }
                return false;
            }
            bool TryStepRight()
            {
                // Attempt to do a step up.
                for (int step = 1; step <= autoStepHeight; step++)
                {
                    if (colliders[1, step - 1] && !colliders.AnyInside(1, step, 1, step + TileHeight - 1))
                    {
                        Direction = Direction.Right;
                        mode = MovementMode.Grounded;
                        currentAnimations.Clear();
                        currentAnimations.Enqueue(new BeatAnimation(
                            ActorAnimationsGenerator.CreateWalkPath(1, step)));
                        animator.SetTrigger("Walked");
                        nextMode = MovementMode.Grounded;
                        return true;
                    }
                }
                // Attempt to do a step down.
                for (int step = -1; step >= -autoStepHeight; step--)
                {
                    if (colliders[1, step - 1] && !colliders.AnyInside(1, step, 1, step + TileHeight - 1))
                    {
                        Direction = Direction.Right;
                        currentAnimations.Clear();
                        currentAnimations.Enqueue(new BeatAnimation(
                            ActorAnimationsGenerator.CreateWalkPath(1, step)));
                        animator.SetTrigger("Walked");
                        nextMode = MovementMode.Grounded;
                        return true;
                    }
                }
                return false;
            }
            bool TryJump()
            {
                int xStep = Direction is Direction.Right ? 1 : -1;
                // Start by looking for a max height jump,
                // then progress downwards to a height one jump.
                for (int height = jumpApex; height > 0; height--)
                {
                    // Check for ceiling clearence.
                    int maxXDistance = maxJumpX;
                    for (int x = 0; Mathf.Abs(x) < maxJumpX; x += xStep)
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
                        for (int x = xStep; Mathf.Abs(x) <= maxXDistance; x += xStep)
                        {
                            // Is the sweep path blocked? If so stop checking this row.
                            if (colliders[x, y]) break;
                            // Is there a block to land on, and is it a far enough jump?
                            if (colliders[x, y - 1] && Mathf.Abs(x) >= minJumpX)
                            {
                                currentAnimations.Clear();
                                List<ActorAnimationPath> jumpArc = ActorAnimationsGenerator.CreateJumpPaths(x, y, height);
                                foreach (ActorAnimationPath path in jumpArc)
                                    currentAnimations.Enqueue(new BeatAnimation(path));
                                animator.SetTrigger("Jumped");
                                SetMode(MovementMode.Airtime);
                                nextMode = MovementMode.Airtime;
                                return true;
                            }
                        }
                    }
                }
                return false;
            }
            bool TryGrabUpRight()
            {
                // Attempt to do an instant grab up.
                if (colliders[1, TileHeight - 1] && !colliders.AnyInside(1, TileHeight, 1, 2 * TileHeight - 1)
                    && !colliders.AnyInside(0, TileHeight - 1, 0, 2 * TileHeight - 1))
                {
                    Direction = Direction.Right;
                    PullUpRight();
                    return true;
                }
                return false;
            }
            bool TryJumpGrabRight()
            {
                // Attempt to jump into a grab.
                for (int step = TileHeight + 1; step <= maxPullupHeight; step++)
                {
                    if (colliders[1, step - 1] && !colliders.AnyInside(1, step, 1, step + TileHeight - 1)
                        && !colliders.AnyInside(0, TileHeight - 1, 0, step + TileHeight - 1))
                    {
                        Direction = Direction.Right;
                        mode = MovementMode.HangingLeft;
                        currentAnimations.Clear();
                        currentAnimations.Enqueue(new BeatAnimation(
                            ActorAnimationsGenerator.CreateJumpUpPath(step - TileHeight)));

                        animator.SetTrigger("Jumped");
                        SetMode(MovementMode.Airtime);
                        nextMode = MovementMode.HangingRight;
                        return true;
                    }
                }
                return false;
            }
            bool TryStepLeft()
            {
                // Attempt to do a step up.
                for (int step = 1; step <= autoStepHeight; step++)
                {
                    if (colliders[-1, step - 1] && !colliders.AnyInside(-1, step, -1, step + TileHeight - 1))
                    {
                        mode = MovementMode.Grounded;
                        Direction = Direction.Left;
                        currentAnimations.Clear();
                        currentAnimations.Enqueue(new BeatAnimation(
                            ActorAnimationsGenerator.CreateWalkPath(-1, step)));
                        animator.SetTrigger("Walked");
                        nextMode = MovementMode.Grounded;
                        return true;
                    }
                }
                // Attempt to do a step down.
                for (int step = -1; step >= -autoStepHeight; step--)
                {
                    if (colliders[-1, step - 1] && !colliders.AnyInside(-1, step, -1, step + TileHeight - 1))
                    {
                        mode = MovementMode.Grounded;
                        Direction = Direction.Left;
                        currentAnimations.Clear();
                        currentAnimations.Enqueue(new BeatAnimation(
                            ActorAnimationsGenerator.CreateWalkPath(-1, step)));
                        animator.SetTrigger("Walked");
                        nextMode = MovementMode.Grounded;
                        return true;
                    }
                }
                return false;
            }
            bool TryGrabUpLeft()
            {
                // Attempt to do an instant grab up.
                if (colliders[-1, TileHeight - 1] && !colliders.AnyInside(-1, TileHeight, -1, 2 * TileHeight - 1)
                    && !colliders.AnyInside(0, TileHeight - 1, 0, 2 * TileHeight - 1))
                {
                    PullUpLeft();
                    Direction = Direction.Left;
                    return true;
                }
                return false;
            }
            bool TryJumpGrabLeft()
            {
                // Attempt to jump into a grab.
                for (int step = TileHeight + 1; step <= maxPullupHeight; step++)
                {
                    if (colliders[-1, step - 1] && !colliders.AnyInside(-1, step, -1, step + TileHeight - 1)
                        && !colliders.AnyInside(0, TileHeight - 1, 0, step + TileHeight - 1))
                    {
                        mode = MovementMode.HangingRight;
                        Direction = Direction.Left;
                        currentAnimations.Clear();
                        currentAnimations.Enqueue(new BeatAnimation(
                            ActorAnimationsGenerator.CreateJumpUpPath(step - TileHeight)));
                        animator.SetTrigger("Jumped");
                        SetMode(MovementMode.Airtime);
                        nextMode = MovementMode.HangingLeft;
                        return true;
                    }
                }
                return false;
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
                float targetDegrees = 0f;
                switch (Direction)
                {
                    case Direction.Left: targetDegrees = 180f; break;
                    case Direction.Right: targetDegrees = 0f; break;
                }
                meshContainer.localRotation = Quaternion.RotateTowards(meshContainer.localRotation, Quaternion.AngleAxis(targetDegrees, Vector3.up), Time.deltaTime * pivotDegreesPerBeat * (1f / beatService.SecondsPerBeat));
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
