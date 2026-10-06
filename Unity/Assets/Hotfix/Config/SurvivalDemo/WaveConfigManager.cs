using System.Collections.Generic;
using System.Linq;

namespace GameframeX.Config
{
    public class WaveSpawnEntryConfig
    {
        public int Id { get; set; }
        public int MonsterId { get; set; }
        public int SpawnCount { get; set; }
        public float StartTime { get; set; }
        public float SpawnInterval { get; set; }
        public int SpawnWeight { get; set; }
    }

    public class WaveConfig
    {
        public int WaveIndex { get; set; }
        public string WaveName { get; set; }
        public float PrepareTime { get; set; }
        public string Description { get; set; }
        public List<WaveSpawnEntryConfig> SpawnEntries { get; set; } = new List<WaveSpawnEntryConfig>();
        public int TotalCount => SpawnEntries?.Sum(entry => entry.SpawnCount) ?? 0;
    }

    public static class WaveConfigManager
    {
        private static readonly Dictionary<int, WaveConfig> Waves = new Dictionary<int, WaveConfig>();

        static WaveConfigManager()
        {
            AddWave(1, "初露端倪", 8f, "普通丧尸热身波，先建立割草节奏。",
                new WaveSpawnEntryConfig { Id = 400101, MonsterId = 2001, SpawnCount = 12, StartTime = 0f, SpawnInterval = 1.9f, SpawnWeight = 100 });

            AddWave(2, "小规模试探", 10f, "普通丧尸中混入疾行变种，逼玩家开始走位。",
                new WaveSpawnEntryConfig { Id = 400201, MonsterId = 2001, SpawnCount = 16, StartTime = 0f, SpawnInterval = 0.9f, SpawnWeight = 100 },
                new WaveSpawnEntryConfig { Id = 400202, MonsterId = 2003, SpawnCount = 8, StartTime = 4f, SpawnInterval = 1.15f, SpawnWeight = 45 });

            AddWave(3, "精英尖兵", 12f, "毒疫丧尸登场，场面开始出现远程持续压血。",
                new WaveSpawnEntryConfig { Id = 400301, MonsterId = 2001, SpawnCount = 12, StartTime = 0f, SpawnInterval = 0.95f, SpawnWeight = 100 },
                new WaveSpawnEntryConfig { Id = 400302, MonsterId = 2002, SpawnCount = 8, StartTime = 3f, SpawnInterval = 1.4f, SpawnWeight = 35 });

            AddWave(4, "午夜狂欢", 14f, "三种基础尸潮混编，兼顾贴脸和中距离消耗。",
                new WaveSpawnEntryConfig { Id = 400401, MonsterId = 2001, SpawnCount = 22, StartTime = 0f, SpawnInterval = 0.55f, SpawnWeight = 100 },
                new WaveSpawnEntryConfig { Id = 400402, MonsterId = 2003, SpawnCount = 12, StartTime = 4f, SpawnInterval = 0.7f, SpawnWeight = 45 },
                new WaveSpawnEntryConfig { Id = 400403, MonsterId = 2002, SpawnCount = 8, StartTime = 8f, SpawnInterval = 1f, SpawnWeight = 35 });

            AddWave(5, "混合编队", 16f, "暴君压阵，小怪负责填满战场。",
                new WaveSpawnEntryConfig { Id = 400501, MonsterId = 2001, SpawnCount = 18, StartTime = 0f, SpawnInterval = 0.72f, SpawnWeight = 100 },
                new WaveSpawnEntryConfig { Id = 400502, MonsterId = 2003, SpawnCount = 10, StartTime = 2f, SpawnInterval = 0.9f, SpawnWeight = 45 },
                new WaveSpawnEntryConfig { Id = 400503, MonsterId = 2002, SpawnCount = 6, StartTime = 6f, SpawnInterval = 1.15f, SpawnWeight = 35 },
                new WaveSpawnEntryConfig { Id = 400504, MonsterId = 2004, SpawnCount = 2, StartTime = 10f, SpawnInterval = 5f, SpawnWeight = 8 });

            AddWave(6, "最后的绝望", 999f, "多只疾行变种护卫暴君，形成最终压制。",
                new WaveSpawnEntryConfig { Id = 400601, MonsterId = 2003, SpawnCount = 5, StartTime = 0f, SpawnInterval = 1.2f, SpawnWeight = 45 },
                new WaveSpawnEntryConfig { Id = 400602, MonsterId = 2004, SpawnCount = 3, StartTime = 2.5f, SpawnInterval = 4.8f, SpawnWeight = 8 });
        }

        private static void AddWave(int waveIndex, string waveName, float prepareTime, string description, params WaveSpawnEntryConfig[] entries)
        {
            Waves[waveIndex] = new WaveConfig
            {
                WaveIndex = waveIndex,
                WaveName = waveName,
                PrepareTime = prepareTime,
                Description = description,
                SpawnEntries = entries.ToList()
            };
        }

        public static WaveConfig GetWave(int index) => Waves.TryGetValue(index, out var wave) ? wave : null;
        public static List<WaveConfig> GetAllWaves() => Waves.Values.OrderBy(wave => wave.WaveIndex).ToList();
    }
}
