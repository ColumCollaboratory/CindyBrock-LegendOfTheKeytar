using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace BattleRoyalRhythm.GridActors.Player
{
    public sealed class AbilityRepulsor : ActorAbility
    {
        [SerializeField][Min(0)] private int repulsorJumps = 1;
        [SerializeField][Min(1)] private int repulsionRadius = 1;
        [SerializeField][Min(1)] private int repulsorKnockback = 2;

        int jumpsUsed;

        public override void StartUsing(int beatCount)
        {
            jumpsUsed = 0;
        }

        protected override ActorAnimationPath UsingBeatElapsed()
        {
            StopUsing();
            NearbyColliderSet colliders = UsingActor.World.GetNearbyColliders(UsingActor, 0, 15);
            // Is the actor grounded?
            if (colliders[0, -1])
            {
                int y1 = UsingActor.Tile.y;
                int y2 = UsingActor.Tile.y + UsingActor.TileHeight;

                int x1, x2;
                if (UsingActor.IsRightFacing)
                {
                    x1 = UsingActor.Tile.x;
                    x2 = UsingActor.Tile.x + repulsionRadius;
                }
                else
                {
                    x1 = UsingActor.Tile.x - repulsionRadius;
                    x2 = UsingActor.Tile.x;
                }

                x1 = Mathf.Max(x1, 0);
                x2 = Mathf.Min(x2, UsingActor.CurrentSurface.LengthX);
                y1 = Mathf.Max(y1, 0);
                y2 = Mathf.Min(y2, UsingActor.CurrentSurface.LengthY);


                List<GridActor> affectedActors = UsingActor.World.GetIntersectingActors(
                    UsingActor.CurrentSurface, x1, y1, x2, y2, new List<GridActor> { UsingActor });

                Debug.Log(affectedActors.Count);

                foreach (IKnockbackable actor in affectedActors)
                    actor.ApplyKnockback(UsingActor.IsRightFacing ? repulsionRadius : -repulsionRadius, 0);

            }
            return null;
        }
    }
}
