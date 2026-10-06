namespace Hotfix.Gameplay.Combat
{
    public sealed class CombatEntityModel
    {
        public float MaxHp { get; private set; }
        public float CurrentHp { get; private set; }
        public bool IsDead => CurrentHp <= 0f;

        public CombatEntityModel(float maxHp)
        {
            Reset(maxHp);
        }

        public void Reset(float maxHp)
        {
            MaxHp = maxHp > 0f ? maxHp : 1f;
            CurrentHp = MaxHp;
        }

        public void SetCurrentHp(float hp)
        {
            CurrentHp = hp < 0f ? 0f : (hp > MaxHp ? MaxHp : hp);
        }
    }
}
