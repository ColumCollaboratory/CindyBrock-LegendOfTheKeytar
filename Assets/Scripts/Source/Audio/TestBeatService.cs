using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace BattleRoyalRhythm.Audio
{
    public sealed class TestBeatService : BeatService
    {
        [Tooltip("The BPM to simulate.")]
        [SerializeField][Min(1f)] private float beatsPerMinute = 60f;


        private float lastBeatTime = 0f;

        public override float CurrentInterpolant => Time.fixedTime - lastBeatTime / (60f / beatsPerMinute);

        public override event BeatElapsedHandler BeatElapsed;


        private void FixedUpdate()
        {
            if (CurrentInterpolant >= 1f)
            {
                lastBeatTime += 60f / beatsPerMinute;
                BeatElapsed?.Invoke(lastBeatTime);
            }
        }
    }
}
