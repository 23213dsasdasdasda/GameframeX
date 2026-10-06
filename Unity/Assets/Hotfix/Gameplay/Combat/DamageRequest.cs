using UnityEngine;

namespace Hotfix.Gameplay.Combat
{
    public readonly struct DamageRequest
    {
        public CombatEntityModel Source { get; }
        public CombatEntityModel Target { get; }
        public float BaseDamage { get; }
        public float PoisonDps { get; }
        public float PoisonDuration { get; }
        public float KnockbackForce { get; }
        public Vector3 KnockbackDirection { get; }

        public DamageRequest(
            CombatEntityModel source,
            CombatEntityModel target,
            float baseDamage,
            float poisonDps = 0f,
            float poisonDuration = 0f,
            float knockbackForce = 0f,
            Vector3 knockbackDirection = default)
        {
            Source = source;
            Target = target;
            BaseDamage = baseDamage;
            PoisonDps = poisonDps;
            PoisonDuration = poisonDuration;
            KnockbackForce = knockbackForce;
            KnockbackDirection = knockbackDirection;
        }
    }
}
