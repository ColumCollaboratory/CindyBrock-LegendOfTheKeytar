using CindyBrock.Audio;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace CindyBrock.UI
{
    public enum JukeboxLoopMode : byte
    {
        StopAfterSongFinishes,
        LoopSongsLinear,
        LoopSongsShuffle
    }

    public interface IJukebox
    {
        void Stop();
        void Play();
        void SetTrack(int trackID);
        void ToggleTrackAutoplay(int trackID, bool isAutoPlay);
        JukeboxLoopMode LoopMode { get; set; }

        float CurrentTrackDuration { get; }
        float CurrentTrackTime { get; set; }
    }

    public abstract class JukeboxBase : MonoBehaviour, IJukebox
    {


        private JukeboxLoopMode loopMode;
        public JukeboxLoopMode LoopMode
        {
            get => loopMode;
            set
            {
                if (loopMode != value)
                {
                    loopMode = value;
                    OnLoopModeChanged();
                }
            }
        }

        public float CurrentTrackDuration { get; }
        public float CurrentTrackTime { get; set; }

        protected abstract void OnLoopModeChanged();
        public abstract void Play();
        public abstract void SetTrack(int trackID);
        public abstract void Stop();

        public abstract void ToggleTrackAutoplay(int trackID, bool isAutoPlay);
    }

    [Serializable]
    public struct SoundtrackMetaData
    {
        [Tooltip("The signal to be sent to Wwise to trigger this track.")]
        public string wwiseStateName;
        [Header("User Visible Info")]
        [Tooltip("The name of the track.")]
        public string trackName;
        [Tooltip("The artists for the track.")]
        public string artists;
    }
}
