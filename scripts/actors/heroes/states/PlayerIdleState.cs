using Godot;
using System;

namespace Kuros.Actors.Heroes.States
{
	public partial class PlayerIdleState : PlayerState
	{
		public override void Enter()
		{
			Actor.AnimPlayer?.Play("animations/Idle");
			Actor.Velocity = Vector2.Zero;
		}

		public override void PhysicsUpdate(double delta)
		{
			// Check for transitions
			if (Input.IsActionJustPressed("attack") && Actor.AttackTimer <= 0)
			{
				ChangeState("Attack");
				return;
			}
			
			Vector2 input = GetMovementInput();
			if (input != Vector2.Zero)
			{
				if (Input.IsActionPressed("run"))
				{
					ChangeState("Run");
				}
				else
				{
					ChangeState("Walk");
				}
				return;
			}
			
			// Apply friction/stop
			Actor.Velocity = Actor.Velocity.MoveToward(Vector2.Zero, Actor.Speed * 2 * (float)delta);
			Actor.MoveAndSlide();
			Actor.ClampPositionToScreen();
		}
	}
}
