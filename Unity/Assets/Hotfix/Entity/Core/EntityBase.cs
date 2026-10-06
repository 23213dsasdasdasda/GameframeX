using Hotfix.Gameplay.Combat;
using UnityEngine;

namespace Hotfix.Gameplay.Core
{
    public abstract class EntityBase : MonoBehaviour
    {
        public FSM StateMachine { get; private set; } = new FSM();
        public bool TickFromMonoUpdate { get; set; } = true;
        public Animator Anim { get; protected set; }
        public CharacterController Controller { get; protected set; }
        public Collider[] CachedColliders { get; protected set; } = System.Array.Empty<Collider>();
        public AudioSource AudioSource { get; protected set; }
        public CombatEntityModel Combat { get; protected set; }

        public float Hp { get; protected set; }
        public float MaxHp { get; protected set; }
        public float MoveSpeed { get; protected set; }
        public bool IsDead { get; protected set; }

        protected virtual void Awake()
        {
            CacheComponents();
        }

        protected virtual void Update()
        {
            if (!TickFromMonoUpdate)
            {
                return;
            }

            Tick(Time.deltaTime);
        }

        public virtual void Tick(float deltaTime)
        {
            StateMachine?.Update(deltaTime);
        }

        public virtual void InitializeStats(float maxHp, float moveSpeed)
        {
            MaxHp = maxHp;
            Hp = maxHp;
            MoveSpeed = moveSpeed;
            IsDead = false;
            Combat = new CombatEntityModel(maxHp);
        }

        public virtual void CacheComponents()
        {
            Anim = GetComponentInChildren<Animator>(true);
            Controller = GetComponent<CharacterController>();
            AudioSource = GetComponent<AudioSource>();
            CachedColliders = GetComponentsInChildren<Collider>(true);
        }

        public virtual void ChangeState(IState nextState)
        {
            StateMachine.ChangeState(nextState);
        }

        public virtual void Move(Vector3 worldMotion)
        {
            if (Controller != null)
            {
                Controller.Move(worldMotion);
                return;
            }

            transform.position += worldMotion;
        }

        public virtual void FaceDirection(Vector3 direction)
        {
            direction.y = 0f;
            if (direction.sqrMagnitude <= 0.0001f)
            {
                return;
            }

            transform.rotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
        }

        public virtual void PlayAnimation(string stateName, float fadeDuration = 0.08f)
        {
            if (Anim == null || Anim.runtimeAnimatorController == null || string.IsNullOrWhiteSpace(stateName))
            {
                return;
            }

            if (!Anim.HasState(0, Animator.StringToHash(stateName)))
            {
                return;
            }

            Anim.CrossFadeInFixedTime(stateName, fadeDuration);
        }

        public virtual void TakeDamage(float damage)
        {
            if (IsDead || damage <= 0f)
            {
                return;
            }

            var result = DamageResolver.Resolve(new DamageRequest(null, Combat, damage));
            ApplyDamageResult(result);
        }

        public virtual void ApplyDamageResult(in DamageResult result)
        {
            if (Combat != null)
            {
                Combat.SetCurrentHp(result.RemainingHp);
            }

            Hp = result.RemainingHp;
            MaxHp = Combat?.MaxHp ?? MaxHp;
            if (result.IsDead)
            {
                OnDie();
            }
        }

        public virtual void OnDie()
        {
            if (IsDead)
            {
                return;
            }

            IsDead = true;
            SetCollisionEnabled(false);
        }

        public virtual void SetCollisionEnabled(bool enabled)
        {
            if (CachedColliders == null)
            {
                return;
            }

            foreach (var cachedCollider in CachedColliders)
            {
                if (cachedCollider != null)
                {
                    cachedCollider.enabled = enabled;
                }
            }
        }
    }
}
