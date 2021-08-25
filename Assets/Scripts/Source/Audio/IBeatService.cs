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
        #endregion
    }
}
