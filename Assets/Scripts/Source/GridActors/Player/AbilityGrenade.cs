using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace BattleRoyalRhythm.GridActors.Player
{
    public sealed class AbilityGrenade : ActorAbility
    {
        [Header("Base Grenade Attributes")]
        [Tooltip("The maximum number of grenades spawned at any given time.")]
        [SerializeField][Min(1)] private int maxGrenades = 1;
        [Tooltip("The distance to throw the grenade.")]
        [SerializeField][Min(0)] private int throwDistance = 0;
        [Tooltip("The number of beats before the grenade explodes.")]
        [SerializeField][Min(1)] private int beatsUntilExplosion = 3;
        [Tooltip("The radius of the grenade explosion.")]
        [SerializeField][Min(1)] private int explosionRadius = 2;
        [Tooltip("The damage that is dealt to each enemy hit while dashing.")]
        [SerializeField][Min(0)] private int enemyDamage = 0;
        [Tooltip("The number of beats that enemies are stunned for after hit by the dash.")]
        [SerializeField][Min(0)] private int enemyStunBeats = 0;
        [Tooltip("The number of tiles that the enemy is knocked back in the direction of the dash.")]
        [SerializeField][Min(0)] private int enemyKnockback = 0;
        [Header("Grenade Object")]
        [Tooltip("The template GameObject containing a BombActor.")]
        [SerializeField] private GameObject grenadeTemplate = null;

        private int activeGrenades;

        protected override void Awake()
        {
            base.Awake();
            activeGrenades = 0;
        }

        protected override bool IsContextuallyUsable()
        {
            // Only allow bomb usage if more bombs can
            // be spawned.
            return activeGrenades < maxGrenades;
        }

        public override void StartUsing(int beatCount)
        {
            // Since the grenade is thrown automatically,
            // this ability only takes one beat.
            StopUsing();
            // Spawn the bomb actor.
            BombActor newBomb = Instantiate(grenadeTemplate).GetComponent<BombActor>();
            // Assign the properties of the bomb.
            newBomb.beatsUntilExplosion = beatsUntilExplosion;
            newBomb.explosionRadius = explosionRadius;
            newBomb.enemyDamage = enemyDamage;
            newBomb.enemyKnockback = enemyKnockback;
            newBomb.enemyStunBeats = enemyStunBeats;
            // Spawn the bomb at the actor location.
            newBomb.World = UsingActor.World;
            newBomb.BeatService = UsingActor.World.BeatService;
            newBomb.CurrentSurface = UsingActor.CurrentSurface;
            newBomb.Location = UsingActor.Location;
            UsingActor.World.Actors.Add(newBomb);
            newBomb.Destroyed += OnBombDestroyed;
        }

        private void OnBombDestroyed(GridActor bomb)
        {
            activeGrenades--;
        }
    }
}
