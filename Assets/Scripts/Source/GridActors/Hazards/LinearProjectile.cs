using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace BattleRoyalRhythm.GridActors.Hazards
{
    public sealed class LinearProjectile : ProjectileActor
    {
        [Tooltip("The number of tiles this projectile travels in a beat.")]
        [SerializeField][Min(1)] private int tilesPerBeat = 2;

        [SerializeField] private Transform projectilePivot = null;

        private List<GridActor> tentativeTargets;
        private int enemiesHit;

        private ActorAnimationPath currentPath = null;
        Vector2 lastFramePath;
        private bool willDestroy;
        private float destroyInterpolant;

        protected override void OnDirectionChanged(Direction direction)
        {
            float scaleX = 1;
            switch (direction)
            {
                case Direction.Left:
                    scaleX = -1; break;
                case Direction.Right:
                    scaleX = 1; break;
            }
            projectilePivot.transform.localScale = new Vector3(scaleX, 1, 1);
        }

        protected override void Awake()
        {
            base.Awake();
            tentativeTargets = new List<GridActor>();
            enemiesHit = 0;
            willDestroy = false;
        }

        protected override void OnBeatElapsed(float beatTime)
        {

            // Finalize last frame animation.
            if (currentPath != null)
            {
                World.TranslateActor(this, currentPath(1f) - lastFramePath);
                lastFramePath = Vector2.zero;
            }
            int nextMove = Direction is Direction.Right ?
                tilesPerBeat : -tilesPerBeat;
            // Scan for a collider that might destroy this projectile early.
            // TODO this was written quickly AND dioes not
            // properly account for damaging actors in the middle
            // of a beat (should have idle check?)
            NearbyColliderSet colliders = World.GetNearbyColliders(
                this, tilesPerBeat, 0, World.Actors);
            if (nextMove > 0)
            {
                for (int i = 1; i <= nextMove; i++)
                {
                    if (colliders[i, 0, CollisionDirectionMask.Right])
                    {
                        willDestroy = true;
                        destroyInterpolant = (i - 0.5f) / nextMove;
                        break;
                    }
                }
            }
            else
            {
                for (int i = -1; i >= nextMove; i--)
                {
                    if (colliders[i, 0, CollisionDirectionMask.Left])
                    {
                        willDestroy = true;
                        destroyInterpolant = (i - 0.5f) / nextMove;
                        break;
                    }
                }
            }

            // Create the next path.
            currentPath = ActorAnimationsGenerator.CreateWalkPath(nextMove);

            // Check for collisions where the projectile
            // passed through since last frame.
            int minStep = nextMove > 0 ? 1 : -1;
            int maxStep = nextMove > 0 ? nextMove - 1 : nextMove + 1;
            bool isMidStep = Mathf.Abs(maxStep) >= Mathf.Abs(minStep);
            if (isMidStep)
            {
                List<GridActor> trailingActors = World.GetIntersectingActors(
                    CurrentSurface, Tile.x - minStep, Tile.y, Tile.x - maxStep, Tile.y + TileHeight - 1,
                    IgnoredActors);
                foreach (GridActor trailingActor in trailingActors)
                {
                    if (trailingActor is IDamageable damageable &&
                        tentativeTargets.Contains(trailingActor))
                    {
                        damageable.ApplyDamage(damage);
                        enemiesHit++;
                        if (enemiesHit > pierce)
                        {
                            DestroyProjectile();
                            return;
                        }
                    }
                }
            }
            tentativeTargets.Clear();
            // Check for collisions with actors where
            // the projectile currently is.
            List<GridActor> hitActors = World.GetIntersectingActors(
                CurrentSurface, Tile.x, Tile.y, Tile.x, Tile.y + TileHeight - 1,
                IgnoredActors);
            foreach (GridActor actor in hitActors)
            {
                if (actor is IDamageable damageable)
                {
                    damageable.ApplyDamage(damage);
                    enemiesHit++;
                    if (enemiesHit > pierce)
                    {
                        DestroyProjectile();
                        return;
                    }
                }
            }
            // Get the actors that we expect to pass through
            // during the next beat.
            if (isMidStep)
            {
                List<GridActor> speculativeActors = World.GetIntersectingActors(
                    CurrentSurface, Tile.x + minStep, Tile.y, Tile.x + maxStep, Tile.y + TileHeight - 1,
                    IgnoredActors);
                foreach (GridActor speculativeActor in speculativeActors)
                    if (speculativeActor is IDamageable damageable)
                        tentativeTargets.Add(speculativeActor);
            }
        }

        private void DestroyProjectile()
        {
            World.BeatService.BeatElapsed -= OnBeatElapsed;
            World.Actors.Remove(this);
            Destroy(gameObject);
        }

        private void Update()
        {
            if (currentPath != null)
            {
                if (willDestroy && World.BeatService.CurrentInterpolant > destroyInterpolant)
                {
                    DestroyProjectile();
                    currentPath = null;
                }
                else
                {
                    Vector2 thisFramePath = currentPath(World.BeatService.CurrentInterpolant);
                    World.TranslateActor(this, thisFramePath - lastFramePath);
                    lastFramePath = thisFramePath;
                }
            }
        }
    }
}
