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

    public delegate void ActorDestroyed(GridActor actor);

    /// <summary>
    /// Scene instance for the base class of grid actors.
    /// </summary>
#if UNITY_EDITOR
    [ExecuteInEditMode]
#endif
    public abstract class GridActor : MonoBehaviour
    {
        #region Scene Editing State
        // Store the locked transform values.
        private Vector3 currentPosition;
        private Quaternion currentRotation;
        #endregion
        public void RefreshPosition()
        {
            if (currentSurface != null)
            {
                location.x = Mathf.Clamp(location.x, 0.5f, currentSurface.LengthX + 0.5f);
                location.y = Mathf.Clamp(location.y, 0.5f, currentSurface.LengthY + 0.5f);
                Vector2 newLoc = new Vector2(location.x - 0.5f, location.y - 0.5f);
                currentPosition = currentSurface.GetLocation(newLoc);
                currentRotation = Quaternion.LookRotation(currentSurface.GetRight(newLoc), currentSurface.GetUp(newLoc));
                transform.position = currentPosition;
                transform.rotation = currentRotation;
            }
        }
#if UNITY_EDITOR
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


        public virtual event ActorDestroyed Destroyed;

        protected virtual void Awake()
        {
            Location = tile;
        }

        [Tooltip("The current surface that this actor is on.")]
        [SerializeField] private Surface currentSurface = null;
        [Tooltip("The tile location of the actor on this surface.")]
        [SerializeField] private Vector2Int tile = Vector2Int.zero;
        [Tooltip("The vertical height of this actor.")]
        [SerializeField][Min(1)] private int tileHeight = 2;

        public int TileHeight => tileHeight;

        public event SurfaceChangedHandler SurfaceChanged;

        public virtual bool IsIntersecting(Vector2Int checkTile)
        {
            return
                checkTile.x == tile.x &&
                checkTile.y >= tile.y &&
                checkTile.y <= tile.y + tileHeight - 1;
        }

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

        private bool isRightFacing;

        public bool IsRightFacing
        {
            get => isRightFacing;
            set
            {
                if (value != isRightFacing)
                {
                    isRightFacing = value;
                    OnDirectionChanged(value);
                }
            }
        }

        protected virtual void OnDirectionChanged(bool isRightFacing) { }

        [HideInInspector] public GridWorld World;
    }
}
