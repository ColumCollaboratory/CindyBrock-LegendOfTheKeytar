using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace BattleRoyalRhythm.GridActors.Player
{
    public sealed class DashAbility : PlayerAbility
    {

        [SerializeField][Min(1)] private int dashSpaces = 2;

        public override bool IsUsable => throw new NotImplementedException();

        public override void TriggerAbility()
        {
            throw new NotImplementedException();
        }
    }
}
