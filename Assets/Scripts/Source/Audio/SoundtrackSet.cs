using System;
using UnityEngine;

namespace BattleRoyalRhythm.Audio
{
    /// <summary>
    /// Represents a set of tracks that correspond to
    /// a location in the game. Used to wrap audio logic.
    /// </summary>
    [Serializable]
    public sealed class SoundtrackSet
    {
        #region Inspector Fields
        /// <summary>
        /// The name of this soundtrack set.
        /// </summary>
        [Tooltip("The name of the soundtrack set.")]
        [SerializeField] public string name;
        [Tooltip("The BPM of this set of tracks.")]
        [SerializeField][Min(0f)] private float beatsPerMinute;
        // TODO this should be readonly somehow.
        /// <summary>
        /// The unique identifier for this set. Should
        /// not be modified outside of the soundtrack settings.
        /// </summary>
        [SerializeField][HideInInspector] public int id;
        #endregion
        #region Soundtrack Data Properties
        /// <summary>
        /// The BPM of the tracks in this set.
        /// </summary>
        public float BeatsPerMinute
        {
            get => beatsPerMinute;
            set => beatsPerMinute = Mathf.Max(0f, value);
        }
        #endregion
    }
}
