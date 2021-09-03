using UnityEngine;

namespace BattleRoyalRhythm.GridActors.Player
{
    /// <summary>
    /// Base class for all abilities that can be used by
    /// grid actors in the scene.
    /// </summary>
    public abstract class ActorAbility : MonoBehaviour
    {
        #region Common Inspector Fields
        [Header("Ability Cooldown")]
        [Tooltip("The number of beats between usages, relative to the end of the prior use.")]
        [SerializeField][Min(0)] private int cooldownBeats = 1;
        #endregion

        #region Initialization
        protected virtual void Awake()
        {
            // Ensure the cooldown is off
            // when the gameplay starts.
            lastUseBeatCount = -cooldownBeats;
        }
        #endregion



        private int lastUseBeatCount;

        /// <summary>
        /// Checks whether the ability can currently be used.
        /// </summary>
        /// <param name="beatCount">The current beat number.</param>
        /// <returns>True if the ability can currently be used.</returns>
        public bool IsUsable(int beatCount)
        {
            // Check that the cooldown is elapsed and that this ability
            // can be performed in the current context.
            return (beatCount - lastUseBeatCount > cooldownBeats) &&
                IsContextuallyUsable();
        }
        /// <summary>
        /// Checks if the ability state or surrounding world state
        /// allows for this ability to be used.
        /// </summary>
        /// <returns>True if the ability can be used.</returns>
        protected virtual bool IsContextuallyUsable() => true;

        
        /// <summary>
        /// Starts usage of the ability which will last at least
        /// one beat. Elapse beat should be called following this call.
        /// Check the InUse property to see when the ability usage has completed.
        /// </summary>
        /// <param name="beatCount">The beat that this ability is starting to be used on.</param>
        public virtual void StartUsing(int beatCount)
        {
            InUse = true;
            lastUseBeatCount = beatCount;
        }
        /// <summary>
        /// Notifies the ability to stop usage after this
        /// beat has completed. Can be called by the ability
        /// itself.
        /// </summary>
        public virtual void StopUsing()
        {
            InUse = false;
        }

        /// <summary>
        /// Elapses a beat for the ability.
        /// </summary>
        /// <returns>An actor animation path, or null if the ability does not animate the actor.</returns>
        public ActorAnimationPath ElapseBeat()
        {
            lastUseBeatCount++;
            return UsingBeatElapsed();
        }
        /// <summary>
        /// Called whenever the beat elapses for this ability.
        /// </summary>
        /// <returns>An actor animation path, or null if the ability does not animate the actor.</returns>
        protected virtual ActorAnimationPath UsingBeatElapsed() => null;

        /// <summary>
        /// The actor that is using this ability.
        /// </summary>
        public GridActor UsingActor { get; set; }

        /// <summary>
        /// The number of beats between usages, relative to the end of the prior use.
        /// </summary>
        public int CooldownBeats => cooldownBeats;

        /// <summary>
        /// Returns true while the ability is still requesting
        /// control of the actor for the next beat.
        /// </summary>
        public bool InUse { get; private set; }

    }
}
