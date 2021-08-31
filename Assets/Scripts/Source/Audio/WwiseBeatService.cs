using System;
using UnityEngine;
using UnityEngine.InputSystem;
using AK.Wwise;

namespace BattleRoyalRhythm.Audio
{
    /// <summary>
    /// Implements the beat service using an
    /// established connection to Wwise.
    /// </summary>
    public sealed class WwiseBeatService : BeatService
    {
        private float lastFixedTime = 0f;
        private float lastInterpolant = 0f;
        private float currentInterpolant;
        public override float CurrentInterpolant => currentInterpolant;

        #region Beat Interval State
        #endregion

        public override event BeatElapsedHandler BeatElapsed;

        [SerializeField] private int playPosition = 1;

        private uint beatMusicID;

        private float millisPerBeat = 1f;

        public void SetBeatFromEvent(string eventName, float bpm)
        {
            millisPerBeat = 60000f / bpm;
            beatMusicID = AkSoundEngine.PostEvent(
                eventName, gameObject,
                (uint)AkCallbackType.AK_EnableGetSourcePlayPosition,
                WwiseCallback, null);
        }
        // This callback is needed for Wwise to wwork.
        private void WwiseCallback(object in_cookie, AkCallbackType in_type, AkCallbackInfo in_info) { }

        private void FixedUpdate()
        {
            AkSoundEngine.GetSourcePlayPosition(beatMusicID, out playPosition);

            currentInterpolant = ((playPosition + BeatOffset / 1000f) % millisPerBeat) / millisPerBeat;


            // Check if a beat has elapsed.
            if (currentInterpolant < lastInterpolant)
            {
                // Increment the elapsed beat and
                // notify listeners of the service.
                BeatElapsed?.Invoke(Mathf.Lerp(lastFixedTime, Time.fixedTime,
                    Mathf.InverseLerp(lastInterpolant, currentInterpolant + 1f, 1f)));
            }
            lastInterpolant = currentInterpolant;
            lastFixedTime = Time.fixedTime;
        }
    }
}
