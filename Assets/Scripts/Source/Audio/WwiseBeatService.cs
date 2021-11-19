using System;
using UnityEngine;
using UnityEngine.InputSystem;
using AK.Wwise;
using Tools;

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

        public override float SecondsPerBeat => millisPerBeat / 1000f;

        public override event BeatElapsedHandler BeatElapsed;

        [SerializeField][ReadonlyField] private int playPosition = 1;

        private uint beatMusicID;

        private float millisPerBeat = 1f;


        // This callback is needed for Wwise to wwork.
        private void WwiseCallback(object in_cookie, AkCallbackType in_type, AkCallbackInfo in_info) { }

        private void FixedUpdate()
        {
            AkSoundEngine.GetSourcePlayPosition(beatMusicID, out playPosition);

            currentInterpolant = ((playPosition + BeatOffset / 1000f) % millisPerBeat) / millisPerBeat;


            // Check if a beat has elapsed. An epsilon value
            // is used here as a hotfix for some weird bug where
            // an interpolant is calculated as slightly less than the prior frame.
            if (lastInterpolant - currentInterpolant > 0.1f)
            {
                CurrentBeatCount++;
                // Increment the elapsed beat and
                // notify listeners of the service.
                if (CurrentBeatCount % BeatsPerAction == 0)
                {
                    BeatElapsed?.Invoke(Mathf.Lerp(lastFixedTime, Time.fixedTime,
                        Mathf.InverseLerp(lastInterpolant, currentInterpolant + 1f, 1f)));
                }
            }
            lastInterpolant = currentInterpolant;
            lastFixedTime = Time.fixedTime;
        }

        public override void SetBeatSoundtrack(SoundtrackSet set)
        {
            millisPerBeat = 60000f / set.BeatsPerMinute;
            beatMusicID = AkSoundEngine.PostEvent(
                set.name, gameObject,
                (uint)AkCallbackType.AK_EnableGetSourcePlayPosition,
                WwiseCallback, null);
        }
    }
}
