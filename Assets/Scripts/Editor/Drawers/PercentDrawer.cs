using UnityEngine;
using UnityEditor;
using Tools;

namespace CindyBrock.UnityEditor.Drawers
{
    /// <summary>
    /// Draws a float in the inspector that ranges
    /// from 0-1 as a percent between 0-100.
    /// </summary>
    [CustomPropertyDrawer(typeof(PercentAttribute))]
    public sealed class PercentDrawer : PropertyDrawer
    {
        #region Drawer Implementation
        public override sealed void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            Rect prop = new Rect(position);
            prop.width -= EditorGUIUtility.singleLineHeight;

            float startFieldX = EditorGUIUtility.labelWidth + GUI.skin.textField.padding.left;

            Rect fill = new Rect(position)
            {
                x = position.x + startFieldX,
                width = (prop.width - startFieldX) * property.floatValue
            };
            Rect rhs = new Rect(fill)
            {
                x = prop.xMax,
                width = EditorGUIUtility.singleLineHeight
            };
            EditorGUI.DrawRect(fill, new Color(0.5f, 0.1f, 0.1f));

            GUIStyle style = new GUIStyle(GUI.skin.textField)
            {
                alignment = TextAnchor.MiddleRight
            };

            Color prior = GUI.backgroundColor;
            GUI.backgroundColor = new Color(1f, 1f, 1f, 0.5f);
            float newValue = EditorGUI.FloatField(prop, label, property.floatValue * 100f, style) / 100f;
            EditorGUI.LabelField(rhs, "%");
            GUI.backgroundColor = prior;

            newValue = Mathf.Clamp01(newValue);
            property.floatValue = newValue;
        }
        #endregion
    }
}
