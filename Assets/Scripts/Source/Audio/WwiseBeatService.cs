using System;
using UnityEngine;

namespace BattleRoyalRhythm.Audio
{
    /// <summary>
    /// Implements the beat service using an
    /// established connection to Wwise.
    /// </summary>
    public sealed class WwiseBeatService : BeatService
    {
        public override float CurrentInterpolant => throw new NotImplementedException();

        public override event BeatElapsedHandler BeatElapsed;
    }
}
