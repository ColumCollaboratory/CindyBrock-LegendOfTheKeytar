using UnityEngine;
using UnityEditor;

namespace BattleRoyalRhythm.UnityEditor.Drawers
{
    // NOTE an interface is used here because property drawers
    // cannot currently support generic typeargs in classes.
    // Serialized properties can be arbitrarily retrieved,
    // so it is not a problem that the implementation is not specified.
    /// <summary>
    /// Handles drawing for the binding of a given enum
    /// to the string parameters inside an animation state
    /// machine.
    /// </summary>
    [CustomPropertyDrawer(typeof(IAnimatorState), true)]
    public sealed class AnimatorStateDrawer : PropertyDrawer
    {
        #region Drawer State
        private bool isExpanded;
        public AnimatorStateDrawer()
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

                // Grab the properties. (See AnimatorState.cs)
                SerializedProperty animatorProp = property.FindPropertyRelative("boundAnimator");
                SerializedProperty bindingsProp = property.FindPropertyRelative("bindings");
                SerializedProperty stateProp = property.FindPropertyRelative("state");
                string[] names = stateProp.enumDisplayNames;

                // Resize the array of bindings if the enum has changed.
                int size = bindingsProp.arraySize;
                int elementsToAdd = names.Length - size;
                if (elementsToAdd > 0)
                    for (int i = 0; i < elementsToAdd; i++)
                        bindingsProp.InsertArrayElementAtIndex(size + i);
                else if (elementsToAdd < 0)
                    for (int i = 0; i < Mathf.Abs(elementsToAdd); i++)
                        bindingsProp.DeleteArrayElementAtIndex(size - 1 - i);

                // Create fields for the animator reference and each
                // enum bound bool parameter in the animator.
                EditorGUILayout.PropertyField(animatorProp);
                EditorGUILayout.LabelField(new GUIContent("Bool Parameters"));
                for (int i = 0; i < names.Length; i++)
                {
                    SerializedProperty bindingProp = bindingsProp.GetArrayElementAtIndex(i);
                    SerializedProperty nameProp = bindingProp.FindPropertyRelative("boolName");
                    SerializedProperty enumProp = bindingProp.FindPropertyRelative("enumValue");

                    EditorGUILayout.PropertyField(nameProp, new GUIContent(names[i]));
                    // This ensures the proper enum value is bound.
                    // (this is redundant after one pass).
                    enumProp.enumValueIndex = i;
                }

                EditorGUI.indentLevel--;
            }
        }
        #endregion
    }
}
