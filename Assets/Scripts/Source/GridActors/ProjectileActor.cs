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
        [Tooltip("Base damage multiplier for this projectile.")]
        [SerializeField][Min(0f)] protected float damageFactor = 1f;

        protected override void Awake()
        {
            base.Awake();
            IgnoredActors = new List<GridActor>();
        }

        /// <summary>
        /// The actors that this projectile does
        /// not interact with.
        /// </summary>
        public List<GridActor> IgnoredActors { get; private set; }

    }
}
