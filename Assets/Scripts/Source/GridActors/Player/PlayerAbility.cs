using UnityEngine;

namespace BattleRoyalRhythm.GridActors.Player
{
    /// <summary>
    /// Base class for all player abilities.
    /// </summary>
    public abstract class PlayerAbility : MonoBehaviour
    {
        [Header("Ability Cooldown")]
        [Tooltip("The number of beats between usages, relative to the end of the ability.")]
        [SerializeField][Min(0)] private int cooldownBeats = 1;


        public int CooldownBeats => cooldownBeats;

        /// <summary>
        /// Runs in update to move the player actor
        /// while this ability is being used.
        /// </summary>
        /// <returns></returns>
        public virtual bool ApplyMovement() { return false; }

        /// <summary>
        /// Checks whether this ability can currently
        /// be used or advanced in state.
        /// </summary>
        public abstract bool IsUsable { get; }

        /// <summary>
        /// Returns true while this ability is in use.
        /// While the ability is in use, other actions
        /// are masked.
        /// </summary>
        public bool InUse { get; protected set; }

        /// <summary>
        /// Invokes the ability to be used or
        /// to update its current state.
        /// </summary>
        public abstract void TriggerAbility();

    }
}
