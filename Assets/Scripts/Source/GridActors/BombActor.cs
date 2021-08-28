using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace BattleRoyalRhythm.GridActors
{
    public sealed class BombActor : ProjectileActor
    {
        [SerializeField][Min(0)] private int explosionRadius = 1;


    }
}
