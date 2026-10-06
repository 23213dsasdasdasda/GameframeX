using Hotfix.Gameplay.Core;
using Hotfix.Gameplay.Player.States;
using Hotfix.Config;
using UnityEngine;

namespace Hotfix.Gameplay.Player
{
    public sealed class PlayerController : EntityBase
    {
        private PlayerIdleState _idleState;
        private PlayerMoveState _moveState;
        private PlayerAttackState _attackState;
        private PlayerDeadState _deadState;
        private PlayerAnimationDriver _animationDriver;

        public Vector2 MoveInput { get; private set; }
        public Vector3 AimDirection { get; private set; } = Vector3.forward;
        public bool FirePressed { get; private set; }
        public bool DodgePressed { get; private set; }
        public bool PickupPressed { get; private set; }
        public float Gravity { get; private set; } = -24f;
        public float ExternalVelocityDamping { get; private set; } = 7f;
        public float RotationLerpSpeed { get; private set; } = 18f;
        public float VerticalVelocity { get; private set; }
        public Vector3 ExternalVelocity { get; private set; }

        public void InitializeRuntime(float maxHp, float moveSpeed, float gravity, float externalVelocityDamping, float rotationLerpSpeed)
        {
            InitializeStats(maxHp, moveSpeed);
            Gravity = gravity;
            ExternalVelocityDamping = externalVelocityDamping;
            RotationLerpSpeed = rotationLerpSpeed;
            TickFromMonoUpdate = false;
            CacheComponents();
            EnsureStates();
            _animationDriver ??= new PlayerAnimationDriver(Anim);
            _animationDriver.SetWeaponType(EWeaponType.Normal);
            ChangeState(_idleState);
        }

        public void SetWeaponType(EWeaponType weaponType)
        {
            _animationDriver ??= new PlayerAnimationDriver(Anim);
            _animationDriver.SetWeaponType(weaponType);

            if (IsDead)
            {
                _animationDriver.PlayDeath();
            }
            else if (MoveInput.sqrMagnitude > 0.0001f)
            {
                _animationDriver.PlayMove();
            }
            else
            {
                _animationDriver.PlayIdle();
            }
        }

        public void SetCommand(Vector2 moveInput, Vector3 aimDirection, bool firePressed, bool dodgePressed, bool pickupPressed)
        {
            MoveInput = moveInput;
            AimDirection = aimDirection.sqrMagnitude > 0.0001f ? aimDirection.normalized : transform.forward;
            FirePressed = firePressed;
            DodgePressed = dodgePressed;
            PickupPressed = pickupPressed;
        }

        public void ClearTransientCommandFlags()
        {
            FirePressed = false;
            DodgePressed = false;
            PickupPressed = false;
        }

        public void AddExternalVelocity(Vector3 velocity)
        {
            ExternalVelocity += velocity;
        }

        public void TickLocomotion(float deltaTime, Vector3 moveDirection, Vector3 aimDirection)
        {
            if (IsDead)
            {
                _animationDriver?.Tick(deltaTime);
                StateMachine.Update(deltaTime);
                return;
            }

            if (Controller != null && Controller.isGrounded && VerticalVelocity < 0f)
            {
                VerticalVelocity = -2f;
            }

            ExternalVelocity = Vector3.Lerp(ExternalVelocity, Vector3.zero, ExternalVelocityDamping * deltaTime);
            VerticalVelocity += Gravity * deltaTime;
            var velocity = moveDirection * MoveSpeed + ExternalVelocity + Vector3.up * VerticalVelocity;
            Move(velocity * deltaTime);

            if (aimDirection.sqrMagnitude > 0.01f)
            {
                var lookRotation = Quaternion.LookRotation(aimDirection.normalized, Vector3.up);
                transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, RotationLerpSpeed * deltaTime);
            }

            UpdateLocomotionState(moveDirection);
            _animationDriver?.Tick(deltaTime);
            StateMachine.Update(deltaTime);
        }

        public void NotifyAttack()
        {
            if (IsDead)
            {
                return;
            }

            if (MoveInput.sqrMagnitude > 0.0001f)
            {
                return;
            }

            EnsureStates();
            ChangeState(_attackState);
        }

        public override void OnDie()
        {
            base.OnDie();
            EnsureStates();
            _animationDriver?.PlayDeath();
            if (!StateMachine.IsCurrent<PlayerDeadState>())
            {
                ChangeState(_deadState);
            }
        }

        public void PlayIdleStateAnimation()
        {
            _animationDriver?.PlayIdle();
        }

        public void PlayMoveStateAnimation()
        {
            _animationDriver?.PlayMove();
        }

        public void PlayAttackStateAnimation()
        {
            _animationDriver?.PlayAttack();
        }

        public void PlayHitStateAnimation()
        {
            _animationDriver?.PlayHit();
        }

        public void PlayDeathStateAnimation()
        {
            _animationDriver?.PlayDeath();
        }

        private void EnsureStates()
        {
            _idleState ??= new PlayerIdleState(this);
            _moveState ??= new PlayerMoveState(this);
            _attackState ??= new PlayerAttackState(this);
            _deadState ??= new PlayerDeadState(this);
        }

        private void UpdateLocomotionState(Vector3 moveDirection)
        {
            EnsureStates();

            if (moveDirection.sqrMagnitude > 0.0001f)
            {
                ChangeState(_moveState);
            }
            else
            {
                ChangeState(_idleState);
            }
        }
    }
}
