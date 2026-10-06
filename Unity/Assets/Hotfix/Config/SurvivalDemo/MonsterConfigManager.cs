using System.Collections.Generic;
using System.Linq;

namespace GameframeX.Config
{
    /// <summary>
    /// 怪物配置数据结构
    /// </summary>
    public class MonsterConfig
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public int SkillConfigId { get; set; }
        public float MaxHp { get; set; }          // 最大生命值
        public float MoveSpeed { get; set; }      // 移动速度
        public float AttackRange { get; set; }    // 攻击距离 (近战0.5~2，远程10+)
        public float AttackDamage { get; set; }   // 攻击伤害
        public float AttackInterval { get; set; } // 攻击频率 (秒/次)
        public float ExpAward { get; set; }       // 击杀经验奖励
        public string AiType { get; set; }        // AI类型：Simple(无脑冲), Ranged(风筝), Tank(慢速追)
        public string PrefabName { get; set; }    // 对应的预制体名称
        public string PrefabPath { get; set; }    // 对应的工程路径（编辑器兜底）
        public float BodyScale { get; set; }      // 外观缩放
        public float ColliderRadius { get; set; } // 碰撞半径
        public float ColliderHeight { get; set; } // 碰撞高度
        public string Notes { get; set; }         // 设计备注
    }

    /// <summary>
    /// 怪物数据硬编码库
    /// </summary>
    public static class MonsterConfigManager
    {
        private static readonly Dictionary<int, MonsterConfig> _configs = new Dictionary<int, MonsterConfig>();

        static MonsterConfigManager()
        {
            InitConfigs();
        }

        private static void InitConfigs()
        {
            Add(new MonsterConfig {
                Id = 2001, Name = "普通丧尸", MaxHp = 42f, MoveSpeed = 2.35f,
                SkillConfigId = 3001,
                AttackRange = 1.3f, AttackDamage = 6f, AttackInterval = 1.25f,
                ExpAward = 10f, AiType = "Simple", PrefabName = "Zombie Guy 1A",
                PrefabPath = "Assets/Zombie Slaughter/Prefabs/Zombie Guy 1A/Zombie Guy 1A.prefab",
                BodyScale = 0.95f, ColliderRadius = 0.45f, ColliderHeight = 1.8f,
                Notes = "低速近战炮灰，主打数量。"
            });

            Add(new MonsterConfig {
                Id = 2002, Name = "毒疫丧尸", MaxHp = 68f, MoveSpeed = 2.15f,
                SkillConfigId = 3002,
                AttackRange = 11f, AttackDamage = 4f, AttackInterval = 2.8f,
                ExpAward = 18f, AiType = "Ranged", PrefabName = "Zombie Girl 2A",
                PrefabPath = "Assets/Zombie Slaughter/Prefabs/Zombie Girl 2A/Zombie Girl 2A.prefab",
                BodyScale = 0.96f, ColliderRadius = 0.45f, ColliderHeight = 1.8f,
                Notes = "远程喷毒，带持续掉血。"
            });

            Add(new MonsterConfig {
                Id = 2003, Name = "疾行变种", MaxHp = 24f, MoveSpeed = 4.6f,
                SkillConfigId = 3003,
                AttackRange = 1.1f, AttackDamage = 7f, AttackInterval = 0.8f,
                ExpAward = 15f, AiType = "Runner", PrefabName = "Zombie Guy 1B",
                PrefabPath = "Assets/Zombie Slaughter/Prefabs/Zombie Guy 1B/Zombie Guy 1B.prefab",
                BodyScale = 0.9f, ColliderRadius = 0.42f, ColliderHeight = 1.72f,
                Notes = "高移速突袭型，容易被范围伤害清掉。"
            });

            Add(new MonsterConfig {
                Id = 2004, Name = "暴君级巨型怪", MaxHp = 420f, MoveSpeed = 1.55f,
                SkillConfigId = 3004,
                AttackRange = 2.8f, AttackDamage = 18f, AttackInterval = 2.2f,
                ExpAward = 80f, AiType = "Tank", PrefabName = "Zombie Guy 3A",
                PrefabPath = "Assets/Zombie Slaughter/Prefabs/Zombie Guy 3A/Zombie Guy 3A.prefab",
                BodyScale = 1.45f, ColliderRadius = 0.7f, ColliderHeight = 2.7f,
                Notes = "高血高伤小Boss前排，近战命中带击退。"
            });
        }

        private static void Add(MonsterConfig config)
        {
            _configs[config.Id] = config;
        }

        public static MonsterConfig GetConfig(int id) => _configs.TryGetValue(id, out var c) ? c : null;
        public static List<MonsterConfig> GetAllConfigs() => _configs.Values.ToList();
    }
}
