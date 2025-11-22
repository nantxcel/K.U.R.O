using Godot;
using System;
using Kuros.Core;
using Kuros.Systems.FSM;

public partial class SamplePlayer : GameActor
{
	private Area2D _attackArea = null!;
	private Label _statsLabel = null!;
	private int _score = 0;
	private ColorRect _attackVisualization = null!;
	
	public override void _Ready()
	{
		// Initialize base (stats, nodes, animations, fsm)
		base._Ready();
		
		_attackArea = GetNode<Area2D>("AttackArea");
		_statsLabel = GetNode<Label>("../UI/PlayerStats");
		
		UpdateStatsUI();
		
		// Create attack visualization
		_attackVisualization = new ColorRect();
		_attackVisualization.Size = new Vector2(80, 60);
		_attackVisualization.Color = new Color(1, 0, 0, 0.3f);
		_attackVisualization.Position = new Vector2(10, -30);
		AddChild(_attackVisualization);
		_attackVisualization.Visible = false;
	}
	
	// Removed _PhysicsProcess - Now handled by FSM!
	
	public void PerformAttackCheck()
	{
		AttackTimer = AttackCooldown;
		
		GD.Print($"=== Player attacking! ===");
		
		int hitCount = 0;
		float facingDirection = FacingRight ? 1 : -1;
		
		var parent = GetParent();
		foreach (Node child in parent.GetChildren())
		{
			if (child is SampleEnemy enemy)
			{
				Vector2 playerPos = GlobalPosition;
				Vector2 enemyPos = enemy.GlobalPosition;
				Vector2 toEnemy = enemyPos - playerPos;
				float distance = toEnemy.Length();
				
				bool inRange = distance <= AttackRange;
				bool correctDirection = (facingDirection > 0 && toEnemy.X > 0) || 
									   (facingDirection < 0 && toEnemy.X < 0);
				bool inVerticalRange = Mathf.Abs(toEnemy.Y) <= 80.0f;
				
				GD.Print($"Enemy at {enemyPos}, distance: {distance:F2}, direction OK: {correctDirection}, vertical OK: {inVerticalRange}");
				
				if (inRange && correctDirection && inVerticalRange)
				{
					enemy.TakeDamage((int)AttackDamage);
					hitCount++;
					GD.Print($"Hit enemy! Distance: {distance:F2}");
				}
			}
		}
		
		if (hitCount == 0)
		{
			GD.Print("No enemies hit!");
		}
		
		if (_attackVisualization != null)
		{
			_attackVisualization.Visible = true;
			_attackVisualization.Position = FacingRight ? new Vector2(10, -30) : new Vector2(-90, -30);
			
			var vizTween = CreateTween();
			vizTween.TweenInterval(0.2);
			vizTween.TweenCallback(Callable.From(() => _attackVisualization.Visible = false));
		}
	}
	
	public override void TakeDamage(int damage)
	{
		base.TakeDamage(damage);
		UpdateStatsUI();
	}
	
	public void AddScore(int points)
	{
		_score += points;
		UpdateStatsUI();
	}
	
	private void UpdateStatsUI()
	{
		if (_statsLabel != null)
		{
			_statsLabel.Text = $"Player HP: {CurrentHealth}\nScore: {_score}";
		}
	}
	
	protected override void Die()
	{
		GD.Print("Player died! Game Over!");
		GetTree().ReloadCurrentScene();
	}
}
