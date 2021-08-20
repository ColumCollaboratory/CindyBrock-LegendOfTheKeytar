using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using BattleRoyalRhythm.Collections;

namespace BattleRoyalRhythm.Surfaces
{
    [ExecuteInEditMode]
    [DisallowMultipleComponent]
    public sealed class StaticBlockLayout : MonoBehaviour
    {
        [SerializeField][HideInInspector] private ArraySerializer2D<bool> savedLayout = new ArraySerializer2D<bool>();
        [Header("Components")]
        [Tooltip("The target surface that the layout maps to.")]
        [SerializeField] private Surface targetSurface = null;
        [Header("Scene Editing")]
        [Tooltip("Enables editing of the block layout using the mouse.")]
        [SerializeField] private bool enableEditing = false;

        private bool hasEnabled;
        private bool[,] layout;

        private MeshFilter meshFilter;
        private MeshRenderer meshRenderer;

        public bool[,] Layout => savedLayout.Load();

        private Surface priorTargetSurface;


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
                        // assigned surface.
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
            Material thisSurface = new Material(Shader.Find("Unlit/Transparent Cutout"));
            Texture2D thisTexture = new Texture2D(targetSurface.UVUnit, targetSurface.UVUnit);
            Color[] colors = new Color[targetSurface.UVUnit * targetSurface.UVUnit];
            for (int y = 0; y < layout.GetLength(1); y++)
                for (int x = 0; x < layout.GetLength(0); x++)
                    colors[y * targetSurface.UVUnit + x] = layout[x, y] ? Color.magenta : Color.clear;
            thisTexture.SetPixels(colors);
            thisTexture.Apply();
            thisTexture.filterMode = FilterMode.Point;
            thisSurface.mainTexture = thisTexture;
            thisSurface.hideFlags = HideFlags.HideInInspector;
            meshRenderer.material = thisSurface;
        }


        private void OnEnable()
        {
            hasEnabled = true;
            meshFilter = gameObject.GetComponent<MeshFilter>();
            meshRenderer = gameObject.GetComponent<MeshRenderer>();
            if (meshFilter == null)
                meshFilter = gameObject.AddComponent<MeshFilter>();
            if (meshRenderer == null)
                meshRenderer = gameObject.AddComponent<MeshRenderer>();
            meshFilter.hideFlags = HideFlags.HideInInspector;
            meshRenderer.hideFlags = HideFlags.HideInInspector;

            layout = savedLayout.Load();
            SceneView.duringSceneGui += OnScene;
            // Initialize the mesh in the scene.
            if (targetSurface != null)
            {
                targetSurface.MeshStale += OnSurfaceMeshStale;
                targetSurface.DimensionsChanged += OnDimensionsChanged;
                OnDimensionsChanged(targetSurface, targetSurface.LengthX, targetSurface.LengthY);
                OnSurfaceMeshStale(targetSurface, targetSurface.GetTileMesh());
            }
            OnValidate();
        }
        private void OnDisable()
        {
            SceneView.duringSceneGui -= OnScene;
        }

        private bool inDrag = false;
        private Vector2Int startTile;

        private void OnScene(SceneView scene)
        {
            if (Mouse.current != null && Keyboard.current != null)
            {
                if (enableEditing && targetSurface != null)
                {
                    if (Mouse.current.leftButton.isPressed)
                    {
                        if (!inDrag)
                        {
                            Ray r = scene.camera.ScreenPointToRay(new Vector2(
                                Event.current.mousePosition.x,
                                scene.camera.pixelHeight - Event.current.mousePosition.y));

                            if (!targetSurface.TryLinecast(r.origin, r.origin + r.direction * 1000f, out startTile))
                            {
                                startTile = Vector2Int.zero;
                            }
                            else
                            {

                            }

                            inDrag = true;
                        }
                    }
                    else
                    {
                        if (inDrag)
                        {
                            inDrag = false;
                            Vector2Int endTile;
                            Ray r = scene.camera.ScreenPointToRay(new Vector2(
                                Event.current.mousePosition.x,
                                scene.camera.pixelHeight - Event.current.mousePosition.y));


                            if (targetSurface.TryLinecast(r.origin, r.origin + r.direction * 1000f, out endTile))
                            {
                                if (startTile != Vector2Int.zero)
                                {
                                    Vector2Int lower = new Vector2Int(
                                        Mathf.Min(startTile.x, endTile.x),
                                        Mathf.Min(startTile.y, endTile.y));
                                    Vector2Int upper = new Vector2Int(
                                        Mathf.Max(startTile.x, endTile.x),
                                        Mathf.Max(startTile.y, endTile.y));

                                    bool eraser = Keyboard.current.leftShiftKey.isPressed;

                                    for (int x = lower.x; x <= upper.x; x++)
                                    {
                                        for (int y = lower.y; y <= upper.y; y++)
                                        {
                                            layout[x - 1, y - 1] = !eraser;
                                        }
                                    }
                                    savedLayout.Save(ref layout);
                                    RegenerateColors();
                                }
                            }
                        }
                    }
                }

            }
        }

    }
}
