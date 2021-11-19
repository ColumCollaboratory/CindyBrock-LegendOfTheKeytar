using System;
using System.Collections.Generic;
using UnityEngine;

namespace BattleRoyalRhythm.Collections
{

    public interface IEnumMap { }

    /// <summary>
    /// Generates a map between enum values and
    /// a serialized field type in the inspector.
    /// </summary>
    /// <typeparam name="TEnum">The enum to map values/references to.</typeparam>
    /// <typeparam name="TSerialized">The type of the linked serialized field.</typeparam>
    [Serializable]
    public sealed class EnumMap<TEnum, TSerialized> : IEnumMap
        where TEnum : Enum
    {
        [Serializable]
        private sealed class MapEntry
        {
            [SerializeField] public TEnum enumValue;
            [SerializeField] public TSerialized fieldValue;
        }

        #region Initialization
        public EnumMap()
        {
            // This will be resized and filled by the drawer.
            entries = new MapEntry[0];
        }
        #endregion


        [SerializeField] private MapEntry[] entries = null;
        [SerializeField] private TEnum enumIdentifier = default;

        private Dictionary<TEnum, TSerialized> runtimeDictionary;

        public TSerialized this[TEnum key]
        {
            get
            {
                if (runtimeDictionary is null)
                    GenerateDictionary();
                return runtimeDictionary[key];
            }
        }

        private void GenerateDictionary()
        {
            runtimeDictionary = new Dictionary<TEnum, TSerialized>();
            foreach (MapEntry entry in entries)
                runtimeDictionary.Add(entry.enumValue, entry.fieldValue);
        }
    }
}
