using UnityEngine;
using BattleRoyalRhythm.GridActors;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace BattleRoyalRhythm.Surfaces
{
#if UNITY_EDITOR
    #region Scene Editing Delegates
    /// <summary>
    /// Handles a new mesh being generated from changed surface
    /// parameters.
    /// </summary>
    /// <param name="surface">The surface that was changed.</param>
    /// <param name="newMesh">The new mesh that was generated.</param>
    public delegate void SurfaceMeshStaleHandler(Surface surface, Mesh newMesh);
    /// <summary>
    /// Handles a surface's tile count being changed.
    /// </summary>
    /// <param name="surface">The surface that was changed.</param>
    /// <param name="newX">The new x dimension.</param>
    /// <param name="newY">The new y dimension.</param>
    public delegate void SurfaceDimensionsChangedHandler(Surface surface, int newX, int newY);
    #endregion
#endif

    /// <summary>
    /// Scene instance for the base class of surface.
    /// </summary>
#if UNITY_EDITOR
    [ExecuteInEditMode]
#endif
    [DisallowMultipleComponent]
    public abstract class Surface : MonoBehaviour
    {
#if UNITY_EDITOR
        #region Scene Editing State
        // Store the locked transform values.
        private Vector3 currentPosition;
        private Quaternion currentRotation;
        // Store the local wireframe.
        private Vector3[][] localWireframe;
        // Store prior field state so we can
        // raise an event when they change.
        private int priorLengthX;
        private int priorLengthY;
        #endregion
        #region State Editing Dispatchers
        /// <summary>
        /// Called whenever the local mesh for this
        /// surface has changed attributes and needs
        /// to be regenerated.
        /// </summary>
        public event SurfaceMeshStaleHandler MeshStale;
        /// <summary>
        /// Called whenever the tile dimensions for this surface
        /// have changed.
        /// </summary>
        public event SurfaceDimensionsChangedHandler DimensionsChanged;
        #endregion
        #region Scene Editing Utilities
        /// <summary>
        /// The uv map dimensions where one pixel corresponds to one unit
        /// tile on the surface grid.
        /// </summary>
        public int UVUnit => Mathf.NextPowerOfTwo(Mathf.Max(lengthX, lengthY));
        /// <summary>
        /// Sets the transform directly will for the surface.
        /// Cannot be done via setting the transform as this
        /// is locked to prevent designers from overriding
        /// the surface placement logic.
        /// </summary>
        /// <param name="position">The position of the transform.</param>
        /// <param name="rotation">The rotation of the transform.</param>
        public void SetTransform(Vector3 position, Quaternion rotation)
        {
            currentPosition = position;
            currentRotation = rotation;
            transform.position = position;
            transform.rotation = rotation;
            transform.localScale = Vector3.one;
        }
        /// <summary>
        /// Does a line cast againt the surface checking for a hit tile.
        /// </summary>
        /// <param name="start">The start of the line.</param>
        /// <param name="end">The end of the line.</param>
        /// <param name="hitTile">The tile that was hit.</param>
        /// <returns>True if a tile was hit.</returns>
        public bool TryLinecast(Vector3 start, Vector3 end, out Vector2Int hitTile)
        {
            // Transform the line into local space,
            // making it easier to calculate.
            start = transform.InverseTransformPoint(start);
            end = transform.InverseTransformPoint(end);
            // Check for the exact intersection on the surface.
            if (TryLinecastLocal(start, end, out Vector2 hitLocation))
            {
                // Account for the tile counting system
                // to get the tile that was hit.
                hitTile = new Vector2Int(
                    Mathf.RoundToInt(hitLocation.x + 0.5f),
                    Mathf.RoundToInt(hitLocation.y + 0.5f));
                // Return true if the hit tile is in
                // the defined range of the surface.
                return hitTile.x > 0
                    && hitTile.x <= lengthX
                    && hitTile.y > 0
                    && hitTile.y <= lengthY;
            }
            else
            {
                hitTile = default;
                return false;
            }
        }
        /// <summary>
        /// Uses the underlying geometry to check if a line cast
        /// intersects with the front side of this surface.
        /// </summary>
        /// <param name="start">The start of the line in local space.</param>
        /// <param name="end">The end of the line in local space.</param>
        /// <param name="hitLocation">The raw 2D location on the surface strip.</param>
        /// <returns>True if there was a hit.</returns>
        protected abstract bool TryLinecastLocal(Vector3 start, Vector3 end, out Vector2 hitLocation);
        /// <summary>
        /// Uses the underlying geometry to generate wireframe
        /// polylines along the surface grid.
        /// </summary>
        /// <returns>
        /// A collection of polylines, that when drawn provide
        /// the designer with a visual representation of the surface.
        /// </returns>
        protected abstract Vector3[][] GetWireFramePolylines();
        /// <summary>
        /// Uses the underlying geometry to generate a mesh
        /// that is unwrapped such that each tile is one pixel
        /// unit on the mesh uv map.
        /// </summary>
        /// <returns>A new local mesh of the generated surface.</returns>
        public abstract Mesh GetTileMesh();
        #endregion
        #region Gizmos Drawing
        // Handle the gizmo drawing for surfaces.
        private void OnDrawGizmos()
        {
            // Set the color for the wireframe
            // based on if it is selected.
            bool isSelected = Selection.Contains(transform.gameObject);
            Gizmos.color = (isSelected ? Color.yellow : Color.gray);
            // Draw the wireframe.
            if (localWireframe != null)
            {
                for (int i = 0; i < localWireframe.Length; i++)
                {
                    Vector3 previousPoint = transform.TransformPoint(localWireframe[i][0]);
                    for (int j = 1; j < localWireframe[i].Length; j++)
                    {
                        Vector3 nextPoint = transform.TransformPoint(localWireframe[i][j]);
                        Gizmos.DrawLine(previousPoint, nextPoint);
                        previousPoint = nextPoint;
                    }
                }
            }
        }
        #endregion
        #region Base Validation
        public void ValidateConstraints()
        {
            // Validate the stitching constraints one by one.
            if (surfaceLinks != null)
            {
                foreach (StitchingConstraint constraint in surfaceLinks)
                {
                    // Name the surface to make drop down list
                    // more designer friendly.
                    constraint.inspectorName = (constraint.other != null) ?
                        $"Link to {constraint.other.gameObject.name}" : "New Link";
                    // Limit the constraint to be within
                    // valid ranges on this surface.
                    // NOTE negative values are valid and
                    // they represent tiles relative to the end.
                    constraint.fromTileOfThisSurface = Mathf.Clamp(
                        constraint.fromTileOfThisSurface, -lengthX, lengthX);
                    if (constraint.fromTileOfThisSurface == 0)
                        constraint.fromTileOfThisSurface = 1;
                    // Validate the constraint relative
                    // to the other surface.
                    if (constraint.other == null)
                    {
                        // Lock these values when they do nothing.
                        constraint.toTileOfOtherSurface = 1;
                        constraint.yStep = 0;
                    }
                    else
                    {
                        constraint.toTileOfOtherSurface = Mathf.Clamp(
                            constraint.toTileOfOtherSurface,
                            -constraint.other.lengthX, constraint.other.lengthX);
                        if (constraint.toTileOfOtherSurface == 0)
                            constraint.toTileOfOtherSurface = 1;
                        constraint.yStep = Mathf.Clamp(constraint.yStep,
                            1 - constraint.other.lengthY,
                            lengthY - 1);
                    }
                }
            }
        }
        protected virtual void OnValidate()
        {
            // Invoke the grid world to go through
            // and update any transforms containing
            // surfaces.
            GridWorld world = GetComponentInParent<GridWorld>();
            if (world != null)
            {
                world.ValidateLinkedConstraints(this);
                world.SolveStitchingConstraints();
            }
            // Handle a change in the length of the surface.
            if (lengthX != priorLengthX || lengthY != priorLengthY)
            {
                priorLengthX = lengthX;
                priorLengthY = lengthY;
                // Notify listeners in the editor.
                DimensionsChanged?.Invoke(this, lengthX, lengthY);
            }
            // Assume there has been a change in the surface
            // characteristics, and invoke a change.
            localWireframe = GetWireFramePolylines();
            // NOTE the same local mesh is passed to all
            // delegates as a shared resource. The mesh
            // is not meant to be edited.
            EditorApplication.delayCall += () =>
            {
                MeshStale?.Invoke(this, GetTileMesh());
            };
        }
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
#endif
        #region Common Surface Properties
        [Tooltip("Specifies which surfaces are linked to this surface.")]
        [SerializeField] public StitchingConstraint[] surfaceLinks = null;
        [Header("Surface Dimensions")]
        [Tooltip("The length of the surface in the horizontal direction.")]
        [SerializeField][Min(1)] private int lengthX = 10;
        [Tooltip("The length of the surface in the vertical direction.")]
        [SerializeField][Min(1)] private int lengthY = 10;
        #endregion
        #region Exposed Dimension Properties
        /// <summary>
        /// The horizontal span of the surface.
        /// </summary>
        public int LengthX => lengthX;
        /// <summary>
        /// The vertical height of the surface.
        /// </summary>
        public int LengthY => lengthY;
        #endregion
        #region Surface Calculation Methods
        /// <summary>
        /// Gets the world location at a local location
        /// on the surface.
        /// </summary>
        /// <param name="surfaceLocation">The local surface location.</param>
        /// <returns>The location in global space.</returns>
        public Vector3 GetLocation(Vector2 surfaceLocation)
        {
            // Return the location in global space.
            return transform.TransformPoint(
                GetLocationLocal(surfaceLocation));
        }
        /// <summary>
        /// Gets the world outwards direction at a local
        /// location on the surface.
        /// </summary>
        /// <param name="surfaceLocation">The local surface location.</param>
        /// <returns>The outwards direction in global space.</returns>
        public Vector3 GetOutwards(Vector2 surfaceLocation)
        {
            // Return the outwards direction in global space.
            return transform.TransformDirection(
                GetOutwardsLocal(surfaceLocation));
        }
        /// <summary>
        /// Gets the world up direction at a local
        /// location on the surface.
        /// </summary>
        /// <param name="surfaceLocation">The local surface location.</param>
        /// <returns>The up direction in global space.</returns>
        public Vector3 GetUp(Vector2 surfaceLocation)
        {
            // Return the up direction in global space.
            return transform.TransformDirection(
                GetUpLocal(surfaceLocation));
        }
        /// <summary>
        /// Gets the world right direction at a local
        /// location on the surface.
        /// </summary>
        /// <param name="surfaceLocation">The local surface location.</param>
        /// <returns>The right direction in global space.</returns>
        public Vector3 GetRight(Vector2 surfaceLocation)
        {
            // Return the right direction in global space.
            return transform.TransformDirection(
                GetRightLocal(surfaceLocation));
        }
        #endregion
        #region Subclass Local Calculation Methods
        /// <summary>
        /// Calculates the point in space that corresponds
        /// to a coordinate on the local strip.
        /// </summary>
        /// <param name="surfaceLocation">The local surface location.</param>
        /// <returns>The point on the strip in local space.</returns>
        protected abstract Vector3 GetLocationLocal(Vector2 surfaceLocation);
        /// <summary>
        /// Calculates the surface normal that corresponds
        /// to a coordinate on the local strip.
        /// </summary>
        /// <param name="surfaceLocation">The local surface location.</param>
        /// <returns>The surface normal in local space.</returns>
        protected abstract Vector3 GetOutwardsLocal(Vector2 surfaceLocation);
        /// <summary>
        /// Calculates the surface up direction that corresponds
        /// to a coordinate on the local strip. Up is along the positive
        /// direction of the lengthY axis of the strip.
        /// </summary>
        /// <param name="surfaceLocation">The local surface location.</param>
        /// <returns>The up normal in local space.</returns>
        protected abstract Vector3 GetUpLocal(Vector2 surfaceLocation);
        /// <summary>
        /// Calculates the surface right direction that corresponds
        /// to a coordinate on the local strip. Right is along the positive
        /// direction of the lengthX axis of the strip.
        /// </summary>
        /// <param name="surfaceLocation">The local surface location.</param>
        /// <returns>The right normal in local space.</returns>
        protected abstract Vector3 GetRightLocal(Vector2 surfaceLocation);
        #endregion
    }
}
