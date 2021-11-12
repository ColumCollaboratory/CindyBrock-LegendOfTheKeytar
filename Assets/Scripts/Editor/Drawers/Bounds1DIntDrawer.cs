using UnityEngine;
using UnityEditor;
using Tools;

namespace BattleRoyalRhythm.UnityEditor.Drawers
{
    /// <summary>
    /// Draws a bounds 1D range in the inspector,
    /// handling validation of the bounds.
    /// </summary>
    [CustomPropertyDrawer(typeof(Bounds1DInt))]
    public sealed class Bounds1DIntDrawer : PropertyDrawer
    {
        // TODO this is a hotfix for the quickly
        // implemented layout code below.
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label) => 0f;
        #region Drawer Implementation
        public override sealed void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            SerializedProperty min = property.FindPropertyRelative("min");
            SerializedProperty max = property.FindPropertyRelative("max");
            // TODO research vector2int to figure out how to properly draw
            // this including the labels.
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PrefixLabel(label);
            EditorGUIUtility.labelWidth = 100.0f;
            EditorGUILayout.PropertyField(min);
            EditorGUILayout.PropertyField(max);
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
            // Validate the values of the range.
            int minValue = min.intValue;
            int maxValue = max.intValue;
            if (minValue > maxValue)
                min.intValue = maxValue;
        }
        #endregion
    }
}
