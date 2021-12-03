using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace BattleRoyalRhythm.GridActors.Behaviours
{
    public sealed class DescentAction : ActorAction
    {
        #region Serialized Fields
        [Header("Jump Strength")]
        [Tooltip("The maximum height that this actor will voluntarily fall.")]
        [SerializeField][Min(0)] private int maxVoluntaryFallHeight = 5;
        [Header("Fall Duration")]
        [Tooltip("Controls how many beats it takes to fall.")]
        [SerializeField][Min(0f)] private float fallSpeedFactor = 1f;
        #endregion

        public override IActionContext GetActionContext(GridActor withActor)
        {
            throw new NotImplementedException();
        }

        public override void AdvanceActionBeat(ref IActionContext context)
        {
            throw new NotImplementedException();
        }


        public override Vector2 GetActionDelta(ref IActionContext context, float interpolant)
        {
            throw new NotImplementedException();
        }
    }
}
