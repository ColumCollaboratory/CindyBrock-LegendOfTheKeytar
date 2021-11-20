using UnityEngine;

namespace BattleRoyalRhythm.GridActors.Player
{
    public sealed class AbilityDash : ActorAbility
    {
        private enum DashState : byte
        {
            None,
            Dashing,
            Falling
        }

        [Header("Base Dash Attributes")]
        [Tooltip("The distance to move or less if occluded.")]
        [SerializeField][Min(1)] private int dashTiles = 2;
        [Tooltip("When true the player can dash through walls, only if the dash ends outside a wall.")]
        [SerializeField] private bool dashThroughWalls = false;
        [Tooltip("When true the player can dash through special actors (TODO not implemented).")]
        [SerializeField] private bool dashThroughSpecialActors = false;
        [Tooltip("The damage that is dealt to each enemy hit while dashing.")]
        [SerializeField][Min(0)] private int enemyDamage = 0;
        [Tooltip("The number of beats that enemies are stunned for after hit by the dash.")]
        [SerializeField][Min(0)] private int enemyStunBeats = 0;
        [Tooltip("The number of enemies that can be pierced before the dash is interrupted.")]
        [SerializeField][Min(0)] private int enemiesPierced = 0;
        [Tooltip("The number of tiles that the enemy is knocked back in the direction of the dash.")]
        [SerializeField][Min(0)] private int enemyKnockback = 0;

        [SerializeField] private AnimatorState<DashState> animator = null;

        private int calculatedDashTiles;

        protected override void Awake()
        {
            base.Awake();
            animator.State = DashState.None;
        }

        public int DashTiles
        {
            get => dashTiles;
            set => dashTiles = Mathf.Max(1, value);
        }

        public bool CanDashThroughWalls
        {
            get => dashThroughWalls;
            set => dashThroughWalls = value;
        }
        public bool CanDashThroughSpecialActors
        {
            get => dashThroughSpecialActors;
            set => dashThroughSpecialActors = value;
        }

        public override void StartUsing(int beatCount)
        {
            base.StartUsing(beatCount);
            animator.State = DashState.Dashing;
        }

        protected override void PostUsingCleanUp()
        {
            animator.State = DashState.None;
        }

        protected override sealed bool IsContextuallyUsable()
        {
            NearbyColliderSet colliders = UsingActor.World.GetNearbyColliders(
                UsingActor, dashTiles, UsingActor.TileHeight);
            // Check each step along the dash to find
            // the furthest distance that can be dashed.
            calculatedDashTiles = 0;
            if (UsingActor.Direction == Direction.Right)
            {
                for (int x = 1; x <= dashTiles; x++)
                {
                    bool isBlocked = colliders.AnyInside(x, 0, x, UsingActor.TileHeight - 1);
                    if (!isBlocked) calculatedDashTiles = x;
                    else if (!dashThroughWalls) break;
                }
            }
            else
            {
                for (int x = -1; x >= -dashTiles; x--)
                {
                    bool isBlocked = colliders.AnyInside(x, 0, x, UsingActor.TileHeight - 1);
                    if (!isBlocked) calculatedDashTiles = x;
                    else if (!dashThroughWalls) break;
                }
            }
            willFall = false;
            return calculatedDashTiles != 0;
        }

        private bool willFall;

        protected override sealed BeatAction UsingBeatElapsed()
        {
            if (willFall)
                animator.State = DashState.Falling;
            switch (animator.State)
            {
                case DashState.Dashing:
                    // Check to see if the result of the dash will land
                    // the player in midair. If so another step will be required
                    // to drop them to complete the ability.
                    NearbyColliderSet colliders = UsingActor.World.GetNearbyColliders(
                        UsingActor, Mathf.Abs(calculatedDashTiles), 1);
                    if (colliders[calculatedDashTiles, -1])
                        StopUsing();
                    else
                        willFall = true;
                    return new BeatAction(
                        ActorAnimationsGenerator.CreateWalkPath(calculatedDashTiles), 1);
                case DashState.Falling:
                    StopUsing();
                    animator.State = DashState.None;
                    // Scan for a location to drop down to.
                    NearbyColliderSet colliders2 = UsingActor.World.GetNearbyColliders(
                        UsingActor, 0, 30);
                    for (int y = -2; y >= -30; y--)
                        if (colliders2[0, y])
                            return new BeatAction(ActorAnimationsGenerator.CreateDropDownPath(y + 1), 1);
                    throw new System.Exception("FALLING EDGE CASE :(");
            }
            return null;
        }

    }
}
