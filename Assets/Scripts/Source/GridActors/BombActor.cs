using BattleRoyalRhythm.Audio;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BattleRoyalRhythm.GridActors
{


    public sealed class BombActor : ProjectileActor
    {


        public override sealed event ActorRemoved Destroyed;

        [SerializeField][Min(1)] private int explosionRadius = 1;
        [SerializeField][Min(0)] private int enemyDamage = 5;
        [SerializeField][Min(0)] private int enemyStunBeats = 0;
        [SerializeField][Min(0)] private int enemyKnockback = 0;
        [SerializeField][Min(1)] private int beatsUntilExplosion = 3;

        [SerializeField] private Animator animator = null;
        [SerializeField] private ParticleSystem explosionParticles = null;
        [SerializeField] private Transform bombMesh = null;



        private IBeatService beatService;
        public IBeatService BeatService
        {
            set
            {
                beatService = value;
                animator.speed = 1f / beatService.SecondsPerBeat;
                beatService.BeatElapsed += OnBeatElapsed;
            }
        }

        private void OnBeatElapsed(float beatTime)
        {
            beatsUntilExplosion--;
            if (beatsUntilExplosion <= 0)
            {
                explosionParticles.Play();


                // Check for stuff to damage.
                int x1 = Mathf.Max(0, Tile.x - explosionRadius);
                int x2 = Mathf.Min(CurrentSurface.LengthX, Tile.x + explosionRadius);
                int y1 = Mathf.Max(0, Tile.y - explosionRadius);
                int y2 = Mathf.Min(CurrentSurface.LengthY, Tile.y + explosionRadius);
                List<GridActor> actorsHit = World.GetIntersectingActors(CurrentSurface, x1, y1, x2, y2, new List<GridActor>() { this });


                foreach (GridActor actor in actorsHit)
                    if(actor is IDamageable damageActor)
                        damageActor.ApplyDamage(enemyDamage);
                beatService.BeatElapsed -= OnBeatElapsed;
                Destroyed?.Invoke(this);
                World.Actors.Remove(this);

                StartCoroutine(CleanUpParticleSystem());

                Destroy(bombMesh.gameObject);
            }
        }

        private IEnumerator CleanUpParticleSystem()
        {
            yield return new WaitForSeconds(explosionParticles.main.duration);
            Destroy(gameObject);
        }
    }
}
