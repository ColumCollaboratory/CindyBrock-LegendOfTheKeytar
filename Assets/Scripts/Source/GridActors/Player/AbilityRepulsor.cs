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

        public override void StartUsing(int beatCount)
        {
            
        }
    }
}
