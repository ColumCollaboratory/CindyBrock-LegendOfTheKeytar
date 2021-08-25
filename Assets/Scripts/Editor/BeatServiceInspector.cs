using UnityEngine;
using UnityEditor;
using BattleRoyalRhythm.Audio;

/// <summary>
/// Custom inspector for beat services, provides
/// added utility in a metronome for testing the
/// service without audio.
/// </summary>
[CustomEditor(typeof(BeatService), true)]
public sealed class BeatServiceInspector : Editor
{
    #region Inspector State
    private IBeatService service;
    private bool metronomeSide;
    private bool needsConstantRedraw;
    #endregion
    #region Enabling/Disabling
    private void OnEnable()
    {
        // Extract the beat service from
        // the MonoBehaviour.
        service = target as IBeatService;
        // Bind to the service to switch
        // the metronome side.
        service.BeatElapsed += SwitchMetronome;
        needsConstantRedraw = true;
        // Initialize metronome side.
        metronomeSide = true;
    }
    private void OnDisable()
    {
        // Unbind from beat events when disabled.
        service.BeatElapsed -= SwitchMetronome;
        needsConstantRedraw = false;
    }
    #endregion
    #region Draw Inspector
    /// <summary>
    /// Draws the metronome debug tools of the beast service.
    /// </summary>
    public override sealed void OnInspectorGUI()
    {
        // Draw the default properties
        // for the inspector.
        DrawDefaultInspector();
        // Add a labeled slider for the metronome.
        EditorGUILayout.LabelField(
            "Debug Metronome",
            EditorStyles.boldLabel);
        GUI.enabled = false;
        EditorGUILayout.Slider(
            // Sin used here to simulate the
            // movement of a metronome.
            Mathf.Sin(service.CurrentInterpolant * Mathf.PI
                * (metronomeSide ? 1f : -1f)),
            -1f, 1f);
        GUI.enabled = true;
    }
    // This keeps the metronome always
    // drawing in the inspector.
    public override sealed bool RequiresConstantRepaint() => needsConstantRedraw;
    #endregion
    #region Beat Service Listeners
    private void SwitchMetronome(float beatTime)
    {
        metronomeSide = !metronomeSide;
    }
    #endregion
}
