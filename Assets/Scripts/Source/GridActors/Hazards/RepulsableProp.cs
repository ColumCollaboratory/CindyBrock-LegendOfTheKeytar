using System.Collections.Generic;
using UnityEngine;

namespace CindyBrock.GridActors.Hazards
{

    public class RepulsableProp : GridActor, IKnockbackable
    {

        ActorAnimationPath repulsedPath;

        private Vector2 lastStep;

        public void ApplyKnockback(int knockbackX, int knockbackY)
        {
            Debug.Log("Knockback Applied");
            // Find the knockback distance that can be traveled.
            NearbyColliderSet colliders = World.GetNearbyColliders(
                this, 10, 10, new List<GridActor>() { this });

            int travel = 0;
            if (knockbackX > 0)
            {
                for (int x = 1; x <= knockbackX; x++)
                {
                    if (!colliders.AnyInside(x, 0, x, TileHeight - 1))
                        travel = x;
                    else break;
                }
            }
            else
            {
                for (int x = -1; x >= knockbackX; x--)
                {
                    if (!colliders.AnyInside(x, 0, x, TileHeight - 1))
                        travel = x;
                    else break;
                }
            }

            if (travel != 0)
            {
                if (knockbackX > 0)
                {
                    repulsedPath = (float t) =>
                    {
                        return new Vector2(Mathf.Min(travel, t * knockbackX), 0f);
                    };
                }
                else
                {
                    repulsedPath = (float t) =>
                    {
                        return new Vector2(Mathf.Max(travel, t * knockbackX), 0f);
                    };
                }
                World.BeatService.BeatElapsed += OnBeatElapsed;
                lastStep = Vector2.zero;
            }

        }

        private void OnBeatElapsed(float beatTime)
        {
            Vector2 targetLocation = repulsedPath(1f);
            World.TranslateActor(this, targetLocation - lastStep);
            World.BeatService.BeatElapsed -= OnBeatElapsed;
            repulsedPath = null;
        }

        protected virtual void Update()
        {
            if (repulsedPath != null)
            {
                Vector2 targetLocation = repulsedPath(World.BeatService.CurrentInterpolant);
                World.TranslateActor(this, targetLocation - lastStep);
                lastStep = targetLocation;
            }
        }
    }
}
