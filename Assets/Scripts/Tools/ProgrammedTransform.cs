using UnityEngine;

namespace Tools
{
    /// <summary>
    /// Adding this component overrides the standard transform
    /// manipulation in the Unity Editor, allowing it to instead
    /// be driven via code. This script is only meant for edit-time
    /// and becomes disabled during play mode.
    /// </summary>
    [ExecuteInEditMode]
    [DisallowMultipleComponent]
    [AddComponentMenu("")] // Hides this from AddComponent menu.
    public sealed class ProgrammedTransform : MonoBehaviour, IRequestingBuildDeletion
    {
        #region Enums      | State
        /// <summary>
        /// Defines how the programmed transform is
        /// displayed to the editor.
        /// </summary>
        public enum Visibility : byte
        {
            /// <summary>
            /// The transform component is completely hidden.
            /// </summary>
            Hidden,
            /// <summary>
            /// The transform component is shown but the fields are readonly.
            /// </summary>
            ShowAsReadonly
        }
        #endregion
        #region Fields     | State
        // Store the locked transform values.
        private Vector3 position;
        private Quaternion rotation;
        private Vector3 scale;
        // Store how to display the transform.
        private Visibility visibility;
        #endregion
        #region Properties | State
        /// <summary>
        /// How the transform is currently being rendered.
        /// </summary>
        public Visibility CurrentVisibility
        {
            get => visibility;
            set
            {
                visibility = value;
                // Update inspector visibility.
                switch (visibility)
                {
                    case Visibility.Hidden:
                        transform.hideFlags = HideFlags.HideInInspector;
                        break;
                    case Visibility.ShowAsReadonly:
                        transform.hideFlags = HideFlags.NotEditable;
                        break;
                }
            }
        }
        #endregion
        #region Properties | Transform Wrapping
        /// <summary>
        /// The global transform position.
        /// </summary>
        public Vector3 Position
        {
            get => position;
            set
            {
                position = value;
                transform.position = position;
            }
        }
        /// <summary>
        /// The global transform rotation.
        /// </summary>
        public Quaternion Rotation
        {
            get => rotation;
            set
            {
                rotation = value;
                transform.rotation = rotation;
            }
        }
        /// <summary>
        /// The local transform scale.
        /// </summary>
        public Vector3 Scale
        {
            get => scale;
            set
            {
                scale = value;
                transform.localScale = scale;
            }
        }
        #endregion
        #region Methods    | MonoBehaviour Overrides
        private void OnEnable() => Initialize();
        private void Reset() => Initialize();
        private void Initialize()
        {
            // Always hide this component in the inspector.
            this.hideFlags = HideFlags.HideInInspector;
            // Set the transform visibility based on setting
            // that was initialized or loaded from serialized.
            CurrentVisibility = visibility;
            // Set the underlying transform lock values
            // to the current transform values.
            position = transform.position;
            rotation = transform.rotation;
            scale = transform.localScale;
        }
        private void OnDestroy() => DeInitialize();
        private void OnDisable() => DeInitialize();
        private void DeInitialize()
        {
            // Reveal the transform when this component
            // is disabled or destroyed.
            transform.hideFlags = HideFlags.None;
        }
        private void Update()
        {
            // Transform lock is disabled during play mode.
            if (!Application.isPlaying)
            {
                // Prevent changes to this transform
                // that aran't made via this scripts
                // properties.
                if (transform.position != position)
                    transform.position = position;
                if (transform.rotation != rotation)
                    transform.rotation = rotation;
                if (transform.localScale != scale)
                    transform.localScale = scale;
            }
        }
        #endregion
    }
}
