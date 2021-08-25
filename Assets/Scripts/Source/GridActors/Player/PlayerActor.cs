using BattleRoyalRhythm.GridActors;
using BattleRoyalRhythm.Input;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BattleRoyalRhythm.GridActors.Player
{

    public sealed class PlayerActor : GridActor
    {
        [Tooltip("Number of tiles walked in a beat.")]
        [SerializeField][Min(1)] private int walkSpeed = 1;
        [Tooltip("Jump height apex in tiles.")]
        [SerializeField][Min(1)] private int jumpApex = 2;

        [SerializeField] private PlayerController controller = null;

        protected override sealed void Update()
        {
            base.Update();



        }


    }

}
