using BattleRoyalRhythm.Audio;
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
                Destroyed?.Invoke(this);
                World.Actors.Remove(this);
                Destroy(gameObject);
            }
        }
    }
}
