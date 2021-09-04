using BattleRoyalRhythm.Audio;
using BattleRoyalRhythm.GridActors.Player;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace BattleRoyalRhythm.UI
{

    public class BeatControl : MonoBehaviour
    {
        [SerializeField] private PlayerActor player = null;
        [SerializeField] private BeatService beatService = null;

        [SerializeField] private Image revolverImage = null;
        [SerializeField] private Sprite[] revolverTextures = null;

        [SerializeField][Range(0f, 1f)] private float litTolerance = 0.1f;
        [SerializeField] private Image[] beatIndicators = null;
        [SerializeField] private Sprite unlitIndicator = null;
        [SerializeField] private Sprite litIndicator = null;

        [SerializeField][Min(0f)] private float timingWrongDuration = 0.1f;
        [SerializeField] private Image timingWrongImage = null;
        [SerializeField] private Sprite tooEarlyTexture = null;
        [SerializeField] private Sprite tooLateTexture = null;

        private int revolverIndex;

        // Start is called before the first frame update
        void Start()
        {
            revolverIndex = 0;
            beatService.BeatElapsed += OnBeatElapsed;
            timingWrongImage.enabled = false;
            player.BeatEarly += OnBeatEarly;
            player.BeatLate += OnBeatLate;
        }

        private void OnBeatEarly()
        {
            timingWrongImage.enabled = true;
            timingWrongImage.sprite = tooEarlyTexture;
            StartCoroutine(ResetTimingWrongImage());
        }
        private void OnBeatLate()
        {
            timingWrongImage.enabled = true;
            timingWrongImage.sprite = tooLateTexture;
            StartCoroutine(ResetTimingWrongImage());
        }

        private IEnumerator ResetTimingWrongImage()
        {
            yield return new WaitForSeconds(timingWrongDuration);
            timingWrongImage.enabled = false;
        }


        private void Update()
        {
            if (beatService.CurrentInterpolant < litTolerance ||
                beatService.CurrentInterpolant > 1f - litTolerance)
            {
                foreach (Image image in beatIndicators)
                    image.sprite = litIndicator;
            }
            else
            {
                foreach (Image image in beatIndicators)
                    image.sprite = unlitIndicator;
            }
        }

        private void OnBeatElapsed(float beatTime)
        {
            revolverIndex++;
            if (revolverIndex >= revolverTextures.Length)
                revolverIndex = 0;
            revolverImage.sprite = revolverTextures[revolverIndex];
        }
    }
}
