using Godot;
using Kuros.Core;

namespace Kuros.Actors.Enemies.States
{
    /// <summary>
    /// 攻击后短暂后撤，结束后回到 Walk。
    /// </summary>
    public partial class EnemyDashBackState : EnemyState
    {
        [Export(PropertyHint.Range, "10,2000,10")]
        public float DashSpeed = 500.0f;

        [Export(PropertyHint.Range, "0.05,2,0.01")]
        public float DashDuration = 0.2f;

        [Export]
        public string AnimationName = "animations/walk";

        [Export]
        public string NextStateName = "Walk";

        [ExportCategory("Interrupt")]
        [Export]
        public bool EnableDamageImmunity = true;

        [Export]
        public bool EnableSuperArmor = true;

        private float _timer;
        private Vector2 _dashDirection = Vector2.Zero;
        private bool? _previousIgnoreHitStateOnDamage;

        public override void Enter()
        {
            _timer = Mathf.Max(DashDuration, 0.01f);

            Vector2 toPlayer = Enemy.GetDirectionToPlayer();
            if (toPlayer != Vector2.Zero)
            {
                _dashDirection = -toPlayer;
            }
            else
            {
                _dashDirection = Enemy.FacingRight ? Vector2.Left : Vector2.Right;
            }

            if (EnableSuperArmor)
            {
                _previousIgnoreHitStateOnDamage = Enemy.IgnoreHitStateOnDamage;
                Enemy.IgnoreHitStateOnDamage = true;
            }
            else
            {
                _previousIgnoreHitStateOnDamage = null;
            }

            Enemy.DamageIntercepted -= OnEnemyDamageIntercepted;
            if (EnableDamageImmunity)
            {
                Enemy.DamageIntercepted += OnEnemyDamageIntercepted;
            }

            if (Enemy.AnimPlayer != null && !string.IsNullOrEmpty(AnimationName) && Enemy.AnimPlayer.HasAnimation(AnimationName))
            {
                Enemy.AnimPlayer.Play(AnimationName);
            }
        }

        public override void Exit()
        {
            if (Enemy != null && GodotObject.IsInstanceValid(Enemy))
            {
                Enemy.Velocity = Vector2.Zero;
                Enemy.DamageIntercepted -= OnEnemyDamageIntercepted;

                if (_previousIgnoreHitStateOnDamage.HasValue)
                {
                    Enemy.IgnoreHitStateOnDamage = _previousIgnoreHitStateOnDamage.Value;
                    _previousIgnoreHitStateOnDamage = null;
                }
            }
        }

        public override bool CanExitTo(string nextStateName)
        {
            if (_timer <= 0f)
            {
                return true;
            }

            if (nextStateName == "Dying" || nextStateName == "Dead")
            {
                return true;
            }

            if (EnableSuperArmor)
            {
                return false;
            }

            return nextStateName == "Hit"
                || nextStateName == "Frozen"
                || nextStateName == "CooldownFrozen";
        }

        private bool OnEnemyDamageIntercepted(GameActor.DamageEventArgs args)
        {
            if (!EnableDamageImmunity || _timer <= 0f)
            {
                return false;
            }

            args.IsBlocked = true;
            return true;
        }

        public override void PhysicsUpdate(double delta)
        {
            if (Enemy == null || !GodotObject.IsInstanceValid(Enemy))
            {
                return;
            }

            _timer -= (float)delta;
            Enemy.Velocity = _dashDirection * DashSpeed;
            Enemy.MoveAndSlide();

            if (_timer <= 0f)
            {
                Enemy.Velocity = Vector2.Zero;

                if (Enemy.StateMachine != null)
                {
                    if (!string.IsNullOrEmpty(NextStateName) && Enemy.StateMachine.HasState(NextStateName))
                    {
                        Enemy.StateMachine.ChangeState(NextStateName);
                    }
                    else
                    {
                        Enemy.StateMachine.ChangeState("Walk");
                    }
                }
            }
        }
    }
}
