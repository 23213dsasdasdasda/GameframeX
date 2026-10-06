using Hotfix.Gameplay.Core;

namespace Hotfix.Gameplay.Enemy.States
{
    public sealed class EnemyIdleState : StateBase<EnemyController>
    {
        public EnemyIdleState(EnemyController owner) : base(owner)
        {
        }

        public override void Enter()
        {
            Owner.PlayIdleStateAnimation();
        }
    }
}
