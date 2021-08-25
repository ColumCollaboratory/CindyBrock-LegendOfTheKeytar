using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace BattleRoyalRhythm.GridActors.Player
{
    public abstract class PlayerAbility : MonoBehaviour
    {

        public abstract bool IsUsable();

        public abstract void UseAbility();

    }
}
