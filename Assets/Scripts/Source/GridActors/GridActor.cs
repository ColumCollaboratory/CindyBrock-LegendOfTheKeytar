using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using BattleRoyalRhythm.Surfaces;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace BattleRoyalRhythm.GridActors
{
    /// <summary>
    /// Scene instance for the base class of grid actors.
    /// </summary>
#if UNITY_EDITOR
    [ExecuteInEditMode]
#endif
    public abstract class GridActor : MonoBehaviour
    {
#if UNITY_EDITOR
        #region Scene Editing State
        // Store the locked transform values.
        private Vector3 currentPosition;
        private Quaternion currentRotation;
        #endregion
        #region Enforced Transform Lock
        private void OnEnable()
        {
            // Conceal the transform.
            transform.hideFlags = HideFlags.HideInInspector;
        }
        private void Reset()
        {
            // Conceal the transform.
            transform.hideFlags = HideFlags.HideInInspector;
        }
        private void Update()
        {
            // Lock out any value changes.
            if (transform.position != currentPosition)
                transform.position = currentPosition;
            if (transform.rotation != currentRotation)
                transform.rotation = currentRotation;
            if (transform.localScale != Vector3.one)
                transform.localScale = Vector3.one;
        }
        private void OnDisable()
        {
            // Reveal the transform.
            transform.hideFlags = HideFlags.None;
        }
        #endregion

        public void RefreshPosition()
        {
            location.x = Mathf.Clamp(location.x, 1, currentSurface.LengthX);
            location.y = Mathf.Clamp(location.y, 1, currentSurface.LengthY);

            Surfaces.Surface surface = currentSurface;

            Vector2 newLoc = new Vector2(location.x - 0.5f, location.y - 0.5f);
            currentPosition = surface.GetLocation(newLoc);
            currentRotation = Quaternion.LookRotation(surface.GetRight(newLoc), surface.GetUp(newLoc));
            transform.position = currentPosition;
            transform.rotation = currentRotation;
        }

        protected virtual void OnValidate()
        {
            if (currentSurface != null)
                RefreshPosition();
        }
#endif

        [Tooltip("The current surface that this actor is on.")]
        [SerializeField] private Surface currentSurface = null;
        [Tooltip("The location of the actor on this surface.")]
        [SerializeField] private Vector2Int location = Vector2Int.zero;
    }
}
