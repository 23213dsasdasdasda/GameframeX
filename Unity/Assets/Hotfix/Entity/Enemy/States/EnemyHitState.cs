using Hotfix.Gameplay.Core;

namespace Hotfix.Gameplay.Enemy.States
{
    public sealed class EnemyHitState : StateBase<EnemyController>
    {
        public EnemyHitState(EnemyController owner) : base(owner)
        {
        }

        public override void Enter()
        {
            Owner.PlayHitStateAnimation();
        }
    }
}
