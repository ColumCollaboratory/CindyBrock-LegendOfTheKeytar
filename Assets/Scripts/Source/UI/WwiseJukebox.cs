using BattleRoyalRhythm.Audio;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace BattleRoyalRhythm.UI
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

        protected abstract void OnLoopModeChanged();
        public abstract void Play();
        public abstract void SetTrack(int trackID);
        public abstract void Stop();

        public abstract void ToggleTrackAutoplay(int trackID, bool isAutoPlay);
    }

    public sealed class WwiseJukebox : JukeboxBase
    {

        private Dictionary<int, bool> autoplayEnabled;
        private Dictionary<int, string> wwiseLabels;
        private bool isPlaying;
        private int currentTrack;
        private int[] currentShuffleOrder;
        private int currentShuffleIndex;


        [SerializeField] private JukeboxLoopMode defaultLoopMode = JukeboxLoopMode.StopAfterSongFinishes;
        [SerializeField][SoundtrackID] private int[] tracks = null;

        private void Start()
        {
            autoplayEnabled = new Dictionary<int, bool>();
            foreach (int track in tracks)
            {
                autoplayEnabled.Add(track, true);
                wwiseLabels.Add(track, SoundtrackSettings.Load().GetSetByID(track).name);
            }
        }

        public override void Play()
        {
            throw new NotImplementedException();
        }

        public override void SetTrack(int trackID)
        {
            throw new NotImplementedException();
        }

        public override void Stop()
        {
            throw new NotImplementedException();
        }

        public override void ToggleTrackAutoplay(int trackID, bool isAutoPlay)
        {
            if (autoplayEnabled.ContainsKey(trackID))
                autoplayEnabled[trackID] = isAutoPlay;

            uint beatMusicID = AkSoundEngine.PostEvent(wwiseLabels[trackID], gameObject,
                (uint)AkCallbackType.AK_EnableGetSourcePlayPosition,
                WwiseCallback, null);
        }

        private void WwiseCallback(object in_cookie, AkCallbackType in_type, AkCallbackInfo in_info)
        {
        
        }

        protected override void OnLoopModeChanged()
        {
            throw new NotImplementedException();
        }
    }
}
