using UnityEngine;
using UnityEngine.InputSystem;
using BattleRoyalRhythm.Collections;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace BattleRoyalRhythm.Surfaces
{
    /// <summary>
    /// Contains a static block layout for a surface, and
    /// additionally editor logic for design in the Unity Editor.
    /// </summary>
    [ExecuteInEditMode]
    [DisallowMultipleComponent]
    public sealed class StaticBlockLayout : MonoBehaviour
    {
#if UNITY_EDITOR
        #region Scene Editing State
        // Prior field values, for expensive operations
        // not to be invoked every OnValidate call.
        private Surface priorTargetSurface;
        // Components to aid edit time (cleared on scene change).
        private MeshFilter meshFilter;
        private MeshRenderer meshRenderer;
        private Texture2D meshTexture;
        // Prevents OnValidate from running before on enabled.
        private bool hasEnabled;
        // Store a local copy of layout while editing to avoid
        // constant deserializing.
        private bool[,] layout;
        // Cursor drag state.
        private bool inDrag;
        private Vector2Int dragStart;
        #endregion
        #region Scene Initialization + Deinitialization
        private void OnEnable()
        {
            // Take control of existing mesh components
            // or create new ones to be used in the editor.
            meshFilter = gameObject.GetComponent<MeshFilter>();
            meshRenderer = gameObject.GetComponent<MeshRenderer>();
            if (meshFilter == null)
                meshFilter = gameObject.AddComponent<MeshFilter>();
            if (meshRenderer == null)
                meshRenderer = gameObject.AddComponent<MeshRenderer>();
            // Create a new texture that we will use for rendering
            // the layout inside the scene view.
            meshTexture = new Texture2D(8, 8);
            meshTexture.filterMode = FilterMode.Point;
            meshTexture.Apply();
            // Create and assign the material with the texture.
            Material editorSurfaceMat = new Material(Shader.Find("Unlit/Transparent Cutout"));
            editorSurfaceMat.mainTexture = meshTexture;
            meshRenderer.material = editorSurfaceMat;
            // Hide all of these components in the inspector,
            // as they are only meant to be modified via this script.
            meshFilter.hideFlags = HideFlags.HideInInspector;
            meshRenderer.hideFlags = HideFlags.HideInInspector;
            editorSurfaceMat.hideFlags = HideFlags.HideInInspector;
            // Load the serialized layout and if the
            // target surface is not null properly initialize
            // it based on the surface state.
            layout = savedLayout.Load();
            if (targetSurface != null)
            {
                targetSurface.MeshStale += OnSurfaceMeshStale;
                targetSurface.DimensionsChanged += OnDimensionsChanged;
                OnDimensionsChanged(targetSurface, targetSurface.LengthX, targetSurface.LengthY);
                OnSurfaceMeshStale(targetSurface, targetSurface.GetTileMesh());
            }
            // Run on validate since it will have
            // been skipped when first called.
            hasEnabled = true;
            OnValidate();
            // Bind to the scene loop.
            SceneView.duringSceneGui += OnScene;
        }
        private void OnDisable()
        {
            // Unbind from the scene loop.
            SceneView.duringSceneGui -= OnScene;
        }
        private void OnValidate()
        {
            if (hasEnabled)
            {
                layout = savedLayout.Load();
                // Handle a change in the target surface.
                if (targetSurface != priorTargetSurface)
                {
                    // Reassign handlers.
                    if (priorTargetSurface != null)
                    {
                        priorTargetSurface.MeshStale -= OnSurfaceMeshStale;
                        priorTargetSurface.DimensionsChanged -= OnDimensionsChanged;
                    }
                    if (targetSurface != null)
                    {
                        targetSurface.MeshStale += OnSurfaceMeshStale;
                        targetSurface.DimensionsChanged += OnDimensionsChanged;
                        // Regenerate the mesh for the new
                        // assigned surface. Delay call is used here
                        // because this step requires everything to be intialized.
                        EditorApplication.delayCall += () =>
                        {
                            // Redraw the mesh.
                            OnSurfaceMeshStale(targetSurface, targetSurface.GetTileMesh());
                            // Process dimensions if they are
                            // different than the prior dimensions.
                            if (targetSurface.LengthX != layout.GetLength(0)
                            || targetSurface.LengthY != layout.GetLength(1))
                                OnDimensionsChanged(targetSurface, targetSurface.LengthX, targetSurface.LengthY);
                        };
                    }
                    // Store this current reference so we
                    // can strip the handler off if it is
                    // changed again.
                    priorTargetSurface = targetSurface;
                }
            }
        }
        #endregion
        #region Scene Editing Handlers
        private void OnDimensionsChanged(Surface surface, int newX, int newY)
        {
            if (layout.GetLength(0) == 0)
                layout = new bool[1, 1];
            // Attempt to gracefully handle changes
            // in dimensions to be less annoying to
            // the designer.
            // Lower left is preserved when resizing down;
            // Top and right edges are extruded when resizing up.
            bool[,] newLayout = new bool[newX, newY];
            for (int x = 0; x < newX; x++)
                for (int y = 0; y < newY; y++)
                    newLayout[x, y] = layout[
                        Mathf.Min(x, layout.GetLength(0) - 1),
                        Mathf.Min(y, layout.GetLength(1) - 1)];
            // Apply scaled changes.
            layout = newLayout;
            savedLayout.Save(ref layout);
        }
        private void OnSurfaceMeshStale(Surface surface, Mesh newMesh)
        {
            meshFilter.mesh = newMesh;
            RegenerateColors();
        }
        private void RegenerateColors()
        {
            // If the uv unit size has changed,
            // then resize the texture.
            int size = targetSurface.UVUnit;
            if (size != meshTexture.width)
                meshTexture.Resize(size, size);
            // Generate the new color array from
            // the current designer layout.
            Color[] colors = new Color[size * size];
            for (int y = 0; y < layout.GetLength(1); y++)
                for (int x = 0; x < layout.GetLength(0); x++)
                    colors[y * targetSurface.UVUnit + x] =
                        layout[x, y] ? Color.magenta : Color.clear;
            // Apply the color changes.
            meshTexture.SetPixels(colors);
            meshTexture.Apply();
        }
        #endregion
        #region Scene Editing Input
        private void OnScene(SceneView scene)
        {
            // Is there anything preventing editing logic,
            // including non-intialized properties or utilities?
            if (!enableEditing ||
                targetSurface == null ||
                Mouse.current == null ||
                Keyboard.current == null)
                return;
            // TODO the mouse press logic for the editor here
            // should be abstracted into a common utility.
            // Mouse was pressed this frame?
            if (Mouse.current.leftButton.isPressed && !inDrag)
            {
                inDrag = true;
                // Cast a ray against the surface.
                Ray sceneMouseRay = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);
                targetSurface.TryLinecast(
                    sceneMouseRay.origin,
                    sceneMouseRay.origin + sceneMouseRay.direction * 1000f,
                    // If the ray misses this will be the sentinel (0, 0)
                    // which is not a valid tile (grids are 1-indexed).
                    out dragStart);
            }
            // Mouse was released this frame?
            else if (!Mouse.current.leftButton.isPressed && inDrag)
            {
                inDrag = false;
                // Was the start of the click a valid tile?
                if (dragStart != Vector2Int.zero)
                {
                    // Cast a final ray against the surface.
                    Ray sceneMouseRay = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);
                    if (targetSurface.TryLinecast(
                        sceneMouseRay.origin,
                        sceneMouseRay.origin + sceneMouseRay.direction * 1000f,
                        out Vector2Int endTile))
                    {
                        // Get the bounds of the box that was
                        // drawn by the cursor.
                        Vector2Int lower = new Vector2Int(
                            Mathf.Min(dragStart.x, endTile.x),
                            Mathf.Min(dragStart.y, endTile.y));
                        Vector2Int upper = new Vector2Int(
                            Mathf.Max(dragStart.x, endTile.x),
                            Mathf.Max(dragStart.y, endTile.y));
                        // Set our fill state based on whether the 
                        // left shift key is pressed.
                        bool isFilled = !Keyboard.current.leftShiftKey.isPressed;
                        // Fill the selected region.
                        for (int x = lower.x; x <= upper.x; x++)
                            for (int y = lower.y; y <= upper.y; y++)
                                layout[x - 1, y - 1] = isFilled;
                        // Apply changes.
                        savedLayout.Save(ref layout);
                        RegenerateColors();
                    }
                }
            }
        }
        #endregion
#endif
        #region Inspector Fields
        [Header("Components")]
        [Tooltip("The target surface that the layout maps to.")]
        [SerializeField] private Surface targetSurface = null;
        [Header("Scene Editing")]
        [Tooltip("Enables editing of the block layout using the mouse.")]
        [SerializeField] private bool enableEditing = false;
        // This object is serialized (saved) with the scene,
        // but we don't display it in the inspector.
        [SerializeField][HideInInspector] private ArraySerializer2D<bool> savedLayout = new ArraySerializer2D<bool>();
        #endregion
        #region Layout Accessor Property
        /// <summary>
        /// The static collider layout, defined at design time.
        /// </summary>
        public bool[,] Layout => savedLayout.Load();
        #endregion
    }
}
