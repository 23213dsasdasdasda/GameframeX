using System.Collections.Generic;
using System.Linq;

using Hotfix.Config;

namespace GameframeX.Config
{
    /// <summary>
    /// 武器配置数据结构
    /// </summary>
    public class WeaponConfig
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public EWeaponType Type { get; set; }
        public float Damage { get; set; }         // 伤害
        public float BulletSpeed { get; set; }    // 子弹飞行速度
        public float FireRate { get; set; }       // 射速 (发/秒)
        public float FireInterval { get; set; }   // 攻击间隔 (秒)
        public float LifeTime { get; set; }       // 子弹/判定存活时间 (秒)
        public bool DestroyOnHit { get; set; }    // 命中后是否消失 (穿透属性)
        public string Description { get; set; }    // 描述
        public int BulletVisualId { get; set; }    // 弹道表现ID
        
        // 扩展属性
        public int BulletCount { get; set; }      // 每次发射子弹数 (霰弹枪)
        public float ExplosionRadius { get; set; } // 爆炸半径 (榴弹/火箭)
    }

    /// <summary>
    /// 武器库硬编码配置
    /// </summary>
    public static class WeaponConfigManager
    {
        private static readonly Dictionary<int, WeaponConfig> _configs = new Dictionary<int, WeaponConfig>();

        static WeaponConfigManager()
        {
            // 在静态构造函数里直接塞数据
            InitConfigs();
        }

        private static void InitConfigs()
        {
            // 1. 初始手枪
            Add(new WeaponConfig { Id = 101, Name = "初始手枪", Type = EWeaponType.Normal, Damage = 10f, BulletSpeed = 30f, FireRate = 2f, FireInterval = 0.5f, LifeTime = 2f, DestroyOnHit = true, BulletCount = 1, Description = "标准单发" });

            // 2. 霰弹枪
            Add(new WeaponConfig { Id = 102, Name = "霰弹枪", Type = EWeaponType.Shotgun, Damage = 8f, BulletSpeed = 26f, FireRate = 1f, FireInterval = 1f, LifeTime = 0.5f, DestroyOnHit = true, BulletCount = 8, Description = "扇形扩散" });

            // 3. 喷火器
            Add(new WeaponConfig { Id = 103, Name = "喷火器", Type = EWeaponType.Laser, Damage = 2f, BulletSpeed = 18f, FireRate = 20f, FireInterval = 0.05f, LifeTime = 0.8f, DestroyOnHit = false, BulletCount = 1, Description = "高频穿透" });

            // 4. 激光枪
            Add(new WeaponConfig { Id = 104, Name = "激光枪", Type = EWeaponType.Laser, Damage = 5f, BulletSpeed = 80f, FireRate = 60f, FireInterval = 0.01f, LifeTime = 0.1f, DestroyOnHit = false, BulletCount = 1, Description = "瞬间命中" });

            // 5. 榴弹炮
            Add(new WeaponConfig { Id = 105, Name = "榴弹炮", Type = EWeaponType.Grenade, Damage = 40f, BulletSpeed = 14f, FireRate = 0.5f, FireInterval = 2f, LifeTime = 5f, DestroyOnHit = true, BulletCount = 1, ExplosionRadius = 3f, Description = "范围爆炸" });

            // 6. 火箭炮
            Add(new WeaponConfig { Id = 106, Name = "火箭炮", Type = EWeaponType.Grenade, Damage = 100f, BulletSpeed = 16f, FireRate = 0.3f, FireInterval = 3f, LifeTime = 10f, DestroyOnHit = true, BulletCount = 1, ExplosionRadius = 5f, Description = "高伤巨幅爆炸" });
        }

        private static void Add(WeaponConfig config)
        {
            _configs[config.Id] = config;
        }

        /// <summary>
        /// 获取单条配置
        /// </summary>
        public static WeaponConfig GetConfig(int id)
        {
            return _configs.TryGetValue(id, out var config) ? config : null;
        }

        /// <summary>
        /// 获取所有配置列表
        /// </summary>
        public static List<WeaponConfig> GetAllConfigs()
        {
            return _configs.Values.ToList();
        }
    }
}
