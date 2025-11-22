using Godot;
using System;
using Kuros.Systems.FSM;
using Kuros.Core;

namespace Kuros.Actors.Heroes.States
{
    public partial class PlayerState : State
    {
        protected SamplePlayer Player => (SamplePlayer)Actor;
        
        protected Vector2 GetMovementInput()
        {
            return Input.GetVector("move_left", "move_right", "move_forward", "move_back");
        }
    }
}

