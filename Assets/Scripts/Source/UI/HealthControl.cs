using CindyBrock.GridActors.Player;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace CindyBrock.UI
{
    public class HealthControl : MonoBehaviour
    {
        [SerializeField] private PlayerActor player = null;

        [SerializeField] private Image healthBarImage = null;

        [SerializeField] private Sprite[] healthBarStates = null;


        // Update is called once per frame
        void Update()
        {
            healthBarImage.sprite = healthBarStates[
                Mathf.FloorToInt((player.Health / player.MaxHealth) * (healthBarStates.Length - 1))];       
        }
    }
}
