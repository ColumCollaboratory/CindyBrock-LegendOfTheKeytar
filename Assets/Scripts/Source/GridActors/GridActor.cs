using System;
using UnityEngine;
using BattleRoyalRhythm.Surfaces;
using Tools;

namespace BattleRoyalRhythm.GridActors
{
    [Flags]
    public enum ActorTag
    {
        Nothing = 0,
        Hero = 1,
        Passive = 2,
        Villain = 4,
        Intangible = 8
    }

    [Flags]
    public enum CollisionDirectionMask
    {
        Nothing = 0,
        Left = 1,
        Right = 2,
        Up = 4,
        Down = 8,
        Everything = 15
    }

    public enum Direction : byte
    {
        Left,
        Right
    }

    /// <summary>
    /// Called whenever this actor's surface changes.
    /// </summary>
    /// <param name="newSurface"></param>
    public delegate void SurfaceChangedHandler(Surface newSurface);
    /// <summary>
    /// Called when this grid actor has been removed from
    /// interactions on the grid.
    /// </summary>
    /// <param name="actor"></param>
    public delegate void ActorRemoved(GridActor actor);

    /// <summary>
    /// Scene instance for the base class of grid actors.
    /// </summary>
#if UNITY_EDITOR
    [ExecuteInEditMode]
#endif
    public abstract class GridActor : MonoBehaviour
    {



        [Header("Actor Interaction")]
        [Tooltip("The tags associated with this actor.")]
        [SerializeField] protected ActorTag tags = default;
        [Tooltip("The tags that this actor blocks in collisions.")]
        [SerializeField] protected ActorTag blocksTags = default;
        [Tooltip("The directions that this actor blocks in.")]
        [SerializeField] protected CollisionDirectionMask blocksDirections = default;
        [Header("Surface Positioning")]
        [Tooltip("The current surface that this actor is on.")]
        [SerializeField] private Surface currentSurface = null;
        [Tooltip("The tile location of the actor on this surface.")]
        [SerializeField] private Vector2Int tile = Vector2Int.zero;
        [Tooltip("The vertical height of this actor.")]
        [SerializeField][Min(1)] private int tileHeight = 2;

        public ActorTag Tags => tags;
        public ActorTag BlocksTags => blocksTags;
        public CollisionDirectionMask BlocksDirections => blocksDirections;


        #region Scene Editing State
        // Store the locked transform values.
        private ProgrammedTransform programmedTransform;
        #endregion

        public void RefreshPosition()
        {
            if (currentSurface != null)
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
            if (!Application.isPlaying)
                DestroyImmediate(programmedTransform);
        }
        #endregion

        protected virtual void OnDrawGizmos()
        {

        }

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

        public virtual event ActorRemoved Destroyed;

        public int TileHeight
        {
            get => tileHeight;
            protected set => tileHeight = value;
        }

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

        public Direction Direction
        {
            get => direction;
            set
            {
                direction = value;
                OnDirectionChanged(value);
            }
        }

        protected virtual void OnDirectionChanged(Direction direction) { }

        [HideInInspector] public GridWorld World;
    }
}
