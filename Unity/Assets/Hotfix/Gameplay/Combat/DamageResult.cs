using UnityEngine;

namespace Hotfix.Gameplay.Combat
{
    public readonly struct DamageResult
    {
        public float FinalDamage { get; }
        public float RemainingHp { get; }
        public bool IsDead { get; }
        public bool AppliedPoison { get; }
        public float PoisonDps { get; }
        public float PoisonDuration { get; }
        public bool HasKnockback { get; }
        public float KnockbackForce { get; }
        public Vector3 KnockbackDirection { get; }

        public DamageResult(
            float finalDamage,
            float remainingHp,
            bool isDead,
            bool appliedPoison,
            float poisonDps,
            float poisonDuration,
            bool hasKnockback,
            float knockbackForce,
            Vector3 knockbackDirection)
        {
            FinalDamage = finalDamage;
            RemainingHp = remainingHp;
            IsDead = isDead;
            AppliedPoison = appliedPoison;
            PoisonDps = poisonDps;
            PoisonDuration = poisonDuration;
            HasKnockback = hasKnockback;
            KnockbackForce = knockbackForce;
            KnockbackDirection = knockbackDirection;
        }
    }
}
