using System;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEditor;
using System.IO;
using UnityEngine.UIElements;

namespace CindyBrock.Audio
{
    public sealed class SoundtrackSettings : ScriptableObject
    {
        [SerializeField] public SoundtrackSet[] soundtrackSets = new SoundtrackSet[0];


        public string[] GetAllSetNames()
        {
            // Handle null case (could happen in edit time).
            if (soundtrackSets == null)
                return new string[0];
            // Accumulate all names.
            string[] names = new string[soundtrackSets.Length];
            for (int i = 0; i < soundtrackSets.Length; i++)
                names[i] = soundtrackSets[i].name;
            return names;
        }

        public SoundtrackSet GetSetByID(int id)
        {
            // Handle null case (could happen in edit time).
            if (soundtrackSets == null)
                return null;
            // Find the first set with the name.
            // This assumes names are unique
            // (enforced only by inspector at edit time).
            foreach (SoundtrackSet set in soundtrackSets)
                if (set.id == id)
                    return set;
            // If no match is found return null.
            return null;
        }

        public SoundtrackSet GetSetByName(string name)
        {
            // Handle null case (could happen in edit time).
            if (soundtrackSets == null)
                return null;
            // Find the first set with the name.
            // This assumes names are unique
            // (enforced only by inspector at edit time).
            foreach (SoundtrackSet set in soundtrackSets)
                if (set.name == name)
                    return set;
            // If no match is found return null.
            return null;
        }

        private const string PATH = "Assets/Resources/SoundtrackSettings.asset";

        private static SoundtrackSettings instance;

        public static SoundtrackSettings Load()
        {
            // Have the settings not been initialized?
            if (instance == null)
            {
#if UNITY_EDITOR
                instance = AssetDatabase.LoadAssetAtPath<SoundtrackSettings>(PATH);
                // Were there no settings to load?
                if (instance == null)
                {
                    // Create the default settings object.
                    instance = CreateInstance<SoundtrackSettings>();
                    AssetDatabase.CreateAsset(instance, PATH);
                    AssetDatabase.SaveAssets();
                }
#else
                instance = Resources.Load<SoundtrackSettings>("SoundtrackSettings");
#endif
            }
            return instance;
        }
#if UNITY_EDITOR

        /// <summary>
        /// Ensures that the naming of soundtracks is unique.
        /// </summary>
        public void OnValidate()
        {
            if (soundtrackSets != null)
            {
                string[] usedNames = new string[soundtrackSets.Length];
                int i = 0;
                foreach (SoundtrackSet set in soundtrackSets)
                {
                    // Use a default track name in place
                    // of an empty field.
                    if (set.name == string.Empty)
                        set.name = "Track Set";
                    // Ensure that the name is unique.
                    set.name = ObjectNames.GetUniqueName(usedNames, set.name);
                    // Mark the name as used.
                    usedNames[i] = set.name;
                    i++;
                }
                // Ensure that all soundtrack sets
                // have a unique ID. TODO this is done
                // inefficiently, maybe use GUID?
                int[] allIDs = new int[soundtrackSets.Length];
                for (i = 0; i < soundtrackSets.Length; i++)
                    allIDs[i] = soundtrackSets[i].id;
                List<int> usedIDs = new List<int>();
                i = 0;
                foreach (SoundtrackSet set in soundtrackSets)
                {
                    // Does this ID need to be updated because
                    // it is either not generated or conflicting?
                    if (set.id == 0 || usedIDs.Contains(set.id))
                    {
                        int newID = 1;
                        while (allIDs.Contains(newID)) newID++;
                        set.id = newID;
                        allIDs[i] = newID;
                    }
                    usedIDs.Add(set.id);
                    i++;
                }
            }
        }
#endif
    }

#if UNITY_EDITOR
    class SoundtrackSettingsProvider : SettingsProvider
    {
        private static SerializedObject settings;
        private static SoundtrackSettings settingsObj;

        public SoundtrackSettingsProvider(string path, SettingsScope scope = SettingsScope.User)
            : base(path, scope) { }

        public override void OnActivate(string searchContext, VisualElement rootElement)
        {
            // This function is called when the user clicks on the MyCustom element in the Settings window.
            settingsObj = SoundtrackSettings.Load();
            settings = new SerializedObject(settingsObj);
        }

        public override void OnGUI(string searchContext)
        {
            EditorGUI.BeginChangeCheck();

            // Use IMGUI to display UI:
            EditorGUILayout.PropertyField(settings.FindProperty("soundtrackSets"));

            if (EditorGUI.EndChangeCheck())
                settings = new SerializedObject(settingsObj);

        }

        // Register the SettingsProvider
        [SettingsProvider]
        public static SettingsProvider CreateMyCustomSettingsProvider()
        {
            var provider = new SoundtrackSettingsProvider("Project/Soundtrack", SettingsScope.Project);
            
            provider.keywords = new List<string> { "soundtrackSets" };
            return provider;
        }
    }
#endif
}
