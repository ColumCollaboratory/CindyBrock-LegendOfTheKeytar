using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine;

public class SceneIntegration : MonoBehaviour
{
    [SerializeField]
    private GameObject gamePanel;
    [SerializeField]
    private GameObject creditsPanel;
    [SerializeField]
    private GameObject instructionsPanel;
    [SerializeField]
    private GameObject jukeboxPanel;
    [SerializeField]
    private GameObject settingsPanel;

    public void ChangeScene(string nextLevel)
    {
        SceneManager.LoadScene(nextLevel);
    }

    public void ToggleCredits()
    {
        if (gamePanel.activeInHierarchy == true)
        {
            gamePanel.SetActive(false);
            creditsPanel.SetActive(true);
        }
        else
        {

            gamePanel.SetActive(true);
            creditsPanel.SetActive(false);
        }
    }

    public void ToggleInstructions()
    {
        if (gamePanel.activeInHierarchy == true)
        {
            gamePanel.SetActive(false);
            instructionsPanel.SetActive(true);
        }
        else
        {

            gamePanel.SetActive(true);
            instructionsPanel.SetActive(false);
        }
    }

    public void ToggleSettings()
    {
        if (gamePanel.activeInHierarchy == true)
        {
            gamePanel.SetActive(false);
            settingsPanel.SetActive(true);
        }
        else
        {

            gamePanel.SetActive(true);
            settingsPanel.SetActive(false);
        }
    }

    public void ToggleJukebox()
    {
        if (gamePanel.activeInHierarchy == true)
        {
            gamePanel.SetActive(false);
            jukeboxPanel.SetActive(true);
        }
        else
        {

            gamePanel.SetActive(true);
            jukeboxPanel.SetActive(false);
        }
    }

    public void Quit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
