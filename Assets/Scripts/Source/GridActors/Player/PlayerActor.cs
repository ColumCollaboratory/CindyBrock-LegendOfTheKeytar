using BattleRoyalRhythm.Audio;
using BattleRoyalRhythm.GridActors;
using BattleRoyalRhythm.Input;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BattleRoyalRhythm.GridActors.Player
{
    public sealed class PlayerActor : GridActor, IDamageable
    {

        protected override void OnDirectionChanged(bool isRightFacing)
        {
            playerMesh.localRotation = Quaternion.AngleAxis(isRightFacing ? 0f : 180f, Vector3.up);
        }

        private int activeGenre;

        private enum BeatAffordance : byte
        {
            Grounded,
            JumpApex,
            HangingLeft,
            HangingRight,
            Ducking
        }

        [SerializeField] private BeatAffordance affordance;

        private Vector2 lastAnimationFrame;

        private Queue<ActorAnimationPath> currentAnimations;

        private bool abilityInUse = false;

        [SerializeField][Min(1)] private int duckingTileHeight = 1;
        [SerializeField] private Transform playerMesh = null;
        [SerializeField][Min(0f)] private float inputTolerance = 0.1f;

        [SerializeField] private AnimationCurve walkCurve = null;

        [Tooltip("Jump height apex in tiles.")]
        [SerializeField][Min(1)] private int jumpApex = 2;

        [SerializeField] private PlayerController controller = null;
        [SerializeField] private WwiseBeatService beatService = null;

        [SerializeField] private GenreAbilityPair[] genres = null;

        [Serializable]
        private sealed class GenreAbilityPair
        {
            [SerializeField] public string wwiseGenreTarget = "Wwise Target";
            [SerializeField] public ActorAbility ability = null;
        }

        [Header("Automatic Actions")]
        [SerializeField][Min(0)] private int jumpDistance = 3;
        [SerializeField][Min(0)] private int maxDropDistance = 4;

        [SerializeField][Min(0)] private int maxPullupHeight = 3;
        [SerializeField][Min(0)] private int autoStepHeight = 1;


        protected override void OnValidate()
        {
            base.OnValidate();

        }

        private void Start()
        {

            if (Application.isPlaying)
            {
                activeGenre = 0;
                currentAnimations = new Queue<ActorAnimationPath>();
                beatService.BeatOffset = -inputTolerance * 0.5f;
                beatService.BeatElapsed += OnBeatElapsed;
                beatService.SetBeatFromEvent("Stage_1_Started", 140f);

                if (genres != null)
                    foreach (GenreAbilityPair pair in genres)
                        if (pair.ability != null)
                            pair.ability.UsingActor = this;
            }
        }

        private void OnEnable()
        {
            affordance = BeatAffordance.Grounded;
            IsRightFacing = true;
        }
        private void OnDisable()
        {

        }

        private void OnBeatElapsed(float beatTime)
        {
            #region Finalize Last Beat Animations
            // Finalize the prior animation if there was one.
            if (currentAnimations.Count > 0)
            {
                Vector2 toLocation = currentAnimations.Peek()(1f);
                // Apply the translation to the actor.
                World.TranslateActor(this, toLocation - lastAnimationFrame);
                currentAnimations.Dequeue();
            }
            lastAnimationFrame = Vector2.zero;
            #endregion
            #region Query World State
            // Query the world for the surrounding colliders.
            // These will be used for movement logic.
            NearbyColliderSet colliders = World.GetNearbyColliders(this, 9, 9);
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
                        currentAnimations.Enqueue(path);
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
                            IsRightFacing = false;
                            ProcessMoveLeft(); break;
                        case PlayerAction.MoveRight:
                            IsRightFacing = true;
                            ProcessMoveRight(); break;
                        case PlayerAction.UseAbility:
                            ProcessUseAbility(); break;
                        case PlayerAction.SetGenre1:
                            ProcessSetGenre(0); break;
                        case PlayerAction.SetGenre2:
                            ProcessSetGenre(1); break;
                        case PlayerAction.SetGenre3:
                            ProcessSetGenre(2); break;
                        case PlayerAction.SetGenre4:
                            ProcessSetGenre(3); break;
                    }
                }
            }
            // TODO this is needed to advance jump state.
            // Should be removed to conform structure.
            if (affordance is BeatAffordance.JumpApex && currentAnimations.Count == 1)
                affordance = BeatAffordance.Grounded;
            void ProcessJump()
            {
                switch (affordance)
                {
                    case BeatAffordance.Grounded:
                    case BeatAffordance.Ducking:
                        // Attempt to enter a door.
                        if (TryEnterDoor()) break;
                        // Attempt to jump up.
                        if (TryJumpUp()) break;
                        // Otherwise do nothing.
                        break;
                    // Pull up to the ledge.
                    case BeatAffordance.HangingLeft:
                        PullUpLeft(); break;
                    case BeatAffordance.HangingRight:
                        PullUpRight(); break;
                }
            }
            void ProcessDuck()
            {
                switch (affordance)
                {
                    case BeatAffordance.Ducking:
                        // Exit the duck state.
                        affordance = BeatAffordance.Grounded; break;
                    case BeatAffordance.Grounded:
                        // First try hanging from either edge.
                        if (TryHangRight()) break;
                        if (TryHangLeft()) break;
                        // Otherwise simply enter the duck state.
                        affordance = BeatAffordance.Ducking; break;
                    case BeatAffordance.HangingLeft:
                    case BeatAffordance.HangingRight:
                        // Try to drop from the grab.
                        TryDropFromGrab(); break;
                }
            }
            void ProcessMoveLeft()
            {
                switch (affordance)
                {
                    case BeatAffordance.Grounded:
                    case BeatAffordance.Ducking:
                        // Try to step directly to the right.
                        if (TryWalkLeft()) break;
                        // Otherwise try to step up/down to the right.
                        if (TryStepLeft()) break;
                        // Otherwise try to jump a gap.
                        if (TryJumpLeft()) break;
                        // Otherwise try to jump up to grab a ledge.
                        if (TryJumpGrabLeft()) break;
                        // Otherwise try to drop down from a ledge.
                        if (TryHangLeft()) break;
                        // Otherwise try to jump up.
                        if (TryJumpUp()) break;
                        // Otherwise do nothing.
                        break;
                    case BeatAffordance.HangingRight:
                        // Pull up the ledge.
                        PullUpRight(); break;
                    case BeatAffordance.HangingLeft:
                        // Try falling from the grab.
                        if (TryDropFromGrab()) break;
                        // Otherwise do nothing.
                        break;
                }
            }
            void ProcessMoveRight()
            {
                switch (affordance)
                {
                    case BeatAffordance.Grounded:
                    case BeatAffordance.Ducking:
                        // Try to step directly to the right.
                        if (TryWalkRight()) break;
                        // Otherwise try to step up/down to the right.
                        if (TryStepRight()) break;
                        // Otherwise try to jump a gap.
                        if (TryJumpRight()) break;
                        // Otherwise try to jump up to grab a ledge.
                        if (TryJumpGrabRight()) break;
                        // Otherwise try to drop down from a ledge.
                        if (TryHangRight()) break;
                        // Otherwise try to jump up.
                        if (TryJumpUp()) break;
                        // Otherwise do nothing.
                        break;
                    case BeatAffordance.HangingLeft:
                        // Pull up the ledge.
                        PullUpLeft(); break;
                    case BeatAffordance.HangingRight:
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
                        genres[activeGenre].ability.StartUsing(beatService.CurrentBeatCount);
                        ActorAnimationPath path = genres[activeGenre].ability.ElapseBeat();
                        if (path != null)
                            currentAnimations.Enqueue(path);
                        abilityInUse = true;
                    }
                }
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
                currentAnimations.Enqueue(ActorAnimationsGenerator.CreatePullUpPath(true, TileHeight));
                affordance = BeatAffordance.Grounded;
            }
            void PullUpLeft()
            {
                currentAnimations.Clear();
                currentAnimations.Enqueue(ActorAnimationsGenerator.CreatePullUpPath(false, TileHeight));
                affordance = BeatAffordance.Grounded;
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
                    currentAnimations.Enqueue(ActorAnimationsGenerator.CreateJumpUpPath(jumpApex));
                    affordance = BeatAffordance.JumpApex;
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
                        currentAnimations.Enqueue(ActorAnimationsGenerator.CreateDropDownPath(y + 1));
                        affordance = BeatAffordance.Grounded;
                        return true;
                    }
                }
                return false;
            }
            bool TryHangRight()
            {
                if (IsRightFacing && !colliders.AnyInside(1, TileHeight - 1, 1, -TileHeight))
                {
                    affordance = BeatAffordance.HangingRight;
                    currentAnimations.Clear();
                    currentAnimations.Enqueue(ActorAnimationsGenerator.CreateHangDownPath(true, TileHeight));
                    return true;
                }
                return false;
            }
            bool TryHangLeft()
            {
                if (!IsRightFacing && !colliders.AnyInside(-1, TileHeight - 1, -1, -TileHeight))
                {
                    affordance = BeatAffordance.HangingLeft;
                    currentAnimations.Clear();
                    currentAnimations.Enqueue(ActorAnimationsGenerator.CreateHangDownPath(false, TileHeight));
                    return true;
                }
                return false;
            }
            bool TryWalkRight()
            {
                // Is there a wall preventing right movement?
                if (!colliders.AnyInside(1, 0, 1, TileHeight - 1) &&
                    colliders[1, -1])
                {
                    // Move to the right.
                    affordance = BeatAffordance.Grounded;
                    currentAnimations.Clear();
                    currentAnimations.Enqueue(ActorAnimationsGenerator.CreateWalkPath(1));
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
                        affordance = BeatAffordance.Grounded;
                        currentAnimations.Clear();
                        currentAnimations.Enqueue(ActorAnimationsGenerator.CreateWalkPath(1, step));
                        return true;
                    }
                }
                // Attempt to do a step down.
                for (int step = -1; step >= -autoStepHeight; step--)
                {
                    if (colliders[1, step - 1] && !colliders.AnyInside(1, step, 1, step + TileHeight - 1))
                    {
                        currentAnimations.Clear();
                        currentAnimations.Enqueue(ActorAnimationsGenerator.CreateWalkPath(1, step));
                        return true;
                    }
                }
                return false;
            }
            bool TryJumpRight()
            {
                // Check by row to find the highest jump.
                for (int y = 0; y >= -maxDropDistance; y--)
                {
                    for (int x = 1; x <= jumpDistance; x++)
                    {
                        // Is the landing space obstructed?
                        if (colliders[x, y]) break;
                        // Is there a place to land on?
                        if (x >= 3 && colliders[x, y - 1])
                        {
                            currentAnimations.Clear();
                            foreach (ActorAnimationPath path in ActorAnimationsGenerator.CreateJumpPaths(x, y, jumpApex))
                                currentAnimations.Enqueue(path);
                            affordance = BeatAffordance.JumpApex;
                            return true;
                        }
                    }
                }
                return false;
            }
            bool TryJumpGrabRight()
            {
                // Attempt to do an instant grab up.
                if (colliders[1, TileHeight - 1] && !colliders.AnyInside(1, TileHeight, 1, 2 * TileHeight - 1))
                {
                    PullUpLeft();
                    affordance = BeatAffordance.Grounded;
                    return true;
                }
                // Attempt to jump into a grab.
                for (int step = TileHeight + 1; step <= maxPullupHeight; step++)
                {
                    if (colliders[1, step - 1] && !colliders.AnyInside(1, step, 1, step + TileHeight - 1))
                    {
                        affordance = BeatAffordance.HangingLeft;
                        currentAnimations.Clear();
                        currentAnimations.Enqueue(ActorAnimationsGenerator.CreateJumpUpPath(step - TileHeight));
                        return true;
                    }
                }
                return false;
            }
            bool TryWalkLeft()
            {
                // Is there a wall preventing left movement?
                if (!colliders.AnyInside(-1, 0, -1, TileHeight - 1) &&
                    colliders[-1, -1])
                {
                    // Move to the left.
                    affordance = BeatAffordance.Grounded;
                    currentAnimations.Clear();
                    currentAnimations.Enqueue(ActorAnimationsGenerator.CreateWalkPath(-1));
                    return true;
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
                        affordance = BeatAffordance.Grounded;
                        currentAnimations.Clear();
                        currentAnimations.Enqueue(ActorAnimationsGenerator.CreateWalkPath(-1, step));
                        return true;
                    }
                }
                // Attempt to do a step down.
                for (int step = -1; step >= -autoStepHeight; step--)
                {
                    if (colliders[-1, step - 1] && !colliders.AnyInside(-1, step, -1, step + TileHeight - 1))
                    {
                        affordance = BeatAffordance.Grounded;
                        currentAnimations.Clear();
                        currentAnimations.Enqueue(ActorAnimationsGenerator.CreateWalkPath(-1, step));
                        return true;
                    }
                }
                return false;
            }
            bool TryJumpLeft()
            {
                // Check by row to find the highest jump.
                for (int y = 0; y >= -maxDropDistance; y--)
                {
                    for (int x = -1; x >= -jumpDistance; x--)
                    {
                        // Is the landing space obstructed?
                        if (colliders[x, y]) break;
                        // Is there a place to land on?
                        if (x <= -3 && colliders[x, y - 1])
                        {
                            currentAnimations.Clear();
                            foreach (ActorAnimationPath path in ActorAnimationsGenerator.CreateJumpPaths(x, y, jumpApex))
                                currentAnimations.Enqueue(path);
                            affordance = BeatAffordance.JumpApex;
                            return true;
                        }
                    }
                }
                return false;
            }
            bool TryJumpGrabLeft()
            {
                // Attempt to do an instant grab up.
                if (colliders[-1, TileHeight - 1] && !colliders.AnyInside(-1, TileHeight, -1, 2 * TileHeight - 1))
                {
                    PullUpRight();
                    affordance = BeatAffordance.Grounded;
                    return true;
                }
                // Attempt to jump into a grab.
                for (int step = TileHeight + 1; step <= maxPullupHeight; step++)
                {
                    if (colliders[-1, step - 1] && !colliders.AnyInside(-1, step, -1, step + TileHeight - 1))
                    {
                        affordance = BeatAffordance.HangingRight;
                        currentAnimations.Clear();
                        currentAnimations.Enqueue(ActorAnimationsGenerator.CreateJumpUpPath(step - TileHeight));
                        return true;
                    }
                }
                return false;
            }
            #endregion
        }

        protected override sealed void Update()
        {
            base.Update();

            if (Application.isPlaying)
            {
                // Are there animations to execute
                // during this beat?
                if (currentAnimations.Count > 0)
                {
                    // Request the next animation location.
                    Vector2 toLocation = currentAnimations.Peek()
                        (beatService.CurrentInterpolant);
                    // Apply the translation to the actor.
                    World.TranslateActor(this, toLocation - lastAnimationFrame);
                    lastAnimationFrame = toLocation;
                }
            }
        }

        public void ApplyDamage(float amount)
        {
            
        }
    }

}
