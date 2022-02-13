using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CindyBrock.Input
{
    /// <summary>
    /// Describes the player action taken.
    /// </summary>
    public enum PlayerAction : byte
    {
        None,
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
