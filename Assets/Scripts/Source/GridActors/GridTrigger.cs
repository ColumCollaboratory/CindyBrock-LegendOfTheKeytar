using System.Collections.Generic;
using UnityEngine;
using CindyBrock.Audio;
using Tools;

namespace CindyBrock.GridActors
{
    /// <summary>
    /// Base Implementation for a grid trigger.
    /// </summary>
    public abstract class GridTrigger : GridActor
    {
        #region Inspector Fields
        [Tooltip("Whether this trigger is currently active.")]
        [SerializeField][ReadonlyField] private bool triggerEnabled = true;
        #endregion
        #region Precalculated Fields
        // Stores self; avoids a call to List constructor
        // every frame.
        private List<GridActor> ignoredActors;
        #endregion
        #region Initialization + Deinitialization
        protected virtual void Start()
        {
            if (Application.isPlaying)
            {
                ignoredActors = new List<GridActor>() { this };
                ActorsInTrigger = new List<GridActor>();
                World.BeatService.BeatElapsed += OnBeatElapsed;
            }
        }
        protected override void OnDestroy()
        {
            World.BeatService.BeatElapsed -= OnBeatElapsed;
            base.OnDestroy();
        }
        #endregion

        #region Trigger Properties
        /// <summary>
        /// Whether this trigger is currently looking
        /// for intersecting actors each beat.
        /// </summary>
        public bool TriggerEnabled
        {
            get => triggerEnabled;
            set
            {
                // Bind or unbind to the beat service
                // based on the new state.
                if (value != triggerEnabled)
                {
                    triggerEnabled = value;
                    if (triggerEnabled)
                        World.BeatService.BeatElapsed += OnBeatElapsed;
                    else
                        World.BeatService.BeatElapsed -= OnBeatElapsed;
                }
            }
        }
        #endregion
        #region Trigger Implementation On Beat
        private void OnBeatElapsed(float beatTime)
        {
            List<GridActor> intersectingActors = World.GetIntersectingActors(
                CurrentSurface, Tile.x, Tile.y, Tile.x, Tile.y + TileHeight - 1,
                ignoredActors);
            // Check to see if any actors have left.
            foreach (GridActor actor in ActorsInTrigger)
            {
                if (!intersectingActors.Contains(actor))
                {
                    ActorsInTrigger.Remove(actor);
                    OnActorExit(actor);
                }
            }
            // Check to see if any actors have entered.
            foreach (GridActor actor in intersectingActors)
            {
                if (!ActorsInTrigger.Contains(actor))
                {
                    ActorsInTrigger.Add(actor);
                    OnActorEnter(actor);
                }
            }
            // If there are any actors in the trigger,
            // then run while actors call.
            if (ActorsInTrigger.Count > 0)
                WhileActorsInside();
        }
        #endregion

        #region Subclass Properties
        /// <summary>
        /// All actors currently in the trigger this beat.
        /// </summary>
        protected List<GridActor> ActorsInTrigger { get; private set; }
        #endregion
        #region Subclass Methods
        /// <summary>
        /// Called once for each actor that enters this trigger
        /// on the beat that they entered this trigger.
        /// </summary>
        /// <param name="actorEntered">The actor that entered.</param>
        protected virtual void OnActorEnter(GridActor actorEntered) { }
        /// <summary>
        /// Called once for each actor that exits this trigger
        /// on the beat that they exited this trigger.
        /// </summary>
        /// <param name="actorExited">The actor that exited.</param>
        protected virtual void OnActorExit(GridActor actorExited) { }
        /// <summary>
        /// Called every beat while actors are inside the trigger.
        /// </summary>
        protected virtual void WhileActorsInside() { }
        #endregion
    }
}
