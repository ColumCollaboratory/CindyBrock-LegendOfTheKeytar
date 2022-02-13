using System;
using System.Collections.Generic;
using UnityEngine;

namespace BattleRoyalRhythm.Collections
{
    #region Interface Wrapper
    /// <summary>
    /// An empty interface that wraps EnumMap<T1, T2>. Not meant
    /// to be used on other classes. This is a workaround for Unity
    /// not supporting type parameters in `CustomPropertyDrawer` Property.
    /// </summary>
    public interface IEnumMap { }
    #endregion
    /// <summary>
    /// A map between enum values and a serialized field type in the inspector.
    /// These mapped values are immutable during runtime.
    /// </summary>
    /// <typeparam name="TEnum">The enum to map values/references to.</typeparam>
    /// <typeparam name="TSerialized">The type of the linked serialized field.</typeparam>
    [Serializable]
    public sealed class EnumMap<TEnum, TSerialized> : IEnumMap
        where TEnum : Enum
    {
        #region Serialized Storage
        [Serializable]
        private sealed class MapEntry
        {
            [SerializeField] public TEnum enumValue;
            [SerializeField] public TSerialized fieldValue;
        }
        [SerializeField] private MapEntry[] entries = null;
        // Warning disabled below because VS doesn't know that this
        // field is being used by the editor scripts (indirectly
        // via reflection).
#pragma warning disable IDE0052
        [SerializeField] private TEnum enumIdentifier = default;
#pragma warning restore IDE0052
        #endregion
        #region Runtime Storage
        private Dictionary<TEnum, TSerialized> runtimeDictionary;
        #endregion
        #region Constructors
        /// <summary>
        /// Creates a new empty enum map.
        /// </summary>
        public EnumMap()
        {
            // This will be resized and filled by the drawer.
            entries = new MapEntry[0];
        }
        #endregion
        #region Accessor
        /// <summary>
        /// Gets the mapped value linked to the key value.
        /// </summary>
        /// <param name="key">The key value.</param>
        /// <returns>The linked mapped value.</returns>
        public TSerialized this[TEnum key]
        {
            get
            {
                // If this is the first time accessing this map,
                // deserialize the keys and values into a dictionary.
                if (runtimeDictionary is null)
                {
                    runtimeDictionary = new Dictionary<TEnum, TSerialized>();
                    foreach (MapEntry entry in entries)
                        runtimeDictionary.Add(entry.enumValue, entry.fieldValue);
                }
                return runtimeDictionary[key];
            }
        }
        #endregion
    }
}
