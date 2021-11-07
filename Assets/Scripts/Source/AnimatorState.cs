using System;
using UnityEngine;

// This interface is needed for the drawer to
// be applicable to any typeargument for the Enum.
// Typeargs in drawer target types are not supported.
/// <summary>
/// Identifies the standard implementation of
/// AnimatorState(TEnum).<br/>
/// IMPORTANT: This interface is NOT meant to be used outside of
/// the standard implementation.
/// </summary>
public interface IAnimatorState { }

/// <summary>
/// Binds an enum directly to a corresponding set of bool parameters
/// inside a Unity Animator. Use the State property to automatically
/// update the appropriate animator parameters.
/// </summary>
/// <typeparam name="TEnum">The enum to use for state. Each enum value
/// correpsonds to a bool parameter in the Unity Animator.</typeparam>
[Serializable]
public sealed class AnimatorState<TEnum> : IAnimatorState
    where TEnum : Enum
{
    #region Local Data Structure
    // This data structure stores the mappings from
    // the enum to the hash value. Hash value is calculated
    // once at evaluation time from the bool name provided by
    // the inspector.
    [Serializable]
    private sealed class AnimatorBinding
    {
        /// <summary>
        /// The name of the boolean animator parameter.
        /// </summary>
        [SerializeField] public string boolName;
        /// <summary>
        /// The hash ID of the animator paramater.
        /// </summary>
        [SerializeField] public int hashValue;
        /// <summary>
        /// Whether this state exists in the animator.
        /// </summary>
        [SerializeField] public bool existsInAnimator;
        /// <summary>
        /// The bound enum value that enables this parameter.
        /// </summary>
        [SerializeField] public TEnum enumValue;
    }
    #endregion
    #region Serialization Structure
    [Tooltip("The animator that this state drives.")]
    [SerializeField] private Animator boundAnimator = null;
    [SerializeField] private AnimatorBinding[] bindings = null;
    // TODO this should be exposed as a readonly field in the inspector.
    [SerializeField] private TEnum state = default;
    #endregion
    #region Initialization
    public AnimatorState()
    {
        // This will be resized and filled by the drawer.
        bindings = new AnimatorBinding[0];
    }
    #endregion
    #region Runtime Implementation
    /// <summary>
    /// The current state of the animator. Updating this
    /// directly will cause the animator to update.
    /// </summary>
    public TEnum State
    {
        get => state;
        set
        {
            state = value;
            // Update the animator states.
            CheckGenerateHashes();
            foreach (AnimatorBinding binding in bindings)
                if (binding.existsInAnimator)
                    boundAnimator.SetBool(binding.hashValue, state.Equals(binding.enumValue));
        }
    }
    private void CheckGenerateHashes()
    {
        // If the hash values have not been generated,
        // iterate through them so that future state changes
        // will not require hashing.
        if (bindings[0].hashValue == default)
        {
            foreach (AnimatorBinding binding in bindings)
            {
                binding.hashValue = Animator.StringToHash(binding.boolName);
                // Allow for parameters that don't explicitly exist
                // in the animator (this allows a sentinel value).
                binding.existsInAnimator = false;
                foreach (AnimatorControllerParameter parameter in boundAnimator.parameters)
                {
                    if (parameter.nameHash == binding.hashValue)
                    {
                        binding.existsInAnimator = true;
                        break;
                    }
                }
            }
        }
    }
    #endregion
}
