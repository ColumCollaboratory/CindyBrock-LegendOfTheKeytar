using System;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor.SceneManagement;
#endif

// NOTE this class would become obsolete if Unity comes to
// support multi-dim array serialization.
// TODO this should be part of a helper library.
namespace BattleRoyalRhythm.Collections
{
    /// <summary>
    /// Handles serialiazation and deserialization for two-dimensionsal
    /// arrays held in a scene instance. Useful for saving multi-dimensional data
    /// about a scene or actor.
    /// </summary>
    /// <typeparam name="T">The type of data being saved. Must be serializeable by Unity.</typeparam>
    [Serializable]
    public sealed class ArraySerializer2D<T>
    {
        #region Serialized Fields
        // This is what is actually serialized in the scene script.
        [SerializeField][HideInInspector] private int lengthX;
        [SerializeField][HideInInspector] private int lengthY;
        [SerializeField][HideInInspector] private T[] values;
        #endregion
        #region Save/Load Methods
        /// <summary>
        /// Loads the saved two-dimensional array.
        /// </summary>
        /// <returns>The loaded array, or an empty two dimensional array if there is no save.</returns>
        public T[,] Load()
        {
            // Unflatten the saved array.
            T[,] loadedArray = new T[lengthX, lengthY];
            for (int y = 0; y < lengthY; y++)
            {
                for (int x = 0; x < lengthX; x++)
                {
                    int i = y * lengthX + x;
                    // If a range of the 2D array is unsaved
                    // it will remain filled by default/null.
                    if (i < values.Length)
                        loadedArray[x, y] = values[i];
                    else
                        break;
                }
            }
            return loadedArray;
        }
        /// <summary>
        /// Saves the two dimensional array and notifies Unity that
        /// the scene is dirty and needs to save.
        /// </summary>
        /// <param name="saveFrom">The two dimensional array to save from.</param>
        public void Save(ref T[,] saveFrom)
        {
            // Flatten the array.
            lengthX = saveFrom.GetLength(0);
            lengthY = saveFrom.GetLength(1);
            values = new T[lengthX * lengthY];
            for (int y = 0; y < lengthY; y++)
                for (int x = 0; x < lengthX; x++)
                    values[y * lengthX + x] = saveFrom[x, y];
#if UNITY_EDITOR
            // Mark scene dirty to ensure that serialized changes
            // are to be saved. Otherwise the designer might mistakenly
            // close the scene without being prompted to save.
            if (!Application.isPlaying)
                EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
#endif
        }
        #endregion
    }
}
