using UnityEngine;
using BattleRoyalRhythm.Audio;
using Tools;
using BattleRoyalRhythm.GridActors.Player;
using System.Collections;
using System.Collections.Generic;

namespace BattleRoyalRhythm.GridActors.Enemies
{
    public sealed class EnemyTriangle : GridActor, IDamageable
    {
        private enum State : byte
        {
            Idle,
            Attacking,
            TakingDamage,
            Dying
        }

        [Header("Actor Animation")]
        [SerializeField] private Animator animator = null;
        [SerializeField] private AnimatorState<State> animatorBinding = null;
        [SerializeField] private Transform animationPivot = null;

        [Header("Actor Health")]
        [SerializeField][Min(0f)] private float enemyHealth = 10f;

        [Header("Behavior Parameters")]
        // TODO should not need to specify target.
        [SerializeField] private GridActor target = null;
        // TODO use object pooling for projectiles.
        [SerializeField] private GameObject projectileTemplate = null;
        [SerializeField] private Bounds1DInt comfortableDistance = new Bounds1DInt(1, 8);
        [SerializeField] private Bounds1DInt comfortableElevation = new Bounds1DInt(0, 1);
        [SerializeField][Min(0)] private int projectileCooldownBeats = 3;
        [SerializeField][Min(0)] private int movementCooldownBeats = 3;
        [SerializeField][Range(0f, 1f)] private float chanceToShoot = 0.7f;
        [SerializeField][Range(0f, 1f)] private float chanceToChangeHeight = 0.2f;


        [Header("Internal Behaviour State")]
        [SerializeField][ReadonlyField] private int targetElevation = 1;
        [SerializeField][ReadonlyField] private int projectileCooldown = 0;
        [SerializeField][ReadonlyField] private int movementCooldown = 0;

        private State? forcedState;
        private ActorAnimationPath currentPath;
        private Vector2 lastFramePath;
        private int actionDuration;


        protected override void OnDirectionChanged(Direction direction)
        {
            // TODO should be smoothed (should this be the default
            // behaviour)?
            float scale = 1f;
            switch (direction)
            {
                case Direction.Right: scale = 1f; break;
                case Direction.Left: scale = -1f; break;
            }
            animationPivot.localScale = new Vector3(1, 1, scale);
        }


        private void Start()
        {
            World.BeatService.BeatElapsed += OnBeatElapsed;
            animatorBinding.State = State.Idle;
            // TODO get rid of this coroutine hack;
            // caused by script execution order issue.
            StartCoroutine(LateStartHOTFIX());
        }
        private IEnumerator LateStartHOTFIX()
        {
            yield return null;
            // Adjust the animator speed so it matches
            // the BPM of the current stage.
            animator.speed = 1f / World.BeatService.SecondsPerBeat;
        }

        protected override void OnDestroy()
        {
            World.BeatService.BeatElapsed -= OnBeatElapsed;
            base.OnDestroy();
        }

        // TODO this is implemented more like a b-tree;
        // maybe research/abstract this sort of structure?
        private void OnBeatElapsed(float beatTime)
        {
            // Finalize prior frame animation.
            if (currentPath != null)
            {
                Vector2 path = currentPath(1f);
                World.TranslateActor(this, path - lastFramePath);
                currentPath = null;
                lastFramePath = Vector2.zero;
            }

            // Tick cooldowns.
            if (movementCooldown > 0) movementCooldown--;
            if (projectileCooldown > 0) projectileCooldown--;

            // If an interaction forces this actors
            // state than ignore other logic and
            // go directly to that state.
            if (forcedState != null)
            {
                animatorBinding.State = (State)forcedState;
                animator.SetTrigger("Action Executed");
                actionDuration = 1;
                forcedState = null;
                return;
            }

            actionDuration--;
            // TODO ABSOLUTELY HORRENDOUS HOTFIX
            // should use switch or tree structure
            if (actionDuration > 0)
            {
                // Spawn a bullet.
                ProjectileActor newProjectile = Instantiate(projectileTemplate).
                    GetComponent<ProjectileActor>();
                newProjectile.CurrentSurface = CurrentSurface;
                newProjectile.Location = Tile;
                newProjectile.Direction = Direction;
                newProjectile.IgnoredActors.Add(this);
                newProjectile.InitalizeProjectile(World);
                return;
            }

            switch (animatorBinding.State)
            {
                case State.Attacking:
                case State.TakingDamage:
                    animatorBinding.State = State.Idle;
                    return;
                case State.Dying:
                    return;
            }

            // TODO rationalize this magic number for how many
            // tiles need to be checked vertically.
            int y = 10;
            // First determine our current elevation.
            NearbyColliderSet colliders = World.GetNearbyColliders(this, 1, y);
            int elevation = 0;
            for (int i = 1; i < y; i++)
            {
                if (colliders[0, -i, CollisionDirectionMask.Down])
                    break;
                elevation++;
            }
            // Is the player on the surface?
            if (target.CurrentSurface == CurrentSurface)
            {
                // Turn towards the player.
                int dX = Tile.x - target.Tile.x;
                if (dX <= 0)
                    Direction = Direction.Right;
                else
                    Direction = Direction.Left;
                // Can we shoot at the player?
                if (projectileCooldown == 0 &&
                    Tile.y >= target.Tile.y &&
                    Tile.y <= target.Tile.y + target.TileHeight - 1 &&
                    Random.Range(0f, 1f) < chanceToShoot)
                {
                    animatorBinding.State = State.Attacking;
                    animator.SetTrigger("Action Executed");
                    actionDuration = 2;
                    projectileCooldown = projectileCooldownBeats;
                    return;
                }
                // Can we get to a more comfortable distance
                // from the player?
                if (movementCooldown == 0)
                {
                    int desiredMove = 0;
                    if (Mathf.Abs(dX) < comfortableDistance.Min)
                        desiredMove = dX > 0 ? 1 : -1;
                    else if (Mathf.Abs(dX) > comfortableDistance.Max)
                        desiredMove = dX < 0 ? 1 : -1;
                    // Can this move actually be executed?
                    if (colliders[desiredMove, - elevation - 1, CollisionDirectionMask.Down] &&
                        !colliders.AnyInside(desiredMove, 0, desiredMove, TileHeight - 1))
                    {
                        // Move in this direction.
                        animatorBinding.State = State.Idle;
                        actionDuration = 1;
                        currentPath = ActorAnimationsGenerator.CreateWalkPath(desiredMove);
                        movementCooldown = movementCooldownBeats;
                        return;
                    }
                }
            }
            // Change the target elevation?
            if (Random.Range(0f, 1f) < chanceToChangeHeight)
                targetElevation = Random.Range(comfortableElevation.Min, comfortableElevation.Max + 1);
            // Approach the target elevation? (no cooldown)
            if (elevation > targetElevation &&
                !colliders[0, -1, CollisionDirectionMask.Down])
            {
                animatorBinding.State = State.Idle;
                actionDuration = 1;
                currentPath = ActorAnimationsGenerator.CreateWalkPath(0, -1);
                return;
            }
            else if (elevation < targetElevation &&
                !colliders[0, 1, CollisionDirectionMask.Up])
            {
                animatorBinding.State = State.Idle;
                actionDuration = 1;
                currentPath = ActorAnimationsGenerator.CreateWalkPath(0, 1);
                return;
            }
        }

        public void ApplyDamage(float amount)
        {
            // Kill or damage the enemy.
            // Enemy can only take damage when idle.
            switch (animatorBinding.State)
            {
                case State.Idle:
                    enemyHealth -= amount;
                    if (enemyHealth <= 0f)
                    {
                        enemyHealth = 0f;
                        forcedState = State.Dying;
                    }
                    else
                        forcedState = State.TakingDamage;
                    break;
            }
        }

        private void Update()
        {
            // Update the animation of the transform.
            if (currentPath != null)
            {
                Vector2 path = currentPath(World.BeatService.CurrentInterpolant);
                World.TranslateActor(this, path - lastFramePath);
                lastFramePath = path;
            }
        }
    }
}
