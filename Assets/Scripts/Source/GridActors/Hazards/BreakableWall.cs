using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BattleRoyalRhythm.GridActors.Hazards
{

    public sealed class BreakableWall : GridActor, IDamageable
    {
        [SerializeField][Min(0f)] private float health = 1f;

        public override event ActorRemovedHandler RemovedFromGrid;


        public void ApplyDamage(float amount)
        {
            health -= amount;
            if (health <= 0f)
            {
                World.Actors.Remove(this);
                RemovedFromGrid?.Invoke(this);
                Destroy(gameObject);
            }
        }
    }
}
