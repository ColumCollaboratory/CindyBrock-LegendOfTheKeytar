using UnityEngine;
using UnityEngine.UI;

namespace CindyBrock.UI
{
    #region Binding Delegates
    /// <summary>
    /// Handler for when this track is selected by
    /// the user.
    /// </summary>
    /// <param name="control">The control that was selected.</param>
    public delegate void PlaylistTrackControlClickedHandler(PlaylistTrackControl control);
    #endregion

    /// <summary>
    /// Implements the UI for a clickable audio track.
    /// </summary>
    public sealed class PlaylistTrackControl : MonoBehaviour
    {
        #region Inspector Fields
        [Tooltip("The button binded to when this track is selected.")]
        [SerializeField] private Button trackSelectionButton = null;
        [Tooltip("The text for displaying the track number.")]
        [SerializeField] private Text trackNumberText = null;
        [Tooltip("The text for displaying the track title.")]
        [SerializeField] private Text trackTitleText = null;
        [Tooltip("The text for displaying the track artists.")]
        [SerializeField] private Text trackArtistsText = null;
        #endregion
        #region Control State
        private bool isSelected;
        #endregion
        #region Control Events
        /// <summary>
        /// Called when this track is selected.
        /// </summary>
        public event PlaylistTrackControlClickedHandler TrackClicked;
        #endregion
        #region Control Display Properties
        /// <summary>
        /// Whether this track is currently selected.
        /// </summary>
        public bool IsSelected
        {
            get => isSelected;
            set
            {
                isSelected = value;
                trackSelectionButton.interactable = isSelected;
            }
        }
        /// <summary>
        /// Updates the control to display new meta data for the track.
        /// </summary>
        /// <param name="trackNumber">The new track number.</param>
        /// <param name="data">The new meta data associated with the track.</param>
        public void SetMetaData(int trackNumber, SoundtrackMetaData data)
        {
            // Display with two digits.
            trackNumberText.text = trackNumber.ToString("D2");
            trackTitleText.text = data.trackName;
            trackArtistsText.text = data.artists;
        }
        #endregion
        #region Initialization
        private void Awake()
        {
            // Bind to the Unity Button.
            trackSelectionButton.onClick.AddListener(OnTrackClicked);
        }
        private void OnTrackClicked()
        {
            if (!isSelected)
                TrackClicked?.Invoke(this);
        }
        #endregion
    }
}
