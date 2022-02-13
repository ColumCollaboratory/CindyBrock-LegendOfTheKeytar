using CindyBrock.Audio;
using System.Collections.Generic;
using UnityEngine;

namespace CindyBrock.GridActors
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

        protected virtual void Awake()
        {
            IgnoredActors = new List<GridActor>();
        }

        public override void InitializeGrid(GridWorld world)
        {
            base.InitializeGrid(world);
            world.Actors.Add(this);
            World.BeatService.BeatElapsed += OnBeatElapsed;
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
