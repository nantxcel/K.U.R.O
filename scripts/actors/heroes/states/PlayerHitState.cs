using Godot;
using System;

namespace Kuros.Actors.Heroes.States
{
    public partial class PlayerHitState : PlayerState
    {
        private float _stunTimer = 0.0f;
        
        public override void Enter()
        {
            Actor.Velocity = Vector2.Zero;
            Actor.AnimPlayer?.Play("animations/hit");
            
            // Set default stun duration or calculate based on damage
            _stunTimer = 0.6f;
            
            // Optional: Add knockback force if we had access to damage source
        }

        public override void PhysicsUpdate(double delta)
        {
            _stunTimer -= (float)delta;
            
            if (_stunTimer <= 0)
            {
                ChangeState("Idle");
                return;
            }
            
            // While stunned, we can still be moved by external forces (gravity, knockback)
            // but for now we just apply friction/stop
             Actor.Velocity = Actor.Velocity.MoveToward(Vector2.Zero, Actor.Speed * (float)delta);
             Actor.MoveAndSlide();
        }
    }
}

