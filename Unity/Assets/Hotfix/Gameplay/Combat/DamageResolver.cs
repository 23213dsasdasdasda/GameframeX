using UnityEngine;

namespace Hotfix.Gameplay.Combat
{
    public static class DamageResolver
    {
        public static DamageResult Resolve(in DamageRequest request)
        {
            var target = request.Target;
            if (target == null || target.IsDead)
            {
                return default;
            }

            var finalDamage = request.BaseDamage > 0f ? request.BaseDamage : 0f;
            var remainingHp = Mathf.Max(0f, target.CurrentHp - finalDamage);
            target.SetCurrentHp(remainingHp);

            var poisonDps = request.PoisonDps > 0f ? request.PoisonDps : 0f;
            var poisonDuration = request.PoisonDuration > 0f ? request.PoisonDuration : 0f;
            var appliedPoison = poisonDps > 0f && poisonDuration > 0f;
            var knockbackForce = request.KnockbackForce > 0f ? request.KnockbackForce : 0f;
            var hasKnockback = knockbackForce > 0f && request.KnockbackDirection.sqrMagnitude > 0.0001f;
            var knockbackDirection = hasKnockback ? request.KnockbackDirection.normalized : Vector3.zero;

            return new DamageResult(
                finalDamage,
                remainingHp,
                remainingHp <= 0f,
                appliedPoison,
                poisonDps,
                poisonDuration,
                hasKnockback,
                knockbackForce,
                knockbackDirection);
        }
    }
}
