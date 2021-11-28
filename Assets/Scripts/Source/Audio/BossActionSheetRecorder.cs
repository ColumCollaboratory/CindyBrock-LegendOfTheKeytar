using System.Collections.Generic;
using UnityEngine;
using BattleRoyalRhythm.Input;
using BattleRoyalRhythm.UI;
using BattleRoyalRhythm.GridActors.Player;

namespace BattleRoyalRhythm.Audio
{
    public sealed class BossActionSheetRecorder : MonoBehaviour
    {
        [Header("Recorder Context")]
        [SerializeField] private PlayerActor player = null;
        [SerializeField] private BeatTimelineControl timeline = null;
        [Header("Asset Creation Mode")]
        [Tooltip("The asset to be written to.")]
        [SerializeField] private BossActionSheet targetSheetAsset = null;
        [Tooltip("When true existing notes will not be overwritten by inaction.")]
        [SerializeField] private bool onlyRecordOverwrittenNotes = true;

        private List<PlayerAction> recordedActions;

        private int beatIndex;


        private void Start()
        {
            beatIndex = 0;
            // Load the existing notes in.
            recordedActions = new List<PlayerAction>();
            recordedActions.AddRange(targetSheetAsset.notes);

            timeline.FeedUpcomingBeats(recordedActions);

            player.ActionExecuted += OnActionExecuted;
        }

        private void OnActionExecuted(PlayerAction action, int length)
        {
            if (beatIndex < recordedActions.Count)
            {
                if (onlyRecordOverwrittenNotes)
                {
                    if (action != PlayerAction.None)
                        recordedActions[beatIndex] = action;
                }
                else
                    recordedActions[beatIndex] = action;
            }
            else
                recordedActions.Add(action);
            beatIndex++;
        }

        private void OnDestroy()
        {
            // Save the recording results to
            // the target scriptable object.
            targetSheetAsset.notes = recordedActions.ToArray();
        }
    }
}
