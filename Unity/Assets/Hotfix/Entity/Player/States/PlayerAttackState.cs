using Hotfix.Gameplay.Core;

namespace Hotfix.Gameplay.Player.States
{
    public sealed class PlayerAttackState : StateBase<PlayerController>
    {
        public PlayerAttackState(PlayerController owner) : base(owner)
        {
        }

        public override void Enter()
        {
            Owner.PlayAttackStateAnimation();
        }
    }
}
