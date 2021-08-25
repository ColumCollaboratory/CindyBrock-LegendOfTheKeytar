using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BattleRoyalRhythm.Input
{

    public enum PlayerAction : byte
    {
        SetGenre1,
        SetGenre2,
        SetGenre3,
        SetGenre4,
        MoveLeft,
        MoveRight,
        Duck,
        Jump,
        Attack,
        UseAbility
    }

    public interface IPlayerController
    {
        float LatestTimestamp { get; }

        PlayerAction LatestAction { get; }
    }
}
