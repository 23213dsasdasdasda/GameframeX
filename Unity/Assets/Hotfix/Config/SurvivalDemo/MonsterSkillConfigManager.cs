using System.Collections.Generic;
using System.Linq;

namespace GameframeX.Config
{
    public class MonsterSkillConfig
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string AiType { get; set; }
        public float PreferredRange { get; set; }
        public float PoisonDps { get; set; }
        public float PoisonDuration { get; set; }
        public float DashTriggerRange { get; set; }
        public float DashCooldown { get; set; }
        public float DashDuration { get; set; }
        public float DashMultiplier { get; set; }
        public float KnockbackForce { get; set; }
        public string Notes { get; set; }
    }

    public static class MonsterSkillConfigManager
    {
        private static readonly Dictionary<int, MonsterSkillConfig> Configs = new Dictionary<int, MonsterSkillConfig>();

        static MonsterSkillConfigManager()
        {
            Add(new MonsterSkillConfig
            {
                Id = 3001,
                Name = "普通扑击",
                AiType = "Simple",
                PreferredRange = 0f,
                PoisonDps = 0f,
                PoisonDuration = 0f,
                DashTriggerRange = 0f,
                DashCooldown = 0f,
                DashDuration = 0f,
                DashMultiplier = 1f,
                KnockbackForce = 0f,
                Notes = "基础近战追击。"
            });

            Add(new MonsterSkillConfig
            {
                Id = 3002,
                Name = "毒疫喷吐",
                AiType = "Ranged",
                PreferredRange = 8f,
                PoisonDps = 3f,
                PoisonDuration = 4f,
                DashTriggerRange = 0f,
                DashCooldown = 0f,
                DashDuration = 0f,
                DashMultiplier = 1f,
                KnockbackForce = 0f,
                Notes = "中距离持续压血。"
            });

            Add(new MonsterSkillConfig
            {
                Id = 3003,
                Name = "疾行突袭",
                AiType = "Runner",
                PreferredRange = 0f,
                PoisonDps = 0f,
                PoisonDuration = 0f,
                DashTriggerRange = 8f,
                DashCooldown = 3.6f,
                DashDuration = 0.35f,
                DashMultiplier = 2.2f,
                KnockbackForce = 0f,
                Notes = "短时爆发接近目标。"
            });

            Add(new MonsterSkillConfig
            {
                Id = 3004,
                Name = "暴君重击",
                AiType = "Tank",
                PreferredRange = 0f,
                PoisonDps = 0f,
                PoisonDuration = 0f,
                DashTriggerRange = 0f,
                DashCooldown = 0f,
                DashDuration = 0f,
                DashMultiplier = 1f,
                KnockbackForce = 8f,
                Notes = "近战命中附带击退。"
            });
        }

        private static void Add(MonsterSkillConfig config)
        {
            Configs[config.Id] = config;
        }

        public static MonsterSkillConfig GetConfig(int id) => Configs.TryGetValue(id, out var config) ? config : null;
        public static List<MonsterSkillConfig> GetAllConfigs() => Configs.Values.ToList();
    }
}
