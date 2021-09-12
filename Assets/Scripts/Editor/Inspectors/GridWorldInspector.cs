using UnityEngine;
using UnityEditor;
using static UnityEditor.EditorApplication;
using static UnityEditor.EditorGUI;
using static UnityEditor.EditorGUILayout;
using BattleRoyalRhythm.GridActors;
using BattleRoyalRhythm.Surfaces;

namespace BattleRoyalRhythm.UnityEditor.Inspectors
{
    /// <summary>
    /// Custom inspector for the grid world. Stores
    /// editor preferences for the grid world.
    /// </summary>
    [CustomEditor(typeof(GridWorld), true)]
    public sealed class GridWorldInspector : Editor
    {
        #region Inspector State
        private GridWorldInspectorState state;
        private GridWorld world;
        #endregion
        #region Enabling / Disabling
        private void OnEnable()
        {
            world = target as GridWorld;
            // Retrieve or create the editor preferences
            // that are serialized on the target world.
            if (world.EditorPreferences != null)
                state = world.EditorPreferences as GridWorldInspectorState;
            else
            {
                state = new GridWorldInspectorState();
                world.EditorPreferences = state;
            }
            // Initialize fill visibility state.
            UpdateEditorFillVisibility();
            // Listen for the transition between play and edit modes.
            playModeStateChanged += OnPlayModeStateChanged;
        }
        private void OnDisable()
        {
            // Remove listeners.
            playModeStateChanged -= OnPlayModeStateChanged;
        }
        #endregion
        #region Play Mode Listener
        private void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            // Update the fill visibility when the
            // play mode changes.
            switch (state)
            {
                case PlayModeStateChange.EnteredEditMode:
                case PlayModeStateChange.EnteredPlayMode:
                    UpdateEditorFillVisibility();
                    break;
            }
        }
        #endregion
        #region Draw Inspector
        public sealed override void OnInspectorGUI()
        {
            // Draw the base properties of the grid world.
            base.OnInspectorGUI();
            // Draw the additional properties for the editor.
            state.showEditorProperties = Foldout(state.showEditorProperties, "Editor Preferences");
            if (state.showEditorProperties)
            {
                indentLevel++;
                if (state.showGuidesInSceneView !=
                    Toggle("Show Guides In Scene View", state.showGuidesInSceneView))
                {
                    state.showGuidesInSceneView = !state.showGuidesInSceneView;
                    EditorUtility.SetDirty(world);
                    UpdateEditorFillVisibility();
                }
                if (state.showGuidesInPlayMode !=
                    Toggle("Show Guides In Play Mode", state.showGuidesInPlayMode))
                {
                    state.showGuidesInPlayMode = !state.showGuidesInPlayMode;
                    EditorUtility.SetDirty(world);
                    UpdateEditorFillVisibility();
                }
                Color newWireColor = ColorField("Grid Wire Color", state.wireColor);
                if (newWireColor != state.wireColor)
                {
                    state.wireColor = newWireColor;
                    EditorUtility.SetDirty(world);
                    SceneView.RepaintAll();
                }
                Color newFillColor = ColorField("Grid Fill Color", state.fillColor);
                if (newFillColor != state.fillColor)
                {
                    state.fillColor = newFillColor;
                    EditorUtility.SetDirty(world);
                    UpdateEditorFillColor(newFillColor);
                }
                indentLevel--;
            }
        }
        #endregion
        #region Update Block Layouts
        private void UpdateEditorFillVisibility()
        {
            // Toggle visibility on all surface meshes.
            if (Application.isPlaying)
                foreach (StaticBlockLayout layout in world.GetComponentsInChildren<StaticBlockLayout>())
                    layout.GetComponent<Renderer>().enabled = world.EditorPreferences.ShowGuidesInPlayMode;
            else
                foreach (StaticBlockLayout layout in world.GetComponentsInChildren<StaticBlockLayout>())
                    layout.GetComponent<Renderer>().enabled = world.EditorPreferences.ShowGuidesInSceneView;
        }
        private void UpdateEditorFillColor(Color newColor)
        {
            // Update the fill color on each static block layout.
            foreach (StaticBlockLayout layout in world.GetComponentsInChildren<StaticBlockLayout>())
                layout.FillColor = newColor;
        }
        #endregion
    }
}
