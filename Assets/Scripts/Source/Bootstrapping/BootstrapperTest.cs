using UnityEngine.SceneManagement;

namespace CindyBrock.Bootstrapping
{
    /// <summary>
    /// A variation of the bootstrapper that loads the neccesary
    /// scenes to run this scene, allowing for testing in the current
    /// scene without having to start from the bootstrapper. The behaviour
    /// of this class is disabled in the build.
    /// </summary>
    public sealed class BootstrapperTest : Bootstrapper
    {
        #region Initiate Base Bootstrap
        protected override sealed void Start()
        {
#if DEBUG
            // Goto the boot scene after setting
            // a custom reroute to our current scene.
            if (!hasBootstrapped)
            {
                rerouteLocation = goToLocation;
                SceneManager.LoadScene(0);
            }
#endif
        }
        #endregion
    }
}
