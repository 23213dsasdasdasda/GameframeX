using Hotfix.Gameplay.Core;

namespace Hotfix.Gameplay.Enemy.States
{
    public sealed class EnemyAttackState : StateBase<EnemyController>
    {
        public EnemyAttackState(EnemyController owner) : base(owner)
        {
        }

        public override void Enter()
        {
            Owner.FaceTarget();
            Owner.PlayAttackStateAnimation();
        }
    }
}
