using UnityEngine;
using BattleRoyalRhythm.Surfaces;
using Tools;

namespace BattleRoyalRhythm.GridActors
{
    #region Actor State Enums
    /// <summary>
    /// Represents an actor's facing direction relative
    /// to the screen scroll.
    /// </summary>
    public enum Direction : byte
    {
        /// <summary>
        /// Actor is looking towards the left.
        /// </summary>
        Left,
        /// <summary>
        /// Actor is looking towards the right.
        /// </summary>
        Right,
        /// <summary>
        /// Actor is looking towards the screen.
        /// </summary>
        Neutral
    }
    #endregion
    #region Actor State Change Handlers
    /// <summary>
    /// Called when this actor's surface changes.
    /// </summary>
    /// <param name="newSurface">The new surface that the actor has moved to.</param>
    public delegate void SurfaceChangedHandler(Surface newSurface);
    #endregion

    /// <summary>
    /// Scene instance for the base class of grid actors.
    /// </summary>
    public abstract class GridActor : MonoBehaviour
    {
        #region Base Grid Actor Inspector Fields
        [Header("Surface Positioning")]
        [Tooltip("The current surface that this actor is on.")]
        [SerializeField] private Surface currentSurface = null;
        [Tooltip("The tile location of the actor on this surface.")]
        [SerializeField] private Vector2Int tile = Vector2Int.zero;
        [Tooltip("The vertical height of this actor.")]
        [SerializeField][Min(1)] private int tileHeight = 2;
        #endregion


        public GridWorld World { get; set; }


        #region Scene Editing State
        // Store the locked transform values.
        private ProgrammedTransform programmedTransform;
        #endregion

        /// <summary>
        /// Refreshed the position of the transform relative to
        /// the grid that this Grid Actor is on.
        /// </summary>
        public void RefreshPosition()
        {
            if (currentSurface == null)
            {
                
            }
            else
            {
                location = new Vector2(
                    Mathf.Clamp(location.x, 0.5f, currentSurface.LengthX + 0.5f),
                    Mathf.Clamp(location.y, 0.5f, currentSurface.LengthY + 0.5f));
                Vector2 newLoc = new Vector2(location.x - 0.5f, location.y - 0.5f);
                if (programmedTransform == null)
                    Initialize();
                programmedTransform.Position = currentSurface.GetLocation(newLoc);
                programmedTransform.Rotation = Quaternion.LookRotation(currentSurface.GetRight(newLoc), currentSurface.GetUp(newLoc));
            }
        }

#if UNITY_EDITOR
        #region Enforced Transform Lock
        protected virtual void OnEnable() => Initialize();
        protected virtual void Reset() => Initialize();
        private void Initialize()
        {
            programmedTransform = GetComponent<ProgrammedTransform>();
            if (programmedTransform == null)
                programmedTransform = gameObject.AddComponent<ProgrammedTransform>();
            programmedTransform.CurrentVisibility = ProgrammedTransform.Visibility.Hidden;
        }
        protected virtual void OnDestroy()
        {
            DestroyImmediate(programmedTransform);
        }
        #endregion


        protected virtual void OnValidate()
        {
            location = Tile;
            if (currentSurface != null)
            {
                Location = new Vector2(
                    Mathf.Clamp(location.x, 1f, currentSurface.LengthX),
                    Mathf.Clamp(location.y, 1f, currentSurface.LengthY));
                RefreshPosition();
            }
        }
#else
        protected virtual void OnValidate() { }
#endif

        public virtual event ActorRemovedHandler Destroyed;

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
                    RefreshPosition();
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
                location = value;
                tile = new Vector2Int(Mathf.RoundToInt(location.x), Mathf.RoundToInt(location.y));
                RefreshPosition();
            }
        }

        private Direction direction;

        /// <summary>
        /// The direction that the actor is facing relative to the grid.
        /// </summary>
        public Direction Direction
        {
            get => direction;
            set
            {
                if (value != direction)
                {
                    direction = value;
                    OnDirectionChanged(value);
                }
            }
        }

        /// <summary>
        /// Override this method to respond to a change in actor orientation.
        /// </summary>
        /// <param name="direction">The new direction.</param>
        protected virtual void OnDirectionChanged(Direction direction) { }
    }
}
