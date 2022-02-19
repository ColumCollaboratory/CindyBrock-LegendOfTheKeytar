using UnityEngine;

namespace CindyBrock
{
    /// <summary>
    /// Represents a singleton that exists as a Unity MonoBehaviour.
    /// Base class handles the instance management, while subclasses
    /// define singleton behaviour (which can be wrapped by an interface
    /// to hide inherited MonoBehaviour features).
    /// </summary>
    /// <typeparam name="T">The class or interface of the singleton instance.</typeparam>
    public abstract class Singleton<T> : MonoBehaviour
    {
        #region Singleton State
        private static T instance;
        private static GameObject instanceOwner;
        #endregion
        #region Singleton Accessor
        /// <summary>
        /// The instance of the singleton, or null/default if not initialized.
        /// </summary>
        public static T Instance
        {
            get
            {
#if DEBUG
                if (instance is null)
                {
                    // Notify editor user about the failure to access
                    // the singleton and pause the editor, so the log doesn't
                    // become filled with other errors that result from this error.
                    Debug.LogError(
                        $"A singleton of type {typeof(T).Name} was requested but " +
                        $"has not been created yet. Make sure it is enabled in a " +
                        $"persistance scene that is being loaded.");
                    Debug.Break();
                }
#endif
                return instance;
            }
        }
        #endregion
        #region Initialization
        // Initialize this singleton when the GameObject is first loaded.
        // Note that this can create issues with script execution order,
        // thus it is reccomended not to have singletons depend on each,
        // and to have them loaded in a seperate persistence scene.
        private void Awake()
        {
            // Check for singleton single instance rule violation.
            if (instance != null)
            {
#if DEBUG
                // Notify that this initialization was ignored, and pause the editor,
                // so the log doesn't become filled with other errors that result from
                // this error.
                Debug.LogError(
                    $"A singleton of type {typeof(T).Name} has ignored an initialization " +
                    $"from object `{gameObject}` because it has already been initialized " +
                    $"from another object `{instanceOwner}`. This may cause unexpected " +
                    $"behaviour. It is reccomended to remove the duplicate instance.");
                Debug.Break();
#endif
                return;
            }
            instanceOwner = gameObject;
            instance = Initialize();
        }
        /// <summary>
        /// Intitializes the state of this singleton.
        /// </summary>
        /// <returns>The singleton instance or instance wrapping interface.</returns>
        protected abstract T Initialize();
        #endregion
    }
}
