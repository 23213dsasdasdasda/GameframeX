using Hotfix.Gameplay.Core;

namespace Hotfix.Gameplay.Player.States
{
    public sealed class PlayerDeadState : StateBase<PlayerController>
    {
        public PlayerDeadState(PlayerController owner) : base(owner)
        {
        }

        public override void Enter()
        {
            Owner.PlayDeathStateAnimation();
        }
    }
}
