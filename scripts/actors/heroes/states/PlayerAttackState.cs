using Godot;
using System;

namespace Kuros.Actors.Heroes.States
{
	public partial class PlayerAttackState : PlayerState
	{
		public override void Enter()
		{
			Actor.Velocity = Vector2.Zero; // Stop moving when attacking
			
			if (Actor.AnimPlayer != null)
			{
				Actor.AnimPlayer.Play("animations/attack");
				// We hook into the animation finished signal via GameActor or just wait time
				// For better FSM, we can listen to the signal locally or check IsPlaying
			}
			
			// Perform the actual attack logic (damage dealing) immediately or via animation event
			// For this sample, we do it on Enter like before
			Player.PerformAttackCheck();
		}

		public override void PhysicsUpdate(double delta)
		{
			// Wait for animation to finish
			if (Actor.AnimPlayer != null && !Actor.AnimPlayer.IsPlaying())
			{
				ChangeState("Idle");
			}
			// Fallback if no animation player
			else if (Actor.AnimPlayer == null)
			{
				 ChangeState("Idle");
			}
		}
		
		// Note: In a production env, we'd use AnimationPlayer signals or Call Method tracks 
		// to trigger the "End of Attack" transition, but polling IsPlaying is okay for simple setups.
	}
}
