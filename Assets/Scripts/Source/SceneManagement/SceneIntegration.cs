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
}
