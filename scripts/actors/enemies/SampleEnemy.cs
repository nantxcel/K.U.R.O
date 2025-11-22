using Godot;
using System;
using Kuros.Core;

public partial class SampleEnemy : GameActor
{
    [Export] public float DetectionRange = 300.0f;
    [Export] public int ScoreValue = 10;
    
    private SamplePlayer? _player;
    private RandomNumberGenerator _rng = new RandomNumberGenerator();
    private float _hitStunTimer = 0.0f; // Re-declared here as it was removed from base
    
    public SampleEnemy()
    {
        Speed = 150.0f;
        AttackRange = 80.0f;
        AttackDamage = 10.0f;
        AttackCooldown = 1.5f;
        MaxHealth = 50;
    }
    
    public override void _Ready()
    {
        base._Ready();
        _rng.Randomize();
        
        // Find player
        var parent = GetParent();
        if (parent != null)
        {
            _player = parent.GetNodeOrNull<SamplePlayer>("Player");
        }
    }
    
    public override void _PhysicsProcess(double delta)
    {
        if (_player == null) return;
        
        base._PhysicsProcess(delta);
        
        if (_hitStunTimer > 0)
        {
             _hitStunTimer -= (float)delta;
        }
        
        // Distance check
        Vector2 playerPos = _player.GlobalPosition;
        Vector2 enemyPos = GlobalPosition;
        float distanceToPlayer = playerPos.DistanceTo(enemyPos);
        
        Vector2 velocity = Velocity;
        
        if (_hitStunTimer > 0)
        {
            velocity = Vector2.Zero;
            Velocity = velocity;
            MoveAndSlide();
            return;
        }
        
        // AI Logic
        if (distanceToPlayer <= DetectionRange)
        {
            if (distanceToPlayer <= AttackRange)
            {
                // Stop and attack
                velocity = Vector2.Zero;
                
                if (AttackTimer <= 0 && _hitStunTimer <= 0) // Used Property AttackTimer from base
                {
                    AttackPlayer();
                }
            }
            else
            {
                // Chase
                Vector2 direction = (playerPos - enemyPos).Normalized();
                velocity = direction * Speed;
                
                // Face player
                if (direction.X != 0)
                {
                    FlipFacing(direction.X > 0);
                }
            }
        }
        else
        {
            // Idle behavior (slow down)
            velocity.X = Mathf.MoveToward(Velocity.X, 0, Speed * 2);
            velocity.Y = Mathf.MoveToward(Velocity.Y, 0, Speed * 2);
        }
        
        ClampPositionToScreen();
        
        Velocity = velocity;
        MoveAndSlide();
    }
    
    private void AttackPlayer()
    {
        AttackTimer = AttackCooldown; // Used Property AttackTimer
        if (_player != null)
        {
            _player.TakeDamage((int)AttackDamage);
            GD.Print("Enemy attacked player!");
        }
        
        // Attack visual effect (scaling)
        Node2D? visualNode = _spineCharacter ?? (Node2D?)_sprite;
        if (visualNode != null)
        {
            var originalScale = visualNode.Scale;
            var targetScale = new Vector2(
                originalScale.X * 1.3f,
                originalScale.Y * 1.3f
            );
            var tween = CreateTween();
            tween.TweenProperty(visualNode, "scale", targetScale, 0.15);
            tween.TweenProperty(visualNode, "scale", originalScale, 0.15);
        }
    }
    
    public override void TakeDamage(int damage)
    {
        base.TakeDamage(damage);
        // Enemy has shorter stun
        _hitStunTimer = 0.3f;
        
        // If we want to play hit animation manually since base FSM logic might not cover enemy without state machine
        if (_animationPlayer != null)
        {
             _animationPlayer.Play("animations/hit");
             // We'd need to listen to finish to go back to idle but for simple enemy without FSM, this is tricky
             // For now, just play it.
        }
    }
    
    protected override void Die()
    {
        GD.Print("Enemy died!");
        
        if (_player != null)
        {
            _player.AddScore(ScoreValue);
        }
        
        // Shrink and disappear
        Node2D? visualNode = _spineCharacter ?? (Node2D?)_sprite;
        if (visualNode != null)
        {
            var tween = CreateTween();
            tween.TweenProperty(visualNode, "scale", Vector2.Zero, 0.3);
            tween.TweenCallback(Callable.From(QueueFree));
        }
        else
        {
            QueueFree();
        }
    }
}
