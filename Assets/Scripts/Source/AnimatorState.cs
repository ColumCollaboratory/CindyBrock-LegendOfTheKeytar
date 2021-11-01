using System;
using UnityEngine;

public interface IAnimatorState { }

[Serializable]
public sealed class AnimatorState<TEnum> : IAnimatorState
    where TEnum : Enum
{
    [Serializable]
    private sealed class AnimatorBinding
    {
        [SerializeField] public string boolName;
        [SerializeField] public int hashValue;
        [SerializeField] public TEnum enumValue;
    }


    [SerializeField] private Animator boundAnimator = null;

    [SerializeField] private TEnum state = default;

    [SerializeField] private AnimatorBinding[] bindings = null;

    private bool hasInitialized;

    public AnimatorState()
    {
        bindings = new AnimatorBinding[0];
    }

    public TEnum State
    {
        get => state;
        set
        {
            state = value;
            // Update the animator, this works with normal
            // and Flags enums.
            CheckGenerateHashes();
            foreach (AnimatorBinding binding in bindings)
                boundAnimator.SetBool(binding.hashValue, state.HasFlag(binding.enumValue));
        }
    }

    private void CheckGenerateHashes()
    {
        // If the hash values have not been generated,
        // iterate through them so that future state changes
        // will not require hashing.
        if (bindings[0].hashValue == default)
            foreach (AnimatorBinding binding in bindings)
                binding.hashValue = Animator.StringToHash(binding.boolName);
    }
}
