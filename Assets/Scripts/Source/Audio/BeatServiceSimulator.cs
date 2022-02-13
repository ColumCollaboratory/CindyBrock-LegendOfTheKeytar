using UnityEngine;
using Tools;

namespace CindyBrock.Audio
{
    /// <summary>
    /// Simulates a beat service timing mechanism. This is
    /// used for testing only and is not directly bound to
    /// any audio solution.
    /// </summary>
    public sealed class BeatServiceSimulator : MonoBehaviour
    {
        #region Inspector Fields
        [Tooltip("The BPM to simulate.")]
        [SerializeField][Min(1f)] private float bpm = 140f;
        [Tooltip("The speed of the simulation. Can be slowed to closely observe behaviour.")]
        [SerializeField][Percent] private float simulationSpeed = 1f;
        [Tooltip("Pauses the editor the instant before beat elapsed is called.")]
        [SerializeField] private bool pauseEditorBeforeBeatElapsed = false;
        [Tooltip("Pauses the editor the instant after beat elapsed is called.")]
        [SerializeField] private bool pauseEditorAfterBeatElapsed = false;
        #endregion
        #region Simulated Events
        /// <summary>
        /// Called when a simulated beat has elapsed.
        /// </summary>
        public event BeatElapsedHandler BeatElapsed;
        #endregion
        #region Simulated Properties
        /// <summary>
        /// The current interpolant from the prior beat to the next beat.
        /// </summary>
        public float CurrentInterpolant { get; private set; }
        #endregion
        #region Local Timer Fields
        private bool hasInitialized;
        private float secondsPerBeat;
        private float timeElapsed;
        #endregion
        #region Beat Timer Implementation
        private void LateUpdate()
        {
            if (!hasInitialized)
            {
                // Force the first beat early, but make
                // sure that Awake and Start have enabled.
                timeElapsed = secondsPerBeat;
                secondsPerBeat = 1f / (bpm / 60f);
                hasInitialized = true;
            }
        }
        private void FixedUpdate()
        {
            if (hasInitialized)
            {
                // Step time and check for an elapsed beat.
                timeElapsed += Time.fixedDeltaTime * simulationSpeed;
                if (timeElapsed > secondsPerBeat)
                {
                    // When debugging the edge state can be checked
                    // before and after this event is called.
                    if (pauseEditorBeforeBeatElapsed) Debug.Break();
                    BeatElapsed?.Invoke(Time.fixedTime - (timeElapsed - secondsPerBeat));
                    if (pauseEditorAfterBeatElapsed) Debug.Break();
                    timeElapsed -= secondsPerBeat;
                }
                // Update the simulated interpolant.
                CurrentInterpolant = timeElapsed / secondsPerBeat;
            }
        }
        #endregion
    }
}
