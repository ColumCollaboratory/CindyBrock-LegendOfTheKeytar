using UnityEngine;
using CindyBrock.AssetDirectory;

namespace CindyBrock.Bootstrapping
{
    /// <summary>
    /// The bootstrapper class exists solely to load into
    /// the startup screen after core singletons have initialized.
    /// </summary>
    public abstract class Bootstrapper : MonoBehaviour
    {
        #region Bootstrapper State
        protected static bool hasBootstrapped = false;
#if DEBUG
        // Allows the test bootstrapper to reroute where
        // we go after the singletons have setup.
        protected static AssetScene? rerouteLocation = null;
#endif
        #endregion
        #region Inspector Fields
        [Tooltip("Where the game goes after global systems have loaded.")]
        [SerializeField] protected AssetScene goToLocation = AssetScene.MainMenu;
        #endregion
        #region Bootstrapping Function
        // This should run after all singletons have
        // initialized from the Awake call.
        protected virtual void Start()
        {
#if DEBUG
            if (rerouteLocation is AssetScene newLocation)
                goToLocation = newLocation;
#endif
            if (!hasBootstrapped)
            {
                SingletonSceneManager.Instance.ChangeLocation(goToLocation);
                hasBootstrapped = true;
            }
#if DEBUG
            else
            {
                // Notify if someone attempts to place multiple
                // bootstrappers in the project.
                Debug.LogError(
                    $"Additional run of the bootstrapper from object `{gameObject}` " +
                    $"was ignored because the bootstrapper can only run once. Please " +
                    $"remove instances of bootstrapper outside of scene 0.");
                Debug.Break();
            }
#endif
        }
        #endregion
    }
}
