using UnityEngine;
using UnityEditor;
using Tools;

namespace CindyBrock.UnityEditor.Drawers
{
    /// <summary>
    /// Disables editing for this field.
    /// </summary>
    [CustomPropertyDrawer(typeof(ReadonlyFieldAttribute))]
    public sealed class ReadonlyFieldDrawer : PropertyDrawer
    {
        #region Draw Property
        public override sealed void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            // Thanks to Patryk Galach for this code reference
            // https://www.patrykgalach.com/2020/01/20/readonly-attribute-in-unity-editor/
            // Record the GUI enabled state and disable the state
            // to draw this property.
            bool previousGUIState = GUI.enabled;
            GUI.enabled = false;
            EditorGUI.PropertyField(position, property, label);
            GUI.enabled = previousGUIState;
        }
        #endregion
    }
}
