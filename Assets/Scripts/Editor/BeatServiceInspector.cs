using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using BattleRoyalRhythm.Audio;

[CustomEditor(typeof(BeatService), true)]
public class BeatServiceInspector : Editor
{
    IBeatService service;
    bool metronomeIsRight;

    private float value;

    private void OnEnable()
    {
        service = target as IBeatService;
        metronomeIsRight = true;
        service.BeatElapsed += SwitchMetronome;
    }
    private void OnDisable()
    {
        service.BeatElapsed -= SwitchMetronome;
    }

    private void SwitchMetronome(float beatTime) => metronomeIsRight = !metronomeIsRight;

    public override void OnInspectorGUI()
    {
        value = Mathf.Sin(service.CurrentInterpolant * Mathf.PI
                * (metronomeIsRight ? 1f : -1f));
        DrawDefaultInspector();
        GUI.enabled = false;
        EditorGUILayout.Slider(value, -1f, 1f);
        GUI.enabled = true;
    }

    public override bool RequiresConstantRepaint() => true;
}
