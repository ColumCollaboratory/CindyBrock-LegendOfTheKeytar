using UnityEngine;

namespace BattleRoyalRhythm.Audio
{
    // NOTE this class exists so a common debugging
    // inspector can be shared between implementations.
    /// <summary>
    /// Base class for beat services implemented in MonoBehaviour.
    /// </summary>
    [DisallowMultipleComponent]
    public abstract class BeatService : MonoBehaviour, IBeatService
    {
        #region Beat Service Requirements
        public abstract float CurrentInterpolant { get; }
        public abstract event BeatElapsedHandler BeatElapsed;
        #endregion

        public float BeatOffset { get; set; }
    }
}
