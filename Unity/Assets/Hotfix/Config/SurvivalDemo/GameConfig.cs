using System.Collections.Generic;
using System.Linq;

namespace GameframeX.Config
{
    public static class GameConfigBus
    {
        static GameConfigBus()
        {
            UILib.Init();
        }

        public static class WeaponLib
        {
            public static WeaponConfig Get(int id) => WeaponConfigManager.GetConfig(id);
            public static List<WeaponConfig> GetAll() => WeaponConfigManager.GetAllConfigs();
        }

        public static class MonsterLib
        {
            public static MonsterConfig Get(int id) => MonsterConfigManager.GetConfig(id);
            public static List<MonsterConfig> GetAll() => MonsterConfigManager.GetAllConfigs();
        }

        public static class MonsterSkillLib
        {
            public static MonsterSkillConfig Get(int id) => MonsterSkillConfigManager.GetConfig(id);
            public static List<MonsterSkillConfig> GetAll() => MonsterSkillConfigManager.GetAllConfigs();
        }

        public static class WaveLib
        {
            public static WaveConfig Get(int index) => WaveConfigManager.GetWave(index);
            public static List<WaveConfig> GetAll() => WaveConfigManager.GetAllWaves();
        }

        public static class UILib
        {
            public static List<HomeMenuConfig> Menus { get; } = new List<HomeMenuConfig>();
            public static List<SysNotifyConfig> Popups { get; } = new List<SysNotifyConfig>();
            public static List<CheckInConfig> CheckInDays { get; } = new List<CheckInConfig>();

            public static void Init()
            {
                if (Menus.Count > 0)
                {
                    return;
                }

                Menus.Add(new HomeMenuConfig { Id = 1, BtnName = "战斗", TargetPage = "Battle" });
                Menus.Add(new HomeMenuConfig { Id = 2, BtnName = "武器", TargetPage = "Weapon" });

                Popups.Add(new SysNotifyConfig { Id = 1, NotifyType = "FirstPay", Priority = 100, PrefabName = "UI_FirstPay" });
                Popups.Add(new SysNotifyConfig { Id = 2, NotifyType = "CheckIn", Priority = 90, PrefabName = "UI_CheckIn" });
                Popups.Sort((a, b) => b.Priority.CompareTo(a.Priority));

                for (var day = 1; day <= 7; day++)
                {
                    CheckInDays.Add(new CheckInConfig
                    {
                        Day = day,
                        ItemId = 1000 + day,
                        Count = day * 10
                    });
                }
            }
        }
    }

    public static class PlayerGlobalConfig
    {
        public static int CurrentStrength = 20;
    }

    public class HomeMenuConfig
    {
        public int Id { get; set; }
        public string BtnName { get; set; }
        public string TargetPage { get; set; }
    }

    public class SysNotifyConfig
    {
        public int Id { get; set; }
        public string NotifyType { get; set; }
        public int Priority { get; set; }
        public string PrefabName { get; set; }
    }

    public class CheckInConfig
    {
        public int Day { get; set; }
        public int ItemId { get; set; }
        public int Count { get; set; }
    }
}
