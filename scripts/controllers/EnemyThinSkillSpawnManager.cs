using Godot;
using Kuros.Core;

namespace Kuros.Controllers
{
    /// <summary>
    /// EnemyThin 专用召唤管理器：复用 EnemySpawnManager 的生成/特效逻辑，
    /// 在宿主敌人进入指定状态后自动触发，而不依赖玩家进入触发范围。
    /// </summary>
    [Tool]
    [GlobalClass]
    public partial class EnemyThinSkillSpawnManager : EnemySpawnManager
    {
        [ExportCategory("Auto Trigger")]
        [Export] public bool AutoTriggerEnabled { get; set; } = true;
        [Export(PropertyHint.Range, "0,10,0.05")] public float AutoTriggerDelay { get; set; } = 0.0f;
        [Export] public bool DisablePlayerTriggerArea { get; set; } = true;
        [Export] public string TriggerStateName { get; set; } = "EnemySpawn";
        [Export] public bool ResetTriggerOnStateEntry { get; set; } = true;

        private GameActor? _ownerActor;
        private bool _wasInTriggerState;
        private bool _waitingForStateTrigger;

        public override void _Ready()
        {
            SpawnOnReady = false;
            AssignDefaultSpawnParent();
            base._Ready();

            _ownerActor = GetParentOrNull<GameActor>();

            if (DisablePlayerTriggerArea && TriggerArea != null)
            {
                TriggerArea.Monitoring = false;
                TriggerArea.Monitorable = false;
                TriggerArea.CollisionMask = 0;
            }
        }

        public override void _Process(double delta)
        {
            base._Process(delta);

            if (Engine.IsEditorHint() || !AutoTriggerEnabled)
            {
                return;
            }

            _ownerActor ??= GetParentOrNull<GameActor>();
            string? currentStateName = _ownerActor?.StateMachine?.CurrentState?.Name;
            bool isInTriggerState = !string.IsNullOrEmpty(currentStateName)
                && currentStateName == TriggerStateName;

            if (isInTriggerState && !_wasInTriggerState && !_waitingForStateTrigger)
            {
                _ = AutoStartAsync();
            }

            _wasInTriggerState = isInTriggerState;
        }

        public void TriggerAutoSpawnNow()
        {
            StartSpawnSequence();
        }

        private void AssignDefaultSpawnParent()
        {
            if (!SpawnParentPath.IsEmpty)
            {
                return;
            }

            Node? currentScene = GetTree()?.CurrentScene;
            if (currentScene == null || !GodotObject.IsInstanceValid(currentScene))
            {
                return;
            }

            Node? worldNode = currentScene.GetNodeOrNull<Node>("World");
            SpawnParentPath = worldNode?.GetPath() ?? currentScene.GetPath();
        }

        private async System.Threading.Tasks.Task AutoStartAsync()
        {
            _waitingForStateTrigger = true;

            try
            {
                float delay = Mathf.Max(0f, AutoTriggerDelay);
                if (delay > 0f)
                {
                    var timer = GetTree().CreateTimer(delay);
                    await ToSignal(timer, SceneTreeTimer.SignalName.Timeout);
                }

                if (!GodotObject.IsInstanceValid(this))
                {
                    return;
                }

                _ownerActor ??= GetParentOrNull<GameActor>();
                if (_ownerActor?.StateMachine?.CurrentState?.Name != TriggerStateName)
                {
                    return;
                }

                if (ResetTriggerOnStateEntry)
                {
                    ResetTrigger();
                }

                StartSpawnSequence();
            }
            finally
            {
                _waitingForStateTrigger = false;
            }
        }
    }
}
