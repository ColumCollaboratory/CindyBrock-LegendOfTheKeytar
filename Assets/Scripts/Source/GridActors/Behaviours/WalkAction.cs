using UnityEngine;
using Tools;

namespace CindyBrock.GridActors.Behaviours
{
    [CreateAssetMenu(
        fileName = "Walk Action",
        menuName = "Grid World/Behaviors/Walk Action")]
    /// <summary>
    /// An action where the actor walks in the direction
    /// that they are currently facing if there is something
    /// to walk on.
    /// </summary>
    public sealed class WalkAction : ActorAction
    {
        #region Serialized Fields
        [Header("Walk Distance")]
        [Tooltip("The range of walk distances (max walk is executed if possible).")]
        [SerializeField] private Bounds1DInt walkDistanceRange = new Bounds1DInt(1, 1);
        [Header("Walk Duration")]
        [Tooltip("The number of tiles passed in one beat. Fractional values will complete before the end of a beat.")]
        [SerializeField][Min(0f)] private float speed = 1f;
        #endregion
        #region Action Context State
        private sealed class Context : ContextBase
        {
            /// <summary>
            /// The distance of the current walk.
            /// </summary>
            public int Distance { get; set; }
            /// <summary>
            /// The total number of beats of the current walk motion.
            /// </summary>
            public int TotalBeats { get; set; }
        }
        #endregion
        #region Action Implementation
        public override sealed IActionContext GetActionContext(GridActor withActor)
        {
            int direction = withActor.Direction is Direction.Left ? -1 : 1;
            // Query the colliders in the walk range.
            NearbyColliderSet colliders = withActor.World.GetNearbyColliders(withActor, 
                walkDistanceRange.Max, withActor.TileHeight);
            // Scan for open walking space and
            // available floor tiles to move on.
            int walk = 0;
            for (int i = 1; i <= walkDistanceRange.Max; i++)
            {
                walk = i * direction;
                if (colliders.AnyInside(walk, 0, walk, withActor.TileHeight - 1) ||
                    !colliders[walk, -1])
                {
                    walk = (i - 1) * direction;
                    break;
                }
            }
            // Should a walk be executed?
            bool canWalk = Mathf.Abs(walk) >= walkDistanceRange.Min;
            // Calculate how long the walk will take.
            int beatsToExecute = Mathf.CeilToInt(Mathf.Abs(walk) / speed);
            // Return the context of this walk.
            return new Context()
            {
                IsPossible = canWalk,
                HasEffect = canWalk,
                PredictedEndpointDelta = Vector2Int.right * walk,
                IsInterruptible = false,
                BeatsLeft = beatsToExecute,
                Distance = walk,
                TotalBeats = beatsToExecute
            };
        }
        public override sealed void AdvanceActionBeat(ref IActionContext contextToUpdate)
        {
            Context context = contextToUpdate as Context;
            // Advance the beat.
            if (context.BeatsLeft > 0)
                context.BeatsLeft--;
            // Check if we are on a clean tile this beat.
            // This allows this action to be interrupted
            // with no jarring movement teleportation.
            float currentDelta = context.TotalBeats - context.BeatsLeft * speed;
            context.IsInterruptible = currentDelta % 1.0f < float.Epsilon;
        }
        public override sealed Vector2 GetActionDelta(ref IActionContext contextToQuery, float interpolant)
        {
            Context context = contextToQuery as Context;
            // Calculate the movement delta.
            float distanceTraveled = (context.TotalBeats - context.BeatsLeft + interpolant) * speed;
            // Clamp the distance if this motion reaches the
            // target tile earlier than the beat.
            if (context.Distance > 0f)
            {
                distanceTraveled = Mathf.Min(context.Distance, distanceTraveled);
            }
            else
            {
                distanceTraveled *= -1f;
                distanceTraveled = Mathf.Max(context.Distance, distanceTraveled);
            }
            return Vector2.right * distanceTraveled;
        }
        #endregion
    }
}
