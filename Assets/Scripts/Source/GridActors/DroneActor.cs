using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.InputSystem;

namespace BattleRoyalRhythm.GridActors
{
    /// <summary>
    /// An actor that can be controlled. It can fly around
    /// and traverse seams, but does not interact with
    /// other actors.
    /// </summary>
    public sealed class DroneActor : GridActor
    {
        private enum MovementMode : byte
        {
            GridUnit,
            Continuous
        }

        [SerializeField] private MovementMode movementMode = MovementMode.GridUnit;
        [SerializeField][Min(0f)] private float speed = 1f;

        protected override sealed void Update()
        {
            base.Update();
            if (World != null)
            {
                bool enteredDoorway = false;
                switch (movementMode)
                {
                    case MovementMode.GridUnit:
                        if (Keyboard.current.wKey.wasPressedThisFrame)
                        {
                            enteredDoorway = World.TryTurnForwards(this);
                            if (!enteredDoorway)
                                World.TranslateActor(this, Vector2.up);
                        }
                        else if (Keyboard.current.sKey.wasPressedThisFrame)
                            World.TranslateActor(this, Vector2.down);
                        else if (!enteredDoorway)
                        {
                            if (Keyboard.current.aKey.wasPressedThisFrame)
                                World.TranslateActor(this, Vector2.left);
                            else if (Keyboard.current.dKey.wasPressedThisFrame)
                                World.TranslateActor(this, Vector2.right);
                        }
                        Location = Tile;
                        break;
                    case MovementMode.Continuous:
                        Vector2 movement = Vector2.zero;
                        if (Keyboard.current.wKey.wasPressedThisFrame)
                        {
                            enteredDoorway = World.TryTurnForwards(this);
                        }
                        else if (Keyboard.current.wKey.isPressed)
                            movement += Vector2.up;
                        if (Keyboard.current.aKey.isPressed)
                            movement += Vector2.left;
                        if (Keyboard.current.sKey.isPressed)
                            movement += Vector2.down;
                        if (Keyboard.current.dKey.isPressed)
                            movement += Vector2.right;
                        if (enteredDoorway)
                            movement.x = 0;
                        movement = movement.normalized * speed * Time.deltaTime;
                        World.TranslateActor(this, movement);
                        break;
                }
            }
        }
    }
}
