using BattleRoyalRhythm.Audio;
using BattleRoyalRhythm.GridActors;
using BattleRoyalRhythm.Input;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BattleRoyalRhythm.GridActors.Player
{
    public sealed class PlayerActor : GridActor, IDamageable
    {

        private bool isRightFacing;

        private bool IsRightFacing
        {
            get => isRightFacing;
            set
            {
                isRightFacing = value;
                playerMesh.localRotation = Quaternion.AngleAxis(value ? 0f : 180f, Vector3.up);
            }
        }



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

        private Queue<AnimationPath> currentAnimations;


        [SerializeField][Min(1)] private int tileHeight = 2;
        [SerializeField][Min(1)] private int duckingTileHeight = 1;
        [SerializeField] private Transform playerMesh = null;
        [SerializeField][Min(0f)] private float inputTolerance = 0.1f;

        [SerializeField] private AnimationCurve walkCurve = null;

        [Tooltip("Jump height apex in tiles.")]
        [SerializeField][Min(1)] private int jumpApex = 2;

        [SerializeField] private PlayerController controller = null;
        [SerializeField] private WwiseBeatService beatService = null;

        [SerializeField] private PlayerAbility[] abilities = null;

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
                currentAnimations = new Queue<AnimationPath>();
                beatService.BeatOffset = -inputTolerance * 0.5f;
                beatService.SetBeatFromEvent("Stage_1_Started", 140f);
                beatService.BeatElapsed += OnBeatElapsed;
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

        delegate Vector2 AnimationPath(float t);





        private Queue<AnimationPath> CreateWalkPath(int moveX, int moveY = 0)
        {
            // Store the start and end values for
            // the animation to lerp between.
            Vector2 end = new Vector2(moveX, moveY);
            Queue<AnimationPath> animations = new Queue<AnimationPath>();
            animations.Enqueue((float t) =>
            {
                // Apply walk curve, then lerp.
                t = walkCurve.Evaluate(t);
                return end * t;
            });
            return animations;
        }
        private Queue<AnimationPath> CreateJumpPaths(int jumpX, int jumpY)
        {
            // Precalculate variables for the
            // parabolic jump curve.
            float midX = Mathf.Round(0.5f * jumpX);
            float coef1, coef2;
            // Equations are swapped based on the direction
            // of the jump, coefficients are calculated here
            // so that the update time impact is minimized.
            if (jumpX > 0f)
            {
                coef1 = -jumpApex / (midX * midX);
                coef2 = (-jumpApex + jumpY) / ((jumpX - midX) * (jumpX - midX));
            }
            else
            {
                coef2 = -jumpApex / (midX * midX);
                coef1 = (-jumpApex + jumpY) / ((jumpX - midX) * (jumpX - midX)); ;
            }
            // Create the animation arc code.
            Queue<AnimationPath> animations = new Queue<AnimationPath>();
            animations.Enqueue((float t) =>
            {
                float x = midX * t;
                return new Vector2(x,
                    coef1 * (x - midX) * (x - midX) + jumpApex
                );
            });
            animations.Enqueue((float t) =>
            {
                float x = Mathf.Lerp(midX, jumpX, t);
                return new Vector2(x - midX,
                    coef2 * (x - midX) * (x - midX)
                );
            });
            return animations;
        }
        private Queue<AnimationPath> CreateInPlaceJumpPaths()
        {
            // Create the animation arc code.
            Queue<AnimationPath> animations = new Queue<AnimationPath>();
            // Left hand side of a scaled projectile motion arc.
            animations.Enqueue((float t) =>
                new Vector2(0f,
                    jumpApex - jumpApex * (1f - t) * (1f - t)
                ));
            // Right hand side of a scaled projectile motion arc.
            animations.Enqueue((float t) =>
                new Vector2(0f, -jumpApex * t * t));
            return animations;
        }
        private Queue<AnimationPath> CreatePullUpPaths(bool ledgeFacesRight)
        {
            // Store the start and end values for
            // the animation to lerp between.
            Vector2 segment1 = Vector2.up * tileHeight;
            Vector2 segment2 = ledgeFacesRight ? Vector2.left : Vector2.right;
            Queue<AnimationPath> animations = new Queue<AnimationPath>();

            float pullUpSegment = tileHeight / (tileHeight + 1f);
            animations.Enqueue((float t) =>
            {
                if (t < pullUpSegment)
                {
                    return segment1 * (t / pullUpSegment);
                }
                else
                {
                    return segment1 + segment2 * ((t - pullUpSegment) / (1f - pullUpSegment));
                }
            });
            return animations;
        }
        private Queue<AnimationPath> CreateHangDownPaths(bool ledgeFacesRight)
        {
            // Store the start and end values for
            // the animation to lerp between.
            Vector2 segment1 = ledgeFacesRight ? Vector2.right : Vector2.left;
            Vector2 segment2 = Vector2.down * tileHeight;
            Queue<AnimationPath> animations = new Queue<AnimationPath>();

            float slideOutSegment = 1f - tileHeight / (tileHeight + 1f);
            animations.Enqueue((float t) =>
            {
                if (t < slideOutSegment)
                {
                    return segment1 * (t / slideOutSegment);
                }
                else
                {
                    return segment1 + segment2 * ((t - slideOutSegment) / (1f - slideOutSegment));
                }
            });
            return animations;
        }
        private Queue<AnimationPath> CreateDropDownPaths(int moveY)
        {
            // Create the animation arc code.
            Queue<AnimationPath> animations = new Queue<AnimationPath>();
            // Right hand side of a scaled projectile motion arc.
            animations.Enqueue((float t) =>
                new Vector2(0f, moveY * t * t));
            return animations;
        }
        private Queue<AnimationPath> CreateJumpUpPaths(int moveY)
        {
            // Create the animation arc code.
            Queue<AnimationPath> animations = new Queue<AnimationPath>();
            // Left hand side of a scaled projectile motion arc.
            animations.Enqueue((float t) =>
                new Vector2(0f,
                    moveY - moveY * (1f - t) * (1f - t)
                ));
            return animations;
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
                }
            }
            // TODO this is needed to advance jump state.
            // Should be removed to conform structure.
            if (affordance is BeatAffordance.JumpApex)
                affordance = BeatAffordance.Grounded;
            void ProcessJump()
            {
                switch (affordance)
                {
                    case BeatAffordance.Grounded:
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
            #endregion
            #region State Changes
            void PullUpRight()
            {
                currentAnimations = CreatePullUpPaths(true);
                affordance = BeatAffordance.Grounded;
            }
            void PullUpLeft()
            {
                currentAnimations = CreatePullUpPaths(false);
                affordance = BeatAffordance.Grounded;
            }
            bool TryJumpUp()
            {
                // Is there room to jump?
                if (!colliders.AnyInside(0, 0, 0, tileHeight - 1 + jumpApex))
                {
                    currentAnimations = CreateInPlaceJumpPaths();
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
                        currentAnimations = CreateDropDownPaths(y + 1);
                        affordance = BeatAffordance.Grounded;
                        return true;
                    }
                }
                return false;
            }
            bool TryHangRight()
            {
                if (IsRightFacing && !colliders.AnyInside(1, tileHeight - 1, 1, -tileHeight))
                {
                    affordance = BeatAffordance.HangingRight;
                    currentAnimations = CreateHangDownPaths(true);
                    return true;
                }
                return false;
            }
            bool TryHangLeft()
            {
                if (!IsRightFacing && !colliders.AnyInside(-1, tileHeight - 1, -1, -tileHeight))
                {
                    affordance = BeatAffordance.HangingLeft;
                    currentAnimations = CreateHangDownPaths(false);
                    return true;
                }
                return false;
            }
            bool TryWalkRight()
            {
                // Is there a wall preventing right movement?
                if (!colliders.AnyInside(1, 0, 1, tileHeight - 1) &&
                    colliders[1, -1])
                {
                    // Move to the right.
                    affordance = BeatAffordance.Grounded;
                    currentAnimations = CreateWalkPath(1);
                    return true;
                }
                return false;
            }
            bool TryStepRight()
            {
                // Attempt to do a step up.
                for (int step = 1; step <= autoStepHeight; step++)
                {
                    if (colliders[1, step - 1] && !colliders.AnyInside(1, step, 1, step + tileHeight - 1))
                    {
                        affordance = BeatAffordance.Grounded;
                        currentAnimations = CreateWalkPath(1, step);
                        return true;
                    }
                }
                // Attempt to do a step down.
                for (int step = -1; step >= -autoStepHeight; step--)
                {
                    if (colliders[1, step - 1] && !colliders.AnyInside(1, step, 1, step + tileHeight - 1))
                    {
                        affordance = BeatAffordance.Grounded;
                        currentAnimations = CreateWalkPath(1, step);
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
                        if (colliders[x, y - 1])
                        {
                            currentAnimations = CreateJumpPaths(x, y);
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
                if (colliders[1, tileHeight - 1] && !colliders.AnyInside(1, tileHeight, 1, tileHeight + tileHeight - 1))
                {
                    PullUpRight();
                    affordance = BeatAffordance.Grounded;
                    return true;
                }
                // Attempt to jump into a grab.
                for (int step = tileHeight + 1; step <= maxPullupHeight; step++)
                {
                    if (colliders[1, step - 1] && !colliders.AnyInside(1, step, 1, step + tileHeight - 1))
                    {
                        affordance = BeatAffordance.HangingLeft;
                        currentAnimations = CreateJumpUpPaths(step - tileHeight);
                        return true;
                    }
                }
                return false;
            }
            bool TryWalkLeft()
            {
                // Is there a wall preventing left movement?
                if (!colliders.AnyInside(-1, 0, -1, tileHeight - 1) &&
                    colliders[-1, -1])
                {
                    // Move to the left.
                    affordance = BeatAffordance.Grounded;
                    currentAnimations = CreateWalkPath(-1);
                    return true;
                }
                return false;
            }
            bool TryStepLeft()
            {
                // Attempt to do a step up.
                for (int step = 1; step <= autoStepHeight; step++)
                {
                    if (colliders[-1, step - 1] && !colliders.AnyInside(-1, step, -1, step + tileHeight - 1))
                    {
                        affordance = BeatAffordance.Grounded;
                        currentAnimations = CreateWalkPath(-1, step);
                        return true;
                    }
                }
                // Attempt to do a step down.
                for (int step = -1; step >= -autoStepHeight; step--)
                {
                    if (colliders[-1, step - 1] && !colliders.AnyInside(-1, step, -1, step + tileHeight - 1))
                    {
                        affordance = BeatAffordance.Grounded;
                        currentAnimations = CreateWalkPath(-1, step);
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
                        if (colliders[x, y - 1])
                        {
                            currentAnimations = CreateJumpPaths(x, y);
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
                if (colliders[-1, tileHeight - 1] && !colliders.AnyInside(-1, tileHeight, -1, tileHeight + tileHeight - 1))
                {
                    PullUpLeft();
                    affordance = BeatAffordance.Grounded;
                    return true;
                }
                // Attempt to jump into a grab.
                for (int step = tileHeight + 1; step <= maxPullupHeight; step++)
                {
                    if (colliders[-1, step - 1] && !colliders.AnyInside(-1, step, -1, step + tileHeight - 1))
                    {
                        affordance = BeatAffordance.HangingRight;
                        currentAnimations = CreateJumpUpPaths(step - tileHeight);
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
