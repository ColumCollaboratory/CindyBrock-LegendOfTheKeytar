using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Video;

public class SkippableCutscene : MonoBehaviour
{
    [SerializeField] private Transform hiddenContent = null;
    [SerializeField] private VideoPlayer videoPlayer = null;

    private bool startedCutscene;
    private bool finishedCutscene;

    private void Start()
    {
        startedCutscene = false;
        finishedCutscene = false;
        hiddenContent.gameObject.SetActive(false);
    }

    // Update is called once per frame
    void Update()
    {
        if (!startedCutscene && videoPlayer.isPlaying)
            startedCutscene = true;
        else if (startedCutscene && !finishedCutscene)
        {
            if (Mouse.current.leftButton.isPressed)
            {
                videoPlayer.Stop();
            }
            hiddenContent.gameObject.SetActive(!videoPlayer.isPlaying);
            if (!videoPlayer.isPlaying)
            {
                finishedCutscene = true;
            }
        }
    }
}
