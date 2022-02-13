using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using CindyBrock.Audio;

namespace CindyBrock.GridActors
{
    /// <summary>
    /// An actor that ticks and explodes after a set number
    /// of beats, effecting nearby actors.
    /// </summary>
    public sealed class BombActor : ProjectileActor
    {
        #region Serialized Fields
        [Tooltip("The number of tiles that the explosion effect reaches from the bomb location.")]
        [SerializeField][Min(0)] private int explosionRadius = 1;
        [Tooltip("Stun applied to nearby actors.")]
        [SerializeField][Min(0)] private int stunBeats = 0;
        [Tooltip("Knockback applied to nearby actors (away from the bomb).")]
        [SerializeField][Min(0)] private int knockbackTiles = 0;
        [Tooltip("The number of beats until explosion upon spawning.")]
        [SerializeField][Min(1)] private int beatsUntilExplosion = 3;
        [Tooltip("The animator for the bomb.")]
        [SerializeField] private Animator animator = null;
        [Tooltip("The particle system for explosions.")]
        [SerializeField] private ParticleSystem explosionParticles = null;
        [Tooltip("The mesh that contains the mesh, will be deleted when particle effects start.")]
        [SerializeField] private Transform bombMesh = null;
        #endregion

        public override sealed event ActorRemovedHandler RemovedFromGrid;

        private IBeatService beatService;
        public IBeatService BeatService
        {
            set
            {
                beatService = value;
                // Adjust the animator speed so it matches
                // the BPM of the current stage.
                animator.speed = 1f / beatService.SecondsPerBeat;
                beatService.BeatElapsed += DEPRECATED_BEAT_ELAPSED;
            }
        }

        // TODO see linear projectile actor
        // to conslidate this method to the
        // base class structure.
        private void DEPRECATED_BEAT_ELAPSED(float beatTime)
        {
            beatsUntilExplosion--;
            if (beatsUntilExplosion <= 0)
            {
                // Check for actors in the blast radius.
                // TODO this should account for cover blocking explosions.
                int x1 = Mathf.Max(0, Tile.x - explosionRadius);
                int x2 = Mathf.Min(CurrentSurface.LengthX, Tile.x + explosionRadius);
                int y1 = Mathf.Max(0, Tile.y - explosionRadius);
                int y2 = Mathf.Min(CurrentSurface.LengthY, Tile.y + explosionRadius);
                List<GridActor> actorsHit = World.GetIntersectingActors(
                    CurrentSurface, x1, y1, x2, y2, new List<GridActor>() { this });
                // Apply effects to the nearby actors.
                foreach (GridActor actor in actorsHit)
                {
                    if(actor is IDamageable damageActor)
                        damageActor.ApplyDamage(damage);
                    // TODO implement other effects here.
                }
                // Remove this actor from the context of the grid.
                RemovedFromGrid?.Invoke(this);
                World.Actors.Remove(this);
                beatService.BeatElapsed -= DEPRECATED_BEAT_ELAPSED;
                // Start the explosion effect.
                StartCoroutine(PlayParticleExplosion());
            }
        }

        private IEnumerator PlayParticleExplosion()
        {
            Destroy(bombMesh.gameObject);
            explosionParticles.Play();
            yield return new WaitForSeconds(explosionParticles.main.duration);
            Destroy(gameObject);
        }

        protected override void OnBeatElapsed(float beatTime)
        {
            
        }
    }
}
