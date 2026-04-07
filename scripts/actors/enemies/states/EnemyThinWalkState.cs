using Godot;

namespace Kuros.Actors.Enemies.States
{
    public partial class EnemyThinWalkState : EnemyWalkState
    {
        public override void PhysicsUpdate(double delta)
        {
            if (Enemy.StateMachine?.HasState("EnemySpawn") == true && ShouldEnterEnemySpawnState())
            {
                ChangeState("EnemySpawn");
                return;
            }

            if (Enemy.StateMachine?.HasState("DashBack") == true && Enemy.IsPlayerAttacking() && Enemy.IsEnemyInPlayerAttackRange())
            {
                ChangeState("DashBack");
                return;
            }

            base.PhysicsUpdate(delta);
        }

        private bool ShouldEnterEnemySpawnState()
        {
            var spawnState = Enemy.StateMachine?.GetNodeOrNull<EnemySpawnState>("EnemySpawn");
            return spawnState?.ShouldTriggerOnLowHealth() == true;
        }
    }
}
