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
        #region Drawer Initialization + Styling
        private readonly GUIStyle minLabelStyle, maxLabelStyle, minFieldStyle, maxFieldStyle;
        private readonly float minMaxLabelWidth, fieldWidth, gapWidth;
        public Bounds1DIntDrawer()
        {
            // Precalculate styles and layout values.
            minLabelStyle = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleRight };
            maxLabelStyle = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleLeft };
            minFieldStyle = new GUIStyle(GUI.skin.textField) { alignment = TextAnchor.MiddleRight };
            maxFieldStyle = new GUIStyle(GUI.skin.textField) { alignment = TextAnchor.MiddleLeft };
            // Widths are based on proportions of line height.
            minMaxLabelWidth = EditorGUIUtility.singleLineHeight * 2f;
            fieldWidth = EditorGUIUtility.singleLineHeight * 3f;
            gapWidth = EditorGUIUtility.singleLineHeight * 0.5f;
        }
        #endregion
        #region Drawer Implementation
        public override sealed void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            // Calculate the rects to draw.
            Rect labelRect = new Rect(position) { width = EditorGUIUtility.labelWidth };
            Rect minLabelRect = new Rect(labelRect) { width = minMaxLabelWidth, x = labelRect.xMax };
            Rect minRect = new Rect(minLabelRect) { width = fieldWidth, x = minLabelRect.xMax };
            Rect maxRect = new Rect(minRect) { width = fieldWidth, x = minRect.xMax + gapWidth };
            Rect maxLabelRect = new Rect(maxRect) { width = minMaxLabelWidth, x = maxRect.xMax };
            // Get the properties to effect.
            SerializedProperty min = property.FindPropertyRelative("min");
            SerializedProperty max = property.FindPropertyRelative("max");
            // Draw the properties.
            EditorGUI.BeginProperty(position, label, property);
            EditorGUI.LabelField(labelRect, label);
            EditorGUI.LabelField(minLabelRect, min.displayName, minLabelStyle);
            int newMin = EditorGUI.IntField(minRect, min.intValue, minFieldStyle);
            int newMax = EditorGUI.IntField(maxRect, max.intValue, maxFieldStyle);
            EditorGUI.LabelField(maxLabelRect, max.displayName, maxLabelStyle);
            // Validate the values of the range.
            // Assume that only one value has changed, we always want the value
            // adjusting to force the other end of the range.
            if (newMin != min.intValue)
            {
                if (newMin > newMax)
                    newMax = newMin;
            }
            else if (newMax != max.intValue)
                if (newMax < newMin)
                    newMin = newMax;
            // Update the values.
            min.intValue = newMin;
            max.intValue = newMax;
            EditorGUI.EndProperty();
        }
        #endregion
    }
}
