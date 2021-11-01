using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using BattleRoyalRhythm.Audio;
using Tools;

namespace BattleRoyalRhythm.GridActors.Enemies
{

    public sealed class Enemy16thNote : GridActor, IDamageable
    {
        private enum BehaviourState : byte
        {
            Idle,
            Walking,
            Attacked,
            Attacking,
            Dying
        }

        [Header("Enemy Strength")]

        [SerializeField][Min(0)] private int health = 0;

        [SerializeField][Min(0)] private int meleeChargeupBeats = 1;

        [SerializeField][Min(0f)] private float meleeDamage = 1f;

        [SerializeField] private BeatService beatService = null;

        [Header("Enemy State")]
        [SerializeField][ReadonlyField] private GridActor target = null;
        [SerializeField][ReadonlyField] private BehaviourState state = BehaviourState.Idle;

        [Header("Animator Configuration")]
        [SerializeField] private AnimatorState<BehaviourState> animator = null;

        private void Start()
        {
            beatService.BeatElapsed += OnBeatElapsed;
        }

        private void OnBeatElapsed(float beatTime)
        {
            // Handle state change.
            switch (state)
            {
                case BehaviourState.Idle:
                    if (SearchForOpponent())
                    {

                    }
                    break;
            }
        }

        private bool SearchForOpponent()
        {
            throw new System.Exception();
        }

        // Update is called once per frame
        private void Update()
        {
            
        }

        public void ApplyDamage(float amount)
        {
            
        }
    }
}
