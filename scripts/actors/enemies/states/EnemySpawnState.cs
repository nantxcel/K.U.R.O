using System.Collections.Generic;
using Godot;

namespace Kuros.Actors.Enemies.States
{
    public partial class EnemySpawnState : EnemyState
    {
        [ExportCategory("Low Health Trigger")]
        [Export] public float[] LowHealthThresholds = new float[] { 0.5f, 0.3f };

        [Export(PropertyHint.Range, "0.1,10,0.1")] public float DurationSeconds = 2.0f;

        [Export] public bool EnableSuperArmor = true;

        [Export]
        public string NextStateName = "Walk";

        private float _timer;
        private int _triggeredThresholdIndex;
        private float _cachedSpeed;
        private bool? _previousIgnoreHitStateOnDamage;
        private float[] _sortedThresholds = new float[0];

        protected override void _ReadyState()
        {
            RefreshThresholdCache();
        }

        public override void Enter()
        {
            _cachedSpeed = Enemy.Speed;
            Enemy.Speed = 0f;
            Enemy.Velocity = Vector2.Zero;
            _timer = Mathf.Max(DurationSeconds, 0.01f);
            ConsumeCurrentThresholdTrigger();

            if (EnableSuperArmor)
            {
                _previousIgnoreHitStateOnDamage = Enemy.IgnoreHitStateOnDamage;
                Enemy.IgnoreHitStateOnDamage = true;
            }
        }

        public override void Exit()
        {
            Enemy.Speed = _cachedSpeed;
            Enemy.Velocity = Vector2.Zero;

            if (_previousIgnoreHitStateOnDamage.HasValue)
            {
                Enemy.IgnoreHitStateOnDamage = _previousIgnoreHitStateOnDamage.Value;
                _previousIgnoreHitStateOnDamage = null;
            }
        }

        public override bool CanEnterFrom(string? currentStateName)
        {
            return ShouldTriggerOnLowHealth();
        }

        public override bool CanExitTo(string nextStateName)
        {
            if (_timer > 0f)
            {
                if (EnableSuperArmor)
                {
                    return nextStateName == "Dying" || nextStateName == "Dead";
                }

                return nextStateName == "Hit"
                    || nextStateName == "Frozen"
                    || nextStateName == "CooldownFrozen"
                    || nextStateName == "Dying"
                    || nextStateName == "Dead";
            }

            return true;
        }

        public override void PhysicsUpdate(double delta)
        {
            Enemy.Velocity = Vector2.Zero;

            _timer -= (float)delta;
            if (_timer > 0f)
            {
                return;
            }

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

        public bool ShouldTriggerOnLowHealth()
        {
            if (_sortedThresholds.Length == 0)
            {
                RefreshThresholdCache();
            }

            if (Enemy.MaxHealth <= 0 || _triggeredThresholdIndex >= _sortedThresholds.Length)
            {
                return false;
            }

            float healthRatio = (float)Enemy.CurrentHealth / Enemy.MaxHealth;
            return healthRatio <= _sortedThresholds[_triggeredThresholdIndex];
        }

        private void ConsumeCurrentThresholdTrigger()
        {
            if (ShouldTriggerOnLowHealth())
            {
                _triggeredThresholdIndex++;
            }
        }

        private void RefreshThresholdCache()
        {
            var thresholds = new List<float>();

            if (LowHealthThresholds != null)
            {
                foreach (float threshold in LowHealthThresholds)
                {
                    float clampedThreshold = Mathf.Clamp(threshold, 0.01f, 1.0f);
                    if (!thresholds.Contains(clampedThreshold))
                    {
                        thresholds.Add(clampedThreshold);
                    }
                }
            }

            thresholds.Sort((left, right) => right.CompareTo(left));
            _sortedThresholds = thresholds.ToArray();
        }
    }
}

