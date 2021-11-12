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

    public void ChangeScene(string nextLevel)
    {
        SceneManager.LoadScene(nextLevel);
    }

    public void ToggleCredits()
    {
        if (gamePanel.activeSelf == true)
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
        if (gamePanel.activeSelf == true)
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
}
