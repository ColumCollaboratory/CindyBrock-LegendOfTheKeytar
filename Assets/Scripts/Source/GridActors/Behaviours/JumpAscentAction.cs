using UnityEngine;
using Tools;

namespace CindyBrock.GridActors.Behaviours
{
    [CreateAssetMenu(
        fileName = "Jump Ascent Action",
        menuName = "Grid World/Behaviors/Jump Ascent Action")]
    /// <summary>
    /// An action where an actor jumps directly up. Represents
    /// only the ascent of the jump.
    /// </summary>
    public class JumpAscentAction : ActorAction
    {
        #region Serialized Fields
        [Header("Jump Strength")]
        [Tooltip("The range of jump heights (max jump is executed if possible).")]
        [SerializeField] private Bounds1DInt jumpHeightRange = new Bounds1DInt(1, 3);
        [Header("Jump Duration")]
        [Tooltip("The number of beats elapsed to reach the peak of a min jump.")]
        [SerializeField] private Bounds1DInt jumpBeatsRange = new Bounds1DInt(1, 1);
        [Header("Animation Parameters")]
        [Tooltip("A delay in seconds before the actor starts jumping. Allows a jump animation to wind up.")]
        [SerializeField][Min(0f)] private float takeoffDelay = 0f;
        [Tooltip("Truncates the takeoff time delay if it consumes this much of the animation path (in beats).")]
        [SerializeField][Percent] private float delayMaxPercent = 0.5f;
        #endregion
        #region Action Context State
        private sealed class Context : ContextBase
        {
            /// <summary>
            /// The height of the current jump ascent.
            /// </summary>
            public int Height { get; set; }
            /// <summary>
            /// The total number of beats of the current jump ascent.
            /// </summary>
            public int TotalBeats { get; set; }
            /// <summary>
            /// The delay before the actor starts jumping.
            /// </summary>
            public float Delay { get; set; }
        }
        #endregion
        #region Action Implementation
        public override IActionContext GetActionContext(GridActor withActor)
        {
            int scanRange = jumpHeightRange.Max + withActor.TileHeight - 1;
            // Query the colliders above this actor.
            NearbyColliderSet colliders = withActor.World.GetNearbyColliders(withActor,
                0, 0, scanRange, 0);
            // Find the maximum possible jump height.
            int unobstructedTiles = 0;
            for (int y = withActor.TileHeight; y <= scanRange; y++)
            {
                if (colliders[0, y]) break;
                unobstructedTiles++;
            }
            // Should we execute this action?
            bool canJump = unobstructedTiles > jumpHeightRange.Min;
            // How many beats will it take to execute this action?
            int beatsToExecute = Mathf.RoundToInt(
                Mathf.Lerp(jumpBeatsRange.Min, jumpBeatsRange.Max,
                    Mathf.InverseLerp(jumpHeightRange.Min, jumpHeightRange.Max, unobstructedTiles)));
            // How should we apply the delay? If the animation
            // will consume too much of the jump time than limit
            // the amount of delay.
            float delay = Mathf.Min(
                (1f / withActor.World.BeatService.SecondsPerBeat) * takeoffDelay,
                beatsToExecute * delayMaxPercent);
            // Return results.
            return new Context()
            {
                IsPossible = canJump,
                HasEffect = canJump,
                PredictedEndpointDelta = Vector2Int.up * unobstructedTiles,
                IsInterruptible = false,
                BeatsLeft = beatsToExecute,
                Height = unobstructedTiles,
                TotalBeats = beatsToExecute,
                Delay = delay
            };
        }
        public override void AdvanceActionBeat(ref IActionContext contextToUpdate)
        {
            Context context = contextToUpdate as Context;
            // Just advance beat (no recalculation required).
            if (context.BeatsLeft > 0)
                context.BeatsLeft--;
        }
        public override Vector2 GetActionDelta(ref IActionContext contextToQuery, float interpolant)
        {
            Context context = contextToQuery as Context;
            // Get a local interpolant for how far into the jump we are.
            float timeInterpolant = 1f -
                (context.BeatsLeft - interpolant + context.Delay) / (context.TotalBeats - context.Delay);
            // If the delay has not elapsed, do not move the actor yet.
            if (timeInterpolant <= 0f)
                return Vector2.zero;
            else
                return Vector2.up * CalculateProjectileHeightDelta(timeInterpolant) * context.Height;
        }
        #endregion
        #region Local Functions
        /// <summary>
        /// Gets the height of projectile motion at the given time interpolant.
        /// </summary>
        /// <param name="timeInterpolant">The time between 0-1 of the jump.</param>
        /// <returns>A value from 0-1 that represents the amount of the jump ascent has elapsed at this time.</returns>
        protected float CalculateProjectileHeightDelta(float timeInterpolant)
        {
            // This is a parabola translated such that it
            // intersects with (0, 0) with apex at (1, 1).
            return 1f - (timeInterpolant - 1f) * (timeInterpolant - 1f);
        }
        #endregion
    }
}
