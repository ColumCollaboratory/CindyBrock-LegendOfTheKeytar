using UnityEngine;
using UnityEngine.UI;
using BattleRoyalRhythm.GridActors.Player;
using BattleRoyalRhythm.Input;
using BattleRoyalRhythm.Audio;
using Tools;

namespace BattleRoyalRhythm.UI
{
    /// <summary>
    /// Implements UI behaviour for the beat timeline.
    /// </summary>
    public sealed class BeatTimelineControl : MonoBehaviour
    {
        #region Local Data Structures
        /// <summary>
        /// Represents an image moving on the timeline with timing information.
        /// </summary>
        private struct TimedIcon
        {
            /// <summary>
            /// The image element moving along the timeline.
            /// </summary>
            public Image image;
            /// <summary>
            /// The age of the icon relative to the current time.
            /// </summary>
            public float beatAge;
        }
        #endregion

        #region Inspector Fields
        [Tooltip("The beat service that drives this timeline.")]
        [SerializeField] private BeatService beatService = null;
        [Tooltip("The player controller that this timeline reflects.")]
        [SerializeField] private PlayerActor playerActor = null;
        [Tooltip("The image where beat icons pass through as the user makes inputs.")]
        [SerializeField] private Image beatReticle = null;
        [Header("Canvas Layout (Unscaled)")]
        [Tooltip("The length in pixels between each beat input icon.")]
        [SerializeField][Min(0f)] private float iconGap = 50f;
        [Tooltip("The length in pixels which notes exit to the left.")]
        [SerializeField][Min(0f)] private float exitingRangeWidth = 100f;
        [Tooltip("The length in pixels which notes enter from the right.")]
        [SerializeField][Min(0f)] private float enteringRangeWidth = 100f;
        [Header("Icon Animation")]
        [Tooltip("The opacity curve for icons exiting the timeline.")]
        [SerializeField] private AnimationCurve exitingIconsOpacity = null;
        [Tooltip("The opacity curve for icons entering the timeline.")]
        [SerializeField] private AnimationCurve enteringIconsOpacity = null;
        [Header("Input Action Sprites")]
        [SerializeField][Min(1)] private int maxMissSprites = 10;
        [Tooltip("The sprite used when an input is missed.")]
        [SerializeField] private Sprite missSprite = null;
        [Tooltip("The sprite used to represent an upcoming or skipped input.")]
        [SerializeField] private Sprite emptySprite = null;
        [Tooltip("The ribbon used to connect related beats.")]
        [SerializeField] private Sprite connectorStrip = null;
        // TODO there should be tooling to automate the inspector
        // generation of this sort of mapping.
        // TODO since these icons may change per control scheme this
        // should be abstracted into a service.
        [SerializeField] private Sprite leftActionSprite = null;
        [SerializeField] private Sprite rightActionSprite = null;
        [SerializeField] private Sprite duckActionSprite = null;
        [SerializeField] private Sprite jumpActionSprite = null;
        [SerializeField] private Sprite attackActionSprite = null;
        [SerializeField] private Sprite abilityActionSprite = null;
        [SerializeField] private Sprite genre1ActionSprite = null;
        [SerializeField] private Sprite genre2ActionSprite = null;
        [SerializeField] private Sprite genre3ActionSprite = null;
        [SerializeField] private Sprite genre4ActionSprite = null;
        #endregion

        #region Fields
        private TimedIcon[] icons;
        // Index looping state.
        private IndexRange missFeedIndex;
        private IndexRange beatFeedIndex;
        private IndexRange currentBeatIndex;
        // Precalculate values.
        private int feedAge;
        #endregion


        private void Awake()
        {
            // Given the designer provided data, calculate
            // how many icons will be needed to loop through
            // the timeline region, these will be pooled.
            float totalRange = exitingRangeWidth + enteringRangeWidth;
            int iconCount = Mathf.CeilToInt(totalRange / iconGap) + 1;
            // Create a combined pool for all icons, and declare
            // looping index iterators.
            icons = new TimedIcon[iconCount + maxMissSprites];
            currentBeatIndex = new IndexRange(0, iconCount - 1)
            { LocalValue = Mathf.CeilToInt(exitingRangeWidth / iconGap) - 1 };
            beatFeedIndex = new IndexRange(0, iconCount - 1)
            { LocalValue = 0 };
            missFeedIndex = new IndexRange(iconCount, iconCount + maxMissSprites - 1)
            { LocalValue = 0 };

            feedAge = -Mathf.CeilToInt(enteringRangeWidth / iconGap);

            // Populate the beat images.
            beatFeedIndex.For((int i) =>
                icons[i] = GenerateIcon(feedAge, emptySprite));
            // Populate the miss images.
            missFeedIndex.For((int i) =>
                icons[i] = GenerateIcon(-feedAge, missSprite));
            TimedIcon GenerateIcon(float age, Sprite sprite)
            {
                GameObject icon = new GameObject();
                icon.transform.parent = beatReticle.transform;
                TimedIcon timedIcon = new TimedIcon()
                {
                    image = icon.AddComponent<Image>(),
                    beatAge = age
                };
                timedIcon.image.sprite = sprite;
                return timedIcon;
            }

            // Initialize the canvas properties of these images.
            OnCanvasSizeChanged();


            // Bind to the beat service.
            beatService.BeatElapsed += OnBeatElapsed;
            // Bind to the player.
            playerActor.ActionExecuted += OnActionExecuted;
            playerActor.BeatEarly += OnBeatEarly;
            playerActor.BeatLate += OnBeatEarly;
        }

        private void OnBeatEarly(float offsetTime)
        {
            // Set an error beat to the current time.
            //missIcons[missIconIndex].relativeBeatAge = beatService.SecondsPerBeat / offsetTime;
            missFeedIndex++;
        }

        private void OnActionExecuted(PlayerAction action, int duration)
        {
            // TODO this will be refactored (see inspector field notes).
            Sprite sprite = null;
            switch (action)
            {
                case PlayerAction.MoveLeft: sprite = leftActionSprite; break;
                case PlayerAction.MoveRight: sprite = rightActionSprite; break;
                case PlayerAction.Jump: sprite = jumpActionSprite; break;
                case PlayerAction.Duck: sprite = duckActionSprite; break;
                case PlayerAction.Attack: sprite = attackActionSprite; break;
                case PlayerAction.UseAbility: sprite = abilityActionSprite; break;
                case PlayerAction.SetGenre1: sprite = genre1ActionSprite; break;
                case PlayerAction.SetGenre2: sprite = genre2ActionSprite; break;
                case PlayerAction.SetGenre3: sprite = genre3ActionSprite; break;
                case PlayerAction.SetGenre4: sprite = genre4ActionSprite; break;
            }
            // Set the upcoming beat actions.
            int startIndex = currentBeatIndex;
            for (int i = 0; i < duration; i++)
            {
                currentBeatIndex++;
                icons[currentBeatIndex].image.sprite = sprite;
            }
            currentBeatIndex.Value = startIndex;
        }

        private void OnCanvasSizeChanged()
        {
            // Copy the positioning anchors on the reticle box.
            Vector2 anchorMin = beatReticle.rectTransform.anchorMin;
            Vector2 anchorMax = beatReticle.rectTransform.anchorMax;
            Vector2 sizeDelta = beatReticle.rectTransform.sizeDelta;
            foreach (TimedIcon icon in icons)
            {
                icon.image.rectTransform.anchorMin = anchorMin;
                icon.image.rectTransform.anchorMax = anchorMax;
                icon.image.rectTransform.sizeDelta = sizeDelta;
            }
        }

        private void OnBeatElapsed(float beatTime)
        {
            // Age all icons so they move to the
            // next span during the following cycle.
            for (int i = 0; i < icons.Length; i++)
                icons[i].beatAge++;
            // Loop one of the icons back around.
            icons[beatFeedIndex].image.sprite = emptySprite;
            icons[beatFeedIndex].beatAge = feedAge;
            // Increment and loop the pool indices.
            currentBeatIndex++;
            beatFeedIndex++;
        }

        private void Update()
        {
            // Position and apply fade to the beat icons.
            float interpolant = beatService.CurrentInterpolant;
            foreach (TimedIcon icon in icons)
            {
                // Calculate the x position of this sprite.
                float x = (icon.beatAge + interpolant) * iconGap * -1f;
                // Set the icon position and visual properties.
                icon.image.transform.localPosition = new Vector2(x, 0f);
                float opacity;
                if (x > 0f)
                    opacity = enteringIconsOpacity.Evaluate(
                        1 - x / enteringRangeWidth);
                else
                    opacity = exitingIconsOpacity.Evaluate(
                        -x / exitingRangeWidth);
                icon.image.color = new Color(1f, 1f, 1f, opacity);
            }
        }
    }
}
