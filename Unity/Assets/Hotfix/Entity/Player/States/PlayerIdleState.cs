using Hotfix.Gameplay.Core;

namespace Hotfix.Gameplay.Player.States
{
    public sealed class PlayerIdleState : StateBase<PlayerController>
    {
        public PlayerIdleState(PlayerController owner) : base(owner)
        {
        }

        public override void Enter()
        {
            Owner.PlayIdleStateAnimation();
        }
    }
}
