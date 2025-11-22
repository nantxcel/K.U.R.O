using Godot;
using System;
using Kuros.Systems.FSM;

namespace Kuros.Core
{
    public partial class GameActor : CharacterBody2D
    {
        [ExportCategory("Stats")]
        [Export] public float Speed = 300.0f;
        [Export] public float AttackDamage = 25.0f;
        [Export] public float AttackRange = 100.0f;
        [Export] public float AttackCooldown = 0.5f;
        [Export] public int MaxHealth = 100;
        
        [ExportCategory("Components")]
        [Export] public StateMachine StateMachine { get; private set; } = null!;

        // Exposed state for States to use
        public int CurrentHealth { get; protected set; }
        public float AttackTimer { get; set; } = 0.0f;
        public bool FacingRight { get; protected set; } = true;
        public AnimationPlayer? AnimPlayer => _animationPlayer;
        
        protected Node2D _spineCharacter = null!;
        protected Sprite2D _sprite = null!;
        protected AnimationPlayer _animationPlayer = null!;

        public override void _Ready()
        {
            CurrentHealth = MaxHealth;
            
            // Node fetching
            _spineCharacter = GetNodeOrNull<Node2D>("SpineCharacter");
            _sprite = GetNodeOrNull<Sprite2D>("Sprite2D");
            
            if (_spineCharacter != null)
            {
                _animationPlayer = _spineCharacter.GetNodeOrNull<AnimationPlayer>("AnimationPlayer");
            }
            
            // Initialize StateMachine if manually assigned or found
            if (StateMachine == null)
            {
                StateMachine = GetNodeOrNull<StateMachine>("StateMachine");
            }

            if (StateMachine != null)
            {
                StateMachine.Initialize(this);
            }
        }

        public override void _PhysicsProcess(double delta)
        {
            if (AttackTimer > 0) AttackTimer -= (float)delta;
            
            // FSM handles logic, but we can keep global helpers here
            // If using FSM, ensure it is processed either here or by itself (Node process)
            // StateMachine._PhysicsProcess is called automatically by Godot if it's in the tree
        }

        public virtual void TakeDamage(int damage)
        {
            CurrentHealth -= damage;
            CurrentHealth = Mathf.Max(CurrentHealth, 0);
            
            GD.Print($"{Name} took {damage} damage! Health: {CurrentHealth}");
            
            FlashDamageEffect();

            if (CurrentHealth <= 0)
            {
                Die();
            }
            else
            {
                // Force state change to Hit
                if (StateMachine != null)
                {
                    StateMachine.ChangeState("Hit");
                }
            }
        }

        protected virtual void Die()
        {
            QueueFree();
        }

        protected virtual void FlashDamageEffect()
        {
            Node2D visualNode = _spineCharacter ?? (Node2D)_sprite;
            if (visualNode != null)
            {
                var originalColor = visualNode.Modulate;
                visualNode.Modulate = new Color(1, 0, 0); 
                
                var tween = CreateTween();
                tween.TweenInterval(0.1);
                tween.TweenCallback(Callable.From(() => visualNode.Modulate = originalColor));
            }
        }

        public void FlipFacing(bool faceRight)
        {
            if (FacingRight == faceRight) return;
            
            FacingRight = faceRight;
            
            if (_spineCharacter != null)
            {
                var scale = _spineCharacter.Scale;
                float absX = Mathf.Abs(scale.X);
                _spineCharacter.Scale = new Vector2(faceRight ? absX : -absX, scale.Y);
            }
            else if (_sprite != null)
            {
                _sprite.FlipH = !FacingRight;
            }
        }
        
        public void ClampPositionToScreen(float margin = 50f, float bottomOffset = 150f)
        {
             var screenSize = GetViewportRect().Size;
             GlobalPosition = new Vector2(
                Mathf.Clamp(GlobalPosition.X, margin, screenSize.X - margin),
                Mathf.Clamp(GlobalPosition.Y, margin, screenSize.Y - bottomOffset)
            );
        }
    }
}
