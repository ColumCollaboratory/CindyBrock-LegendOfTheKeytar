using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using BattleRoyalRhythm.Surfaces;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace BattleRoyalRhythm.GridActors
{

    public delegate void SurfaceChangedHandler(Surface newSurface);


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
        protected virtual void Update()
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
            location.x = Mathf.Clamp(location.x, 0.5f, currentSurface.LengthX + 0.5f);
            location.y = Mathf.Clamp(location.y, 0.5f, currentSurface.LengthY + 0.5f);

            Vector2 newLoc = new Vector2(location.x - 0.5f, location.y - 0.5f);
            currentPosition = currentSurface.GetLocation(newLoc);
            currentRotation = Quaternion.LookRotation(currentSurface.GetRight(newLoc), currentSurface.GetUp(newLoc));
            transform.position = currentPosition;
            transform.rotation = currentRotation;
        }

        protected virtual void OnValidate()
        {
            location = Tile;
            if (currentSurface != null)
                RefreshPosition();
        }
#else
        protected virtual void OnValidate() { }
        protected virtual void Update() { }
#endif

        protected virtual void Awake()
        {
            Location = tile;
        }

        [Tooltip("The current surface that this actor is on.")]
        [SerializeField] private Surface currentSurface = null;
        [Tooltip("The tile location of the actor on this surface.")]
        [SerializeField] private Vector2Int tile = Vector2Int.zero;


        public event SurfaceChangedHandler SurfaceChanged;

        public Surface CurrentSurface
        {
            get => currentSurface;
            set
            {
                if (value != currentSurface)
                {
                    currentSurface = value;
                    SurfaceChanged?.Invoke(value);
                }
            }
        }

        public Vector2Int Tile => tile;

        private Vector2 location;

        public Vector2 Location
        {
            get => location;
            set
            {
                if (location != value)
                {
                    location = value;
                    tile = new Vector2Int(Mathf.RoundToInt(location.x), Mathf.RoundToInt(location.y));
                    RefreshPosition();
                }
            }
        }

        [HideInInspector] public GridWorld World;
    }
}
