using Hotfix.Gameplay.Core;
using Hotfix.Gameplay.Enemy.States;
using UnityEngine;

namespace Hotfix.Gameplay.Enemy
{
    public sealed class EnemyController : EntityBase
    {
        private EnemyAnimationDriver _animationDriver;
        private EnemyIdleState _idleState;
        private EnemyChaseState _chaseState;
        private EnemyAttackState _attackState;
        private EnemyHitState _hitState;
        private EnemyDeadState _deadState;

        public Transform Target { get; private set; }
        public string AiType { get; private set; }
        public float AttackRange { get; private set; }
        public float AttackDamage { get; private set; }
        public float AttackInterval { get; private set; }
        public float PreferredRange { get; private set; }
        public float PoisonDps { get; private set; }
        public float PoisonDuration { get; private set; }
        public float DashTriggerRange { get; private set; }
        public float DashCooldown { get; private set; }
        public float DashDuration { get; private set; }
        public float DashMultiplier { get; private set; } = 1f;
        public float KnockbackForce { get; private set; }
        public float DetectionRange { get; private set; } = 30f;
        public float AttackCooldown { get; private set; }
        public float DashCooldownTimer { get; private set; }
        public float DashTimeRemaining { get; private set; }
        public float VerticalVelocity { get; private set; }
        public float CurrentDistanceToTarget { get; private set; } = float.MaxValue;
        public Vector3 CurrentMoveDirection { get; private set; } = Vector3.forward;
        public bool MovedThisFrame { get; private set; }
        public float Gravity { get; private set; } = -24f;

        public void InitializeRuntime(
            float maxHp,
            float moveSpeed,
            float attackRange,
            float attackDamage,
            float attackInterval,
            string aiType,
            float preferredRange,
            float poisonDps,
            float poisonDuration,
            float dashTriggerRange,
            float dashCooldown,
            float dashDuration,
            float dashMultiplier,
            float knockbackForce,
            float gravity = -24f,
            float detectionRange = 30f)
        {
            InitializeStats(maxHp, moveSpeed);
            TickFromMonoUpdate = false;
            CacheComponents();
            EnsureAnimationDriver();
            EnsureStates();

            AttackRange = attackRange;
            AttackDamage = attackDamage;
            AttackInterval = attackInterval;
            AiType = aiType;
            PreferredRange = preferredRange;
            PoisonDps = poisonDps;
            PoisonDuration = poisonDuration;
            DashTriggerRange = dashTriggerRange;
            DashCooldown = dashCooldown;
            DashDuration = dashDuration;
            DashMultiplier = Mathf.Max(1f, dashMultiplier);
            KnockbackForce = knockbackForce;
            Gravity = gravity;
            DetectionRange = detectionRange;
            AttackCooldown = Random.Range(0.1f, 0.55f);
            DashCooldownTimer = Random.Range(0f, Mathf.Max(0.01f, dashCooldown));
            ChangeState(_idleState);
        }

        public void SetTarget(Transform target)
        {
            Target = target;
        }

        public bool HasTarget()
        {
            return Target != null;
        }

        public float DistanceToTarget()
        {
            if (Target == null)
            {
                return float.MaxValue;
            }

            return Vector3.Distance(transform.position, Target.position);
        }

        public bool IsTargetInAttackRange()
        {
            return HasTarget() && DistanceToTarget() <= AttackRange;
        }

        public bool CanAttack(float time)
        {
            return HasTarget() && time >= AttackCooldown;
        }

        public void FaceTarget()
        {
            if (Target == null)
            {
                return;
            }

            FaceDirection(Target.position - transform.position);
        }

        public void TickBehaviour(float deltaTime)
        {
            if (IsDead)
            {
                _animationDriver?.Tick(deltaTime);
                StateMachine.Update(deltaTime);
                return;
            }

            AttackCooldown -= deltaTime;
            DashCooldownTimer -= deltaTime;
            DashTimeRemaining = Mathf.Max(0f, DashTimeRemaining - deltaTime);

            var direction = Target != null ? Target.position - transform.position : Vector3.zero;
            direction.y = 0f;
            CurrentDistanceToTarget = direction.magnitude;
            CurrentMoveDirection = CurrentDistanceToTarget > 0.05f ? direction.normalized : transform.forward;

            var targetRange = PreferredRange > 0f ? PreferredRange : AttackRange * 0.9f;
            var moveSpeed = MoveSpeed;
            MovedThisFrame = false;

            if (DashCooldown > 0f &&
                DashTriggerRange > 0f &&
                CurrentDistanceToTarget <= DashTriggerRange &&
                DashCooldownTimer <= 0f)
            {
                DashCooldownTimer = DashCooldown;
                DashTimeRemaining = Mathf.Max(0.1f, DashDuration);
            }

            if (DashTimeRemaining > 0f)
            {
                moveSpeed *= DashMultiplier;
            }

            if (AiType == "Ranged")
            {
                if (CurrentDistanceToTarget > targetRange + 0.4f)
                {
                    MoveHorizontal(CurrentMoveDirection * moveSpeed * deltaTime, deltaTime);
                    MovedThisFrame = true;
                }
                else if (CurrentDistanceToTarget < targetRange - 1.2f)
                {
                    MoveHorizontal(-CurrentMoveDirection * moveSpeed * 0.6f * deltaTime, deltaTime);
                    MovedThisFrame = true;
                }
            }
            else if (CurrentDistanceToTarget > targetRange && CurrentDistanceToTarget > 0.05f)
            {
                MoveHorizontal(CurrentMoveDirection * moveSpeed * deltaTime, deltaTime);
                MovedThisFrame = true;
            }

            if (CurrentMoveDirection.sqrMagnitude > 0.01f)
            {
                FaceDirection(CurrentMoveDirection);
            }

            if (CurrentDistanceToTarget <= AttackRange && AttackCooldown <= 0f)
            {
                ChangeState(_attackState);
            }
            else if (MovedThisFrame)
            {
                ChangeState(_chaseState);
            }
            else
            {
                ChangeState(_idleState);
            }

            StateMachine.Update(deltaTime);
            _animationDriver?.Tick(deltaTime);
        }

        public bool TryAttack()
        {
            if (IsDead || !HasTarget() || CurrentDistanceToTarget > AttackRange || AttackCooldown > 0f)
            {
                return false;
            }

            AttackCooldown = Mathf.Max(0.1f, AttackInterval);
            ChangeState(_attackState);
            return true;
        }

        public void NotifyHit()
        {
            if (IsDead)
            {
                return;
            }

            ChangeState(_hitState);
        }

        public override void OnDie()
        {
            base.OnDie();
            EnsureStates();
            if (!StateMachine.IsCurrent<EnemyDeadState>())
            {
                ChangeState(_deadState);
            }
        }

        public string GetAttackAnimationName()
        {
            if (AiType == "Tank")
            {
                return "Headbutt";
            }

            if (PoisonDps > 0f)
            {
                return "Scream";
            }

            return Random.value > 0.5f ? "Slap Left" : "Biting";
        }

        public string GetLocomotionAnimationName()
        {
            if (DashTimeRemaining > 0f || AiType == "Runner")
            {
                return "Run";
            }

            if (AiType == "Ranged")
            {
                return "Walk Back";
            }

            return AiType == "Tank" ? "Walk" : "Walk Aggro";
        }

        public void PlayIdleStateAnimation()
        {
            PlayAnimation("Idle");
        }

        public void PlayMoveStateAnimation()
        {
            var locomotion = GetLocomotionAnimationName();
            if (!string.IsNullOrWhiteSpace(locomotion))
            {
                PlayAnimation(locomotion);
            }
        }

        public void PlayAttackStateAnimation()
        {
            var attack = GetAttackAnimationName();
            if (!string.IsNullOrWhiteSpace(attack))
            {
                PlayAnimation(attack);
            }
        }

        public void PlayHitStateAnimation()
        {
            PlayAnimation("Damage");
        }

        public void PlayDeathStateAnimation()
        {
            PlayAnimation("Death");
        }

        public override void CacheComponents()
        {
            base.CacheComponents();
            EnsureAnimationDriver();
        }

        public override void PlayAnimation(string stateName, float fadeDuration = 0.08f)
        {
            if (_animationDriver != null && _animationDriver.PlayState(stateName))
            {
                return;
            }

            base.PlayAnimation(stateName, fadeDuration);
        }

        private void OnDestroy()
        {
            _animationDriver?.Dispose();
        }

        private void EnsureStates()
        {
            _idleState ??= new EnemyIdleState(this);
            _chaseState ??= new EnemyChaseState(this);
            _attackState ??= new EnemyAttackState(this);
            _hitState ??= new EnemyHitState(this);
            _deadState ??= new EnemyDeadState(this);
        }

        private void EnsureAnimationDriver()
        {
            if (_animationDriver != null || Anim == null)
            {
                return;
            }

            _animationDriver = new EnemyAnimationDriver(Anim);
            _animationDriver.ConfigureVariant(ResolveAnimationVariant());
        }

        private string ResolveAnimationVariant()
        {
            var controllerName = Anim != null ? Anim.runtimeAnimatorController?.name : string.Empty;
            if (!string.IsNullOrWhiteSpace(controllerName) &&
                controllerName.IndexOf("Girl", System.StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return "Zombie Girl";
            }

            var nameToCheck = Anim != null ? Anim.gameObject.name : gameObject.name;
            if (!string.IsNullOrWhiteSpace(nameToCheck) &&
                nameToCheck.IndexOf("Girl", System.StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return "Zombie Girl";
            }

            return "Zombie Guy";
        }

        private void MoveHorizontal(Vector3 horizontalDisplacement, float deltaTime)
        {
            if (Controller == null)
            {
                transform.position += horizontalDisplacement;
                return;
            }

            if (Controller.isGrounded && VerticalVelocity < 0f)
            {
                VerticalVelocity = -1f;
            }
            else
            {
                VerticalVelocity += Gravity * deltaTime;
            }

            var motion = horizontalDisplacement;
            motion.y = VerticalVelocity * deltaTime;
            Move(motion);
        }
    }
}
