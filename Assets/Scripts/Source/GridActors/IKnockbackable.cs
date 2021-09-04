using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BattleRoyalRhythm.GridActors
{
    interface IKnockbackable
    {
        void ApplyKnockback(int knockbackX, int knockbackY);
    }
}
