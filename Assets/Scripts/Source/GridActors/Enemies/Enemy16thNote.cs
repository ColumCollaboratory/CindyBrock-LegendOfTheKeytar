using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using BattleRoyalRhythm.Audio;
using Tools;
using BattleRoyalRhythm.GridActors.Player;

namespace BattleRoyalRhythm.GridActors.Enemies
{

    public sealed class Enemy16thNote : GridActor, IDamageable
    {
        private enum BehaviourState : byte
        {
            Idle,
            Walking,
            MeleeAttacking,
            TakingDamage,
            TakingKnockback,
            Dying
        }
        [Header("Animator Configuration")]
        [SerializeField] private Transform meshRootTransform = null;
        [SerializeField] private AnimatorState<BehaviourState> animator = null;

        [Header("Enemy Strength")]

        [SerializeField][Min(0)] private float health = 0f;

        [SerializeField][Min(0)] private int meleeChargeupBeats = 1;

        [SerializeField][Min(0f)] private float meleeDamage = 1f;

        [Header("Enemy State")]
        [SerializeField][ReadonlyField] private GridActor target = null;

        private BehaviourState? cachedForcedState;

        private Vector2 lastFramePath;
        private ActorAnimationPath currentPath;

        private void Start()
        {
            // TODO make base grid actor class not execute in editor,
            // causes confusion in sub classes.
            if (Application.isPlaying)
            {
                cachedForcedState = null;
                World.BeatService.BeatElapsed += OnBeatElapsed;
                animator.State = BehaviourState.Idle;
                // Look for a nearby target to approach.
                // TODO should be more generalized (maybe using
                // a utility function on grid world).
                foreach (GridActor actor in World.Actors)
                {
                    if (actor is PlayerActor player)
                    {
                        target = actor;
                        break;
                    }
                }
            }
        }

        protected override void OnDirectionChanged(Direction direction)
        {
            // TODO should be smoothed (should this be the default
            // beahviour)?
            float angle = 0f;
            switch (direction)
            {
                case Direction.Right: angle = 0f; break;
                case Direction.Left: angle = 180f; break;
            }

            meshRootTransform.localEulerAngles = Vector3.up * angle;

        }

        private void OnBeatElapsed(float beatTime)
        {
            // Finalize and clear prior beat animation.
            if (currentPath != null)
            {
                Vector2 position = currentPath(1f);
                World.TranslateActor(this, position - lastFramePath);
                currentPath = null;
            }
            // Check for the AI to change behaviour state,
            // if the actions of the prior frame force a state
            // than that state will be traversed to directly.
            if (cachedForcedState != null)
            {
                animator.State = (BehaviourState)cachedForcedState;
                cachedForcedState = null;
            }
            else
            {
                switch (animator.State)
                {
                    case BehaviourState.Idle: CheckIdleTransition(); break;
                    case BehaviourState.Walking: CheckWalkingTransition(); break;
                    case BehaviourState.MeleeAttacking: CheckMeleeAttackingTransition(); break;
                    case BehaviourState.TakingDamage: CheckTakingDamageTransition(); break;
                    case BehaviourState.TakingKnockback: CheckTakingKnockbackTransition(); break;
                    case BehaviourState.Dying: FinalizeDeath(); break;
                }
            }
            // Perform the action relating to the current state.
            // TODO these switch cases can probably be swapped out
            // for a reusable state-dictionary approach.
            switch (animator.State)
            {
                case BehaviourState.Walking: ExecuteWalk(); break;
                case BehaviourState.MeleeAttacking: ExecuteAttack(); break;
            }
        }

        private void CheckIdleTransition()
        {
            // TODO should be able to see across seams
            // (add utility method for this on grid world).
            if (target.CurrentSurface == CurrentSurface)
            {
                // Which direction is the player in?
                int direction = target.Tile.x - Tile.x;
                if (direction > 0)
                {
                    if (direction == 1 && target.Tile.y == Tile.y)
                    {
                        animator.State = BehaviourState.Idle;
                        Direction = Direction.Right;
                        cachedForcedState = BehaviourState.MeleeAttacking;
                        return;
                    }
                    else
                    {
                        // Can we walk towards the player?
                        NearbyColliderSet colliders = World.GetNearbyColliders(this, 1, 1);
                        if (colliders[1, -1, CollisionDirectionMask.Down] && !colliders.AnyInside(1, 0, 1, TileHeight - 1))
                        {
                            animator.State = BehaviourState.Walking;
                            Direction = Direction.Right;
                            return;
                        }
                    }
                }
                else if (direction < 0)
                {
                    if (direction == -1 && target.Tile.y == Tile.y)
                    {
                        animator.State = BehaviourState.Idle;
                        Direction = Direction.Left;
                        cachedForcedState = BehaviourState.MeleeAttacking;
                        return;
                    }
                    else
                    {
                        // Can we walk towards the player?
                        NearbyColliderSet colliders = World.GetNearbyColliders(this, 1, 1, World.Actors);
                        if (colliders[-1, -1, CollisionDirectionMask.Down] && !colliders.AnyInside(-1, 0, -1, TileHeight - 1))
                        {
                            animator.State = BehaviourState.Walking;
                            Direction = Direction.Left;
                            return;
                        }
                    }
                }
            }
            animator.State = BehaviourState.Idle;
        }
        private void CheckWalkingTransition()
        {
            CheckIdleTransition();
        }
        private void CheckMeleeAttackingTransition()
        {
            // One beat animation, so we will exit immediately.
            animator.State = BehaviourState.Idle;
        }
        private void CheckTakingDamageTransition()
        {
            // One beat animation, so we will exit immediately.
            animator.State = BehaviourState.Idle;
        }
        private void CheckTakingKnockbackTransition()
        {
            // One beat animation, so we will exit immediately.
            animator.State = BehaviourState.Idle;
        }
        private void FinalizeDeath()
        {
            Destroyed?.Invoke(this);
            World.BeatService.BeatElapsed -= OnBeatElapsed;
            Destroy(gameObject);
        }

        public override event ActorRemoved Destroyed;

        private void ExecuteWalk()
        {
            lastFramePath = Vector2.zero;
            if (Direction is Direction.Right)
                currentPath = ActorAnimationsGenerator.CreateWalkPath(1);
            else
                currentPath = ActorAnimationsGenerator.CreateWalkPath(-1);
        }
        private void ExecuteAttack()
        {
            // Deal damage to the actor. TODO this should be a hit
            // check- should see if the player is idle. This will
            // create execution order issues :( priority system needed? x_x
            if (target is IDamageable damageable)
                damageable.ApplyDamage(meleeDamage);
        }



        // Update is called once per frame
        private void Update()
        {
            if (currentPath != null)
            {
                Vector2 position = currentPath(World.BeatService.CurrentInterpolant);
                World.TranslateActor(this, position - lastFramePath);

                lastFramePath = position;
            }
        }

        public void ApplyDamage(float amount)
        {
            health -= amount;
            if (health < 0f)
            {
                health = 0f;
                cachedForcedState = BehaviourState.Dying;
            }
        }
    }
}
