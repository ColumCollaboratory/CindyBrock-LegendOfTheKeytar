using UnityEngine;

namespace CindyBrock.GridActors.Behaviours
{
    #region Action Context Interface for State Machines / BTrees
    /// <summary>
    /// Defines common parameters a behaviour
    /// state machine will use when choosing
    /// whether to execute the action.
    /// </summary>
    public interface IActionContext
    {
        /// <summary>
        /// True if this action can be executed. Only applies
        /// when starting the action.
        /// </summary>
        public bool IsPossible { get; }
        /// <summary>
        /// This value is false when the action can be executed
        /// but does not make sense to execute since it will neither
        /// result in movement nor state change.
        /// </summary>
        public bool HasEffect { get; }
        /// <summary>
        /// The current predicted endpoint of the action (relative
        /// to where the action was started).
        /// </summary>
        public Vector2Int PredictedEndpointDelta { get; }
        /// <summary>
        /// True if this action can be cleanly interrupted on this
        /// beat because it is expected to be on an exact tile.
        /// </summary>
        public bool IsInterruptible { get; }
        /// <summary>
        /// The number of beats remaining until this
        /// action frees its state (if not interrupted).
        /// This number may change or extend itself.
        /// </summary>
        public int BeatsLeft { get; }
    }
    #endregion

    /// <summary>
    /// Base class for all actor actions. These represent actions
    /// that an actor can take on the grid on the evaluation of a beat.
    /// </summary>
    public abstract class ActorAction : ScriptableObject
    {
        #region Base Context Implementation
        /// <summary>
        /// The base implementation of context where all
        /// properties can be read and written to. State
        /// can be added specific to the subclass action.
        /// </summary>
        protected abstract class ContextBase : IActionContext
        {
            public bool IsPossible { get; set; }
            public bool HasEffect { get; set; }
            public Vector2Int PredictedEndpointDelta { get; set; }
            public bool IsInterruptible { get; set; }
            public int BeatsLeft { get; set; }
        }
        #endregion
        #region Required Implementations
        /// <summary>
        /// Gets the current action context given a specific actor.
        /// Should be executed when querying whether to start this action.
        /// </summary>
        /// <param name="withActor">The actor to use the action.</param>
        /// <returns>The action context, which will be used to update the action.</returns>
        public abstract IActionContext GetActionContext(GridActor withActor);
        /// <summary>
        /// Notifies the action to update its state because
        /// a beat has elapsed.
        /// </summary>
        /// <param name="context">The context to update.</param>
        public abstract void AdvanceActionBeat(ref IActionContext context);
        /// <summary>
        /// Gets the animation path offset from where the action started
        /// based on the current interpolant.
        /// </summary>
        /// <param name="context">The context of the action.</param>
        /// <param name="interpolant">The current beat interpolant.</param>
        /// <returns>The offset from the starting location of the action.</returns>
        public abstract Vector2 GetActionDelta(ref IActionContext context, float interpolant);
        #endregion
    }
}
