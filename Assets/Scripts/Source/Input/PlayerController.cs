using UnityEngine;
using CindyBrock.Input.Generated;

namespace CindyBrock.Input
{
    /// <summary>
    /// Implements the player controller by wrapping the Unity Input System package.
    /// </summary>
    public sealed class PlayerController : MonoBehaviour, IPlayerController
    {
        #region Controller State
        private Controls controller;
        #endregion
        #region Player Controller Properties
        /// <summary>
        /// The timestamp of the latest input.
        /// </summary>
        public float LatestTimestamp { get; private set; }
        /// <summary>
        /// The type of the latest input.
        /// </summary>
        public PlayerAction LatestAction { get; private set; }
        #endregion
        #region Initialization & Deinitialization
        private void Awake()
        {
            // Create the controller.
            controller = new Controls();
            // Bind to all of the relevant controls.
            controller.PlayerControl.SetGenreA.performed += GenreAButtonDown;
            controller.PlayerControl.SetGenreB.performed += GenreBButtonDown;
            controller.PlayerControl.SetGenreC.performed += GenreCButtonDown;
            controller.PlayerControl.SetGenreD.performed += GenreDButtonDown;
            controller.PlayerControl.MoveLeft.performed += MoveLeftButtonDown;
            controller.PlayerControl.MoveRight.performed += MoveRightButtonDown;
            controller.PlayerControl.Duck.performed += DuckButtonDown;
            controller.PlayerControl.Jump.performed += JumpButtonDown;
            controller.PlayerControl.Attack.performed += AttackButtonDown;
            controller.PlayerControl.Ability.performed += AbilityButtonDown;
        }
        private void OnDestroy()
        {
            // Unbind from controls to assist clean up.
            controller.PlayerControl.SetGenreA.performed -= GenreAButtonDown;
            controller.PlayerControl.SetGenreB.performed -= GenreBButtonDown;
            controller.PlayerControl.SetGenreC.performed -= GenreCButtonDown;
            controller.PlayerControl.SetGenreD.performed -= GenreDButtonDown;
            controller.PlayerControl.MoveLeft.performed -= MoveLeftButtonDown;
            controller.PlayerControl.MoveRight.performed -= MoveRightButtonDown;
            controller.PlayerControl.Duck.performed -= DuckButtonDown;
            controller.PlayerControl.Jump.performed -= JumpButtonDown;
            controller.PlayerControl.Attack.performed -= AttackButtonDown;
            controller.PlayerControl.Ability.performed -= AbilityButtonDown;
            // Dispose of the controller.
            controller.Dispose();
        }
        // Activate/Deactivate Input as the component is toggled on/off.
        private void OnEnable() => controller.Enable();
        private void OnDisable() => controller.Disable();
        #endregion
        #region Input Handlers
        private float CurrentTime => Time.fixedTime;
        private void GenreAButtonDown(UnityEngine.InputSystem.InputAction.CallbackContext obj)
        {
            LatestAction = PlayerAction.SetGenre1;
            LatestTimestamp = CurrentTime;
        }
        private void GenreBButtonDown(UnityEngine.InputSystem.InputAction.CallbackContext obj)
        {
            LatestAction = PlayerAction.SetGenre2;
            LatestTimestamp = CurrentTime;
        }
        private void GenreCButtonDown(UnityEngine.InputSystem.InputAction.CallbackContext obj)
        {
            LatestAction = PlayerAction.SetGenre3;
            LatestTimestamp = CurrentTime;
        }
        private void GenreDButtonDown(UnityEngine.InputSystem.InputAction.CallbackContext obj)
        {
            LatestAction = PlayerAction.SetGenre4;
            LatestTimestamp = CurrentTime;
        }
        private void MoveLeftButtonDown(UnityEngine.InputSystem.InputAction.CallbackContext obj)
        {
            LatestAction = PlayerAction.MoveLeft;
            LatestTimestamp = CurrentTime;
        }
        private void MoveRightButtonDown(UnityEngine.InputSystem.InputAction.CallbackContext obj)
        {
            LatestAction = PlayerAction.MoveRight;
            LatestTimestamp = CurrentTime;
        }
        private void DuckButtonDown(UnityEngine.InputSystem.InputAction.CallbackContext obj)
        {
            LatestAction = PlayerAction.Duck;
            LatestTimestamp = CurrentTime;
        }
        private void JumpButtonDown(UnityEngine.InputSystem.InputAction.CallbackContext obj)
        {
            LatestAction = PlayerAction.Jump;
            LatestTimestamp = CurrentTime;
        }
        private void AttackButtonDown(UnityEngine.InputSystem.InputAction.CallbackContext obj)
        {
            LatestAction = PlayerAction.Attack;
            LatestTimestamp = CurrentTime;
        }
        private void AbilityButtonDown(UnityEngine.InputSystem.InputAction.CallbackContext obj)
        {
            LatestAction = PlayerAction.UseAbility;
            LatestTimestamp = CurrentTime;
        }
        #endregion
    }
}
