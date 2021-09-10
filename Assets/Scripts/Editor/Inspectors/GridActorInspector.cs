using UnityEngine;
using UnityEditor;
using static UnityEditor.EditorGUILayout;
using BattleRoyalRhythm.GridActors;

namespace BattleRoyalRhythm.UnityEditor.Inspectors
{
    /// <summary>
    /// The base custom inspector for all grid actors,
    /// helps notify designers of reasons why grid
    /// actors will not properly initialize.
    /// </summary>
    [CustomEditor(typeof(GridActor), true)]
    public class GridActorInspector : Editor
    {
        #region Messages
        private const string NOT_WORLD_CHILD_MESSAGE = 
            "This Grid Actor will not activate because it is not the child " +
            "of a GameObject containing a Grid World. Reposition this actor in " +
            "the hierarchy so it is beneath a Grid World.";
        private const string NO_SURFACE_MESSAGE =
            "This Grid Actor will not activate because there is not a specified " +
            "starting surface. Add it to a surface so that a Grid World can register it";
        #endregion
        #region Inspector State
        private GridActor actor;
        #endregion
        #region Enabling / Disabling
        protected virtual void OnEnable()
        {
            actor = target as GridActor;
            // Bind to react to hierarchy changes and play mode starting.
            EditorApplication.hierarchyChanged += Repaint;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }
        protected virtual void OnDisable()
        {
            // Unbind from events.
            EditorApplication.hierarchyChanged -= Repaint;
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
        }
        #endregion
        #region Play Mode Listener
        private void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state is PlayModeStateChange.EnteredPlayMode)
            {
                // Print an error message in the log to notify
                // the designer if an actor may refuse to initialize.
                if (actor.gameObject.GetComponentInParent<GridWorld>() == null)
                    Debug.LogError(NOT_WORLD_CHILD_MESSAGE, target);
                if (actor.CurrentSurface == null)
                    Debug.LogError(NO_SURFACE_MESSAGE, target);
            }
        }
        #endregion
        #region Draw Inspector
        public override sealed void OnInspectorGUI()
        {
            // Add warnings to the actor if it state
            // does not allow it to initialize.
            if (actor.gameObject.GetComponentInParent<GridWorld>() == null)
                HelpBox(NOT_WORLD_CHILD_MESSAGE, MessageType.Error);
            if (actor.CurrentSurface == null)
                HelpBox(NO_SURFACE_MESSAGE, MessageType.Error);
            // Draw the normal inspector, or an inspector
            // specified by a subclass.
            InspectorGUIAfterActorBase();
        }
        /// <summary>
        /// Runs after warnings have been printed for the Grid Actor base.
        /// Override this to draw a custom inspector, otherwise the default
        /// inspector is drawn below the warnings.
        /// </summary>
        protected virtual void InspectorGUIAfterActorBase() => base.OnInspectorGUI();
        #endregion
    }
}
