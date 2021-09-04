using BattleRoyalRhythm.Audio;
using System.Collections.Generic;
using UnityEngine;

namespace BattleRoyalRhythm.GridActors
{


    public sealed class BombActor : ProjectileActor
    {


        public override sealed event ActorDestroyed Destroyed;

        [HideInInspector] public int explosionRadius;
        [HideInInspector] public int enemyDamage;
        [HideInInspector] public int enemyStunBeats;
        [HideInInspector] public int enemyKnockback;
        [HideInInspector] public int beatsUntilExplosion;


        private IBeatService beatService;
        public IBeatService BeatService
        {
            set
            {
                beatService = value;
                beatService.BeatElapsed += OnBeatElapsed;
            }
        }

        private void OnBeatElapsed(float beatTime)
        {
            beatsUntilExplosion--;
            if (beatsUntilExplosion == 0)
            {
                // Check for stuff to damage.
                int x1 = Mathf.Max(0, Tile.x - explosionRadius);
                int x2 = Mathf.Min(CurrentSurface.LengthX, Tile.x + explosionRadius);
                int y1 = Mathf.Max(0, Tile.y - explosionRadius);
                int y2 = Mathf.Min(CurrentSurface.LengthY, Tile.y + explosionRadius);
                List<GridActor> actorsHit = World.GetIntersectingActors(CurrentSurface, x1, y1, x2, y2, new List<GridActor>() { this });


                foreach (GridActor actor in actorsHit)
                    if(actor is IDamageable damageActor)
                    {
                        damageActor.ApplyDamage(enemyDamage);
                    }

                Destroyed?.Invoke(this);
                World.Actors.Remove(this);
                Destroy(gameObject);
            }
        }
    }
}
