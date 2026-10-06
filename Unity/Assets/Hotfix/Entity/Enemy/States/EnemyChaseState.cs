using Hotfix.Gameplay.Core;

namespace Hotfix.Gameplay.Enemy.States
{
    public sealed class EnemyChaseState : StateBase<EnemyController>
    {
        public EnemyChaseState(EnemyController owner) : base(owner)
        {
        }

        public override void Enter()
        {
            Owner.PlayMoveStateAnimation();
        }

        public override void Update(float deltaTime)
        {
            Owner.FaceTarget();
        }
    }
}
