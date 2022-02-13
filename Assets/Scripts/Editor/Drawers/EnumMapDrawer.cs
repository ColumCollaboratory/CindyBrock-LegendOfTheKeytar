using UnityEngine;
using UnityEditor;
using CindyBrock.Collections;

namespace CindyBrock.UnityEditor.Drawers
{
    [CustomPropertyDrawer(typeof(IEnumMap), true)]
    public sealed class EnumMapDrawer : PropertyDrawer
    {
        #region Drawer State
        private bool isExpanded;
        public EnumMapDrawer()
        {
            isExpanded = false;
        }
        #endregion
        #region Drawer Implementation
        public override sealed void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            // Wrap this property in a drop down.
            isExpanded = EditorGUI.Foldout(position, isExpanded, label);
            if (isExpanded)
            {
                EditorGUI.indentLevel++;

                // Grab the properties. (See EnumMap.cs)
                SerializedProperty entriesProp = property.FindPropertyRelative("entries");
                string[] names = property.FindPropertyRelative("enumIdentifier").enumDisplayNames;

                // Resize the array of bindings if the enum has changed.
                int size = entriesProp.arraySize;
                int elementsToAdd = names.Length - size;
                if (elementsToAdd > 0)
                    for (int i = 0; i < elementsToAdd; i++)
                        entriesProp.InsertArrayElementAtIndex(size + i);
                else if (elementsToAdd < 0)
                    for (int i = 0; i < Mathf.Abs(elementsToAdd); i++)
                        entriesProp.DeleteArrayElementAtIndex(size - 1 - i);

                for (int i = 0; i < names.Length; i++)
                {
                    SerializedProperty entryProp = entriesProp.GetArrayElementAtIndex(i);
                    SerializedProperty fieldProp = entryProp.FindPropertyRelative("fieldValue");

                    EditorGUILayout.PropertyField(fieldProp, new GUIContent(names[i]));
                    // This ensures the proper enum value is bound.
                    SerializedProperty enumProp = entryProp.FindPropertyRelative("enumValue");
                    enumProp.enumValueIndex = i;
                }

                EditorGUI.indentLevel--;
            }
        }
        #endregion
    }
}
