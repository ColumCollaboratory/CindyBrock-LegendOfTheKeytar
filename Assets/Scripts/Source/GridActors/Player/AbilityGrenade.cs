using CindyBrock.Audio;
using UnityEngine;

namespace CindyBrock.GridActors.Player
{
    public sealed class AbilityGrenade : ActorAbility
    {
        private enum State
        {
            None,
            PlacingBomb,
        }


        [Header("Base Grenade Attributes")]
        [Tooltip("The maximum number of grenades spawned at any given time.")]
        [SerializeField][Min(1)] private int maxGrenades = 1;
        [Header("Grenade Object")]
        [Tooltip("The template GameObject containing a BombActor.")]
        [SerializeField] private GameObject grenadeTemplate = null;

        [SerializeField] private AnimatorState<State> animator = null;

        private int activeGrenades;

        protected override void Awake()
        {
            base.Awake();
            activeGrenades = 0;
            animator.State = State.None;
        }

        protected override bool IsContextuallyUsable()
        {
            // Only allow bomb usage if more bombs can
            // be spawned.
            return activeGrenades < maxGrenades;
        }

        protected override void PostUsingCleanUp()
        {
            animator.State = State.None;
        }

        public override void StartUsing(int beatCount)
        {
            animator.State = State.PlacingBomb;
            // Since the grenade is thrown automatically,
            // this ability only takes one beat.
            StopUsing();
            // Spawn the bomb actor.
            BombActor newBomb = Instantiate(grenadeTemplate).GetComponent<BombActor>();
            // Spawn the bomb at the actor location.
            newBomb.InitializeGrid(UsingActor.World);
            newBomb.BeatService = UsingActor.World.BeatService;
            newBomb.CurrentSurface = UsingActor.CurrentSurface;
            newBomb.Tile = UsingActor.Tile;
            UsingActor.World.Actors.Add(newBomb);
            newBomb.RemovedFromGrid += OnBombDestroyed;
        }

        private void OnBombDestroyed(GridActor bomb)
        {
            activeGrenades--;
        }
    }
}
