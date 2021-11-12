using BattleRoyalRhythm.Audio;
using System.Collections.Generic;
using UnityEngine;

namespace BattleRoyalRhythm.GridActors
{
    /// <summary>
    /// This grid actor can take damage from
    /// other actors.
    /// </summary>
    public interface IDamageable
    {
        void ApplyDamage(float amount);
    }

    /// <summary>
    /// Base class for projectiles that can damage
    /// other actors on the grid.
    /// </summary>
    public abstract class ProjectileActor : GridActor
    {
        [Header("Projectile Parameters")]
        [Tooltip("Base damage value for this projectile.")]
        [SerializeField][Min(0f)] protected float damage = 1f;
        [Tooltip("The number of enemies this projectile pierces.")]
        [SerializeField][Min(0)] protected int pierce = 0;

        protected BeatService BeatService { get; private set; }

        protected virtual void Awake()
        {
            IgnoredActors = new List<GridActor>();
        }

        public void InitalizeProjectile(BeatService service, GridWorld world)
        {
            World = world;
            world.Actors.Add(this);
            BeatService = service;
            service.BeatElapsed += OnBeatElapsed;
            OnBeatElapsed(0f);
        }

        protected abstract void OnBeatElapsed(float beatTime);

        /// <summary>
        /// The actors that this projectile does
        /// not interact with.
        /// </summary>
        public List<GridActor> IgnoredActors { get; private set; }

    }
}
