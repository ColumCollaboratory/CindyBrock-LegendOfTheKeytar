using UnityEngine;

namespace BattleRoyalRhythm.Audio
{
    /// <summary>
    /// Implements the beat service using the
    /// Fixed Update loop for timing.
    /// </summary>
    public sealed class FixedUpdateBeatService : BeatService
    {
        #region Inspector Fields
        [Tooltip("The BPM the beat service is timed to.")]
        [SerializeField][Min(1f)] private float beatsPerMinute = 60f;
        #endregion
        #region Beat Interval State
        private float lastBeatTime = 0f;
        #endregion
        #region Beat Service Events
        public override sealed event BeatElapsedHandler BeatElapsed;
        #endregion
        #region Beat Service Properties
        public override sealed float CurrentInterpolant =>
            (Time.fixedTime - lastBeatTime) / (60f / beatsPerMinute);
        #endregion
        #region Fixed Update Cycle
        private void FixedUpdate()
        {
            // Check if a beat has elapsed.
            if (CurrentInterpolant >= 1f)
            {
                // Increment the elapsed beat and
                // notify listeners of the service.
                lastBeatTime += 60f / beatsPerMinute;
                BeatElapsed?.Invoke(lastBeatTime);
            }
        }
        #endregion
    }
}
