using UnityEngine;

namespace BattleRoyalRhythm.Audio
{
    /// <summary>
    /// Implements the beat service using the
    /// Fixed Update loop for timing.
    /// </summary>
    public sealed class FixedUpdateBeatService : BeatService
    {
        #region Beat Interval State
        private float beatsPerMinute;
        private bool isRunning = false;
        private float lastBeatTime = 0f;
        #endregion
        #region Beat Service Events
        public override sealed event BeatElapsedHandler BeatElapsed;
        #endregion
        #region Beat Service Properties
        public override sealed float CurrentInterpolant =>
            (Time.fixedTime - lastBeatTime) / (60f / beatsPerMinute);

        public override float SecondsPerBeat => 60f / beatsPerMinute;
        #endregion
        #region Fixed Update Cycle
        private void FixedUpdate()
        {
            // TODO bad use of bool check in update cycle;
            // Use a coroutine instead.
            if (isRunning)
            {
                // Check if a beat has elapsed.
                if (CurrentInterpolant >= 1f)
                {
                    CurrentBeatCount++;
                    // Increment the elapsed beat and
                    // notify listeners of the service.
                    lastBeatTime += 60f / beatsPerMinute;
                    BeatElapsed?.Invoke(lastBeatTime);
                }
            }
        }
        #endregion

        /// <summary>
        /// Sets the beat soundtrack (only used the BPM).
        /// </summary>
        /// <param name="set">The soundtrack set containing the target BPM.</param>
        public override sealed void SetBeatSoundtrack(SoundtrackSet set)
        {
            beatsPerMinute = set.BeatsPerMinute;
            lastBeatTime = Time.fixedTime;
            isRunning = true;
        }
    }
}
