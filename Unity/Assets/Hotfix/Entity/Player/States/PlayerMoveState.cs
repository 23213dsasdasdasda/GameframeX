using Hotfix.Gameplay.Core;
using UnityEngine;

namespace Hotfix.Gameplay.Player.States
{
    public sealed class PlayerMoveState : StateBase<PlayerController>
    {
        public PlayerMoveState(PlayerController owner) : base(owner)
        {
        }

        public override void Enter()
        {
            Owner.PlayMoveStateAnimation();
        }

        public override void Update(float deltaTime)
        {
            var move = new Vector3(Owner.MoveInput.x, 0f, Owner.MoveInput.y);
            if (move.sqrMagnitude <= 0.0001f)
            {
                return;
            }

            Owner.FaceDirection(Owner.AimDirection);
        }
    }
}
