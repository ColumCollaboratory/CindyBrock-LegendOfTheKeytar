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
                int offset = bindingsProp.arraySize - names.Length;
                for (int i = 0; i < offset; i++)
                    bindingsProp.DeleteArrayElementAtIndex(bindingsProp.arraySize - 1);
                offset = names.Length - bindingsProp.arraySize;
                for (int i = 0; i < offset; i++)
                    bindingsProp.InsertArrayElementAtIndex(bindingsProp.arraySize - 1);

                for (int i = 0; i < names.Length; i++)
                {
                    SerializedProperty bindingProp = bindingsProp.GetArrayElementAtIndex(i);
                    SerializedProperty nameProp = bindingProp.FindPropertyRelative("boolName");
                    SerializedProperty enumProp = bindingProp.FindPropertyRelative("enumValue");

                    EditorGUILayout.PropertyField(nameProp, new GUIContent(displayNames[i]));
                }

                EditorGUI.indentLevel--;
            }

            base.OnGUI(position, property, label);
        }

    }
}
