using Hotfix.Gameplay.Core;

namespace Hotfix.Gameplay.Enemy.States
{
    public sealed class EnemyDeadState : StateBase<EnemyController>
    {
        public EnemyDeadState(EnemyController owner) : base(owner)
        {
        }

        public override void Enter()
        {
            Owner.PlayDeathStateAnimation();
        }
    }
}
