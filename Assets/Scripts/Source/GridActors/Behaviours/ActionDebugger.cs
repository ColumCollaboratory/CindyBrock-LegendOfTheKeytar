using CindyBrock.Audio;
using System.Collections;
using System.Collections.Generic;
using Tools;
using UnityEngine;

namespace CindyBrock.GridActors.Behaviours
{
    /// <summary>
    /// On the first beat elapsed, attempts to execute
    /// the specified action given the actor context.
    /// Used to debug the execution of actions.
    /// </summary>
    public sealed class ActionDebugger : GridActor
    {
        [Header("Action Debug")]
        [Tooltip("The target action to debug.")]
        [SerializeField] private ActorAction action = null;
        [Tooltip("The beat service simulator.")]
        [SerializeField] private BeatServiceSimulator beatServiceSimulator = null;
        [Header("Current Action State")]
        [Tooltip("Whether the action was possible.")]
        [SerializeField][ReadonlyField] private bool isPossible = false;
        [Tooltip("Whether executing the action would have any effect.")]
        [SerializeField][ReadonlyField] private bool hasEffect = false;
        [Tooltip("The total movement from this action.")]
        [SerializeField][ReadonlyField] public Vector2Int predictedEndpointDelta = Vector2Int.zero;
        [Tooltip("Whether the action can currently be interrupted.")]
        [SerializeField][ReadonlyField] public bool isInterruptible = false;
        [Tooltip("How many beats are left on this action.")]
        [SerializeField][ReadonlyField] public float beatsLeft = 0f;

        private IActionContext context;
        private bool executingAction;
        private Vector2 lastMovementDelta;




        private void Start()
        {
            executingAction = false;
            beatServiceSimulator.BeatElapsed += OnFirstBeatElapsed;
        }
        protected override sealed void OnDestroy()
        {
            base.OnDestroy();
            beatServiceSimulator.BeatElapsed -= OnFirstBeatElapsed;
            beatServiceSimulator.BeatElapsed -= OnFollowingBeatsElapsed;
        }

        private void OnFirstBeatElapsed(float beatTime)
        {
            context = action.GetActionContext(this);
            if (context.IsPossible)
            {
                executingAction = true;
                lastMovementDelta = Vector2.zero;
                beatServiceSimulator.BeatElapsed += OnFollowingBeatsElapsed;
            }
            beatServiceSimulator.BeatElapsed -= OnFirstBeatElapsed;
            UpdateDebugFields();
        }

        private void OnFollowingBeatsElapsed(float beatTime)
        {
            action.AdvanceActionBeat(ref context);
            if (context.BeatsLeft <= 0)
            {
                executingAction = false;
                beatServiceSimulator.BeatElapsed -= OnFollowingBeatsElapsed;
                // Finalize the position of the actor.
                Vector2 movementDelta = action.GetActionDelta(ref context, 1f);
                World.TranslateActor(this, movementDelta - lastMovementDelta);
            }
            UpdateDebugFields();
        }

        private void UpdateDebugFields()
        {
            // Update the displayed debug values.
            isPossible = context.IsPossible;
            hasEffect = context.HasEffect;
            predictedEndpointDelta = context.PredictedEndpointDelta;
            isInterruptible = context.IsInterruptible;
            beatsLeft = context.BeatsLeft;
        }

        private void Update()
        {
            if (executingAction)
            {
                beatsLeft = context.BeatsLeft - beatServiceSimulator.CurrentInterpolant;
                // Update the position of the actor.
                Vector2 movementDelta = action.GetActionDelta(
                    ref context, beatServiceSimulator.CurrentInterpolant);
                World.TranslateActor(this, movementDelta - lastMovementDelta);
                lastMovementDelta = movementDelta;
            }
        }
    }
}
