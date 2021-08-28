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
        private enum PlayerState : byte
        {
            Idle,
            WalkingLeft,
            WalkingRight,
            Jumping
        }

        private PlayerState state;
        private int airborneTiles;
        private float lastFrameX;
        private float lastFrameY;

        [SerializeField][Min(0f)] private float inputTolerance = 0.1f;

        [Tooltip("Number of tiles walked in a beat.")]
        [SerializeField][Min(1)] private int walkSpeed = 1;
        [SerializeField] private AnimationCurve walkCurve = null;

        [Tooltip("Jump height apex in tiles.")]
        [SerializeField][Min(1)] private int jumpApex = 2;

        [SerializeField] private PlayerController controller = null;
        [SerializeField] private BeatService beatService = null;

        [SerializeField] private PlayerAbility[] abilities = null;

        private void OnEnable()
        {
            beatService.BeatElapsed += OnBeatElapsed;
            state = PlayerState.Idle;
        }
        private void OnDisable()
        {

            beatService.BeatElapsed -= OnBeatElapsed;
        }

        private void OnBeatElapsed(float beatTime)
        {
            NearbyColliderSet colliders = World.GetNearbyColliders(this, 2, 2);

            // Finalize state from prior action.
            switch (state)
            {
                case PlayerState.Jumping:
                    if (colliders[0, -1])
                        state = PlayerState.Idle;
                    else
                        airborneTiles++;
                    break;
                case PlayerState.WalkingLeft:
                case PlayerState.WalkingRight:
                    state = PlayerState.Idle;
                    break;
            }

            // Was the latest input timed well enough?
            if (Mathf.Abs(controller.LatestTimestamp - beatTime) < inputTolerance)
            {

                switch (controller.LatestAction)
                {
                    case PlayerAction.MoveLeft:
                        if (!colliders[-1, 0])
                        {
                            state = PlayerState.WalkingLeft;
                            lastFrameX = 0f;
                        }
                        break;
                    case PlayerAction.MoveRight:
                        if (!colliders[1, 0])
                        {
                            state = PlayerState.WalkingRight;
                            lastFrameX = 0f;
                        }
                        break;
                    case PlayerAction.Jump:
                        if (state is PlayerState.Idle)
                        {
                            state = PlayerState.Jumping;
                            airborneTiles = 0;
                            lastFrameY = 0f;
                        }
                        break;
                }
            }
        }

        protected override sealed void Update()
        {
            base.Update();

            switch (state)
            {
                case PlayerState.Jumping:
                    float t = beatService.CurrentInterpolant + airborneTiles - 1;
                    float desiredHeight = jumpApex - jumpApex * t * t;
                    World.TranslateActor(this, Vector2.up * (desiredHeight - lastFrameY));
                    lastFrameY = desiredHeight;
                    break;
                case PlayerState.WalkingLeft:
                    float t2 = beatService.CurrentInterpolant;
                    float desiredX = -walkCurve.Evaluate(t2) * walkSpeed;
                    World.TranslateActor(this, Vector2.right * (desiredX - lastFrameX));
                    lastFrameX = desiredX;
                    break;
                case PlayerState.WalkingRight:
                    float t3 = beatService.CurrentInterpolant;
                    float desiredX2 = walkCurve.Evaluate(t3) * walkSpeed;
                    World.TranslateActor(this, Vector2.right * (desiredX2 - lastFrameX));
                    lastFrameX = desiredX2;
                    break;
            }


        }

        public void ApplyDamage(float amount)
        {
            
        }
    }

}
