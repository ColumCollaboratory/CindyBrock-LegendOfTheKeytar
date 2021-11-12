namespace BattleRoyalRhythm.Audio
{
    #region Beat Service Handlers
    /// <summary>
    /// An action that is performed when a song beat elapses.
    /// </summary>
    /// <param name="beatTime">The exact time of the beat.</param>
    public delegate void BeatElapsedHandler(float beatTime);
    #endregion

    /// <summary>
    /// Provides an update routine that is called every time
    /// a beat elapses in a playing song.
    /// </summary>
    public interface IBeatService
    {
        #region Beat Events Implemented
        /// <summary>
        /// Called shortly following an elapsed beat.
        /// </summary>
        event BeatElapsedHandler BeatElapsed;
        #endregion
        #region Beat Properties Implemented
        /// <summary>
        /// The current 0-1 interpolant between beats.
        /// </summary>
        float CurrentInterpolant { get; }
        // NOTE this is not overflow-proof.
        /// <summary>
        /// The current beat that this service is on since starting.
        /// </summary>
        int CurrentBeatCount { get; }
        /// <summary>
        /// The current seconds per beat of the service.
        /// </summary>
        float SecondsPerBeat { get; }
        /// <summary>
        /// Controls the offset for when beats are processed
        /// relative to the start of the audio track.
        /// </summary>
        float BeatOffset { get; set; }
        #endregion
        #region Methods Implemented
        /// <summary>
        /// Sets the soundtrack to be used for the beat.
        /// </summary>
        /// <param name="set">The soundtrack set.</param>
        void SetBeatSoundtrack(SoundtrackSet set);
        #endregion
    }
}
