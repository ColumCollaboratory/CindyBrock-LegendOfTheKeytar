using UnityEngine;
using UnityEditor;
using CindyBrock.Audio;

namespace CindyBrock.UnityEditor.Drawers
{
    /// <summary>
    /// Creates a drop down based on the soundtrack settings
    /// that are serialized as a scriptable object asset.
    /// </summary>
    [CustomPropertyDrawer(typeof(SoundtrackIDAttribute))]
    public sealed class SoundtrackIDDrawer : PropertyDrawer
    {
        #region Draw Property
        public override sealed void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            // Retrieve the soundtrack options.
            SoundtrackSettings settings = SoundtrackSettings.Load();
            string[] soundtrackOptions = settings.GetAllSetNames();
            // Is the current int value a valid soundtrack ID?
            int index = -1;
            for (int i = 0; i < settings.soundtrackSets.Length; i++)
            {
                if (settings.soundtrackSets[i].id == property.intValue)
                {
                    index = i;
                    break;
                }
            }
            // If not choose the first soundtrack option by default.
            if (index == -1) index = 0;
            // Create a drop down to choose a soundtrack.
            index = EditorGUI.Popup(position, label.text, index, soundtrackOptions);
            // Re-assign the soundtrack ID to the property.
            property.intValue = settings.GetSetByName(soundtrackOptions[index]).id;
        }
        #endregion
    }
}
