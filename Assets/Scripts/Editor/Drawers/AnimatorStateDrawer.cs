using UnityEngine;
using UnityEditor;
using Tools;
using System;

namespace BattleRoyalRhythm.UnityEditor.Drawers
{
    [CustomPropertyDrawer(typeof(IAnimatorState), true)]
    public sealed class AnimatorStateDrawer : PropertyDrawer
    {

        private bool isExpanded;

        public AnimatorStateDrawer()
        {
            isExpanded = false;
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label) => 0f;

        public override sealed void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {

            isExpanded = EditorGUILayout.Foldout(isExpanded, label);

            if (isExpanded)
            {
                EditorGUI.indentLevel++;
                SerializedProperty animatorProp = property.FindPropertyRelative("boundAnimator");
                EditorGUILayout.PropertyField(animatorProp);

                SerializedProperty bindingsProp = property.FindPropertyRelative("bindings");
                SerializedProperty stateProp = property.FindPropertyRelative("state");

                string[] names = stateProp.enumNames;
                string[] displayNames = stateProp.enumDisplayNames;

                // Resize the array in case the enum has changed.
                int size = bindingsProp.arraySize;
                int resize = names.Length - size;
                if (resize > 0)
                    for (int i = 0; i < resize; i++)
                        bindingsProp.InsertArrayElementAtIndex(size + i);
                else if (resize < 0)
                    for (int i = 0; i < Mathf.Abs(resize); i++)
                        bindingsProp.DeleteArrayElementAtIndex(size - 1 - i);

                EditorGUILayout.LabelField(new GUIContent("Bool Parameters"));

                for (int i = 0; i < names.Length; i++)
                {
                    SerializedProperty bindingProp = bindingsProp.GetArrayElementAtIndex(i);
                    SerializedProperty nameProp = bindingProp.FindPropertyRelative("boolName");
                    SerializedProperty enumProp = bindingProp.FindPropertyRelative("enumValue");

                    EditorGUILayout.PropertyField(nameProp, new GUIContent(displayNames[i]));
                    enumProp.enumValueIndex = i;
                }

                EditorGUI.indentLevel--;
            }
        }

    }
}
