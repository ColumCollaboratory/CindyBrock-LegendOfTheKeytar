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

    public sealed class WwiseJukebox : JukeboxBase
    {

        private bool[] autoplayEnabled;
        private bool isPlaying;
        private int currentTrack;
        private int[] currentShuffleOrder;
        private int currentShuffleIndex;
        private uint wwiseEventID;

        [SerializeField] private JukeboxLoopMode defaultLoopMode = JukeboxLoopMode.StopAfterSongFinishes;
        [SerializeField][SoundtrackID] private int wwiseEvent = 0;
        [SerializeField] private SoundtrackMetaData[] tracks = null;

        [SerializeField] private PlaylistTrackControl trackControlTemplate = null;

        private void Start()
        {
            autoplayEnabled = new bool[tracks.Length];
            if (tracks.Length > 0)
            {
                trackControlTemplate.SetMetaData(1, tracks[0]);
            }
            for (int i = 1; i < tracks.Length; i++)
            {
                PlaylistTrackControl clone =
                    Instantiate(trackControlTemplate.gameObject, trackControlTemplate.transform.parent).
                    GetComponent<PlaylistTrackControl>();
                clone.transform.parent = trackControlTemplate.transform.parent;
                clone.SetMetaData(i + 1, tracks[i]);
                autoplayEnabled[i] = true;
            }

            SoundtrackSettings settings = SoundtrackSettings.Load();
            SoundtrackSet even = settings.GetSetByID(wwiseEvent);

            AkSoundEngine.SetState("Soundtrack", "Boss_CartoonMix");
            wwiseEventID = AkSoundEngine.PostEvent(even.name, gameObject,
                (uint)AkCallbackType.AK_EnableGetSourcePlayPosition,
                WwiseCallback, null);
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
