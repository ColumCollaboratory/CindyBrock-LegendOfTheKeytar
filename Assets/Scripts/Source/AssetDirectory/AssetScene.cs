namespace CindyBrock.AssetDirectory
{
    /// <summary>
    /// References a specific location in the game that
    /// has a unique scene, but also may depend on other
    /// scenes being loaded.
    /// </summary>
    public enum AssetScene : short
    {
        MainMenu,
#if DEBUG
        // Scenes below will not be built in production,
        // but can still be navigated to for testing
        // purposes.
        TestBGMSingleton
#endif
    }
}
