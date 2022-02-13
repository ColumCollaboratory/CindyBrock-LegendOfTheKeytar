using UnityEngine;

namespace CindyBrock.Audio
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

        [Tooltip("Controls how often the player can make an action.")]
        [SerializeField][Min(1)] private int beatsPerAction = 1;


        protected int BeatsPerAction => beatsPerAction;

        public int CurrentBeatCount { get; protected set; }
        public float BeatOffset { get; set; }

        public abstract float SecondsPerBeat { get; }

        public abstract void SetBeatSoundtrack(SoundtrackSet set);
    }
}
