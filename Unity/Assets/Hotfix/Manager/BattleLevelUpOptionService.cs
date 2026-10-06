using System;
using System.Collections.Generic;
using System.Linq;
using GameFrameX.Runtime;
using Hotfix.Config.Tables.survival;

namespace Hotfix.Manager
{
    internal sealed class BattleLevelUpOptionService
    {
        private static readonly HashSet<int> SupportedGroupIds = new HashSet<int>
        {
            1, 4, 5, 6, 7, 8, 17, 18, 19, 20
        };

        private readonly System.Random _random;
        private readonly Dictionary<int, int> _groupPickCounts = new Dictionary<int, int>();
        private readonly HashSet<int> _selectedOptionIds = new HashSet<int>();
        private readonly HashSet<int> _awakenedGroupIds = new HashSet<int>();

        public int CurrentLevel { get; private set; } = 1;
        public float CurrentExp { get; private set; }
        public float NextLevelExp => CalculateNextLevelExp(CurrentLevel);

        public BattleLevelUpOptionService(int? randomSeed = null)
        {
            _random = randomSeed.HasValue ? new System.Random(randomSeed.Value) : new System.Random();
        }

        public void Reset()
        {
            CurrentLevel = 1;
            CurrentExp = 0f;
            _groupPickCounts.Clear();
            _selectedOptionIds.Clear();
            _awakenedGroupIds.Clear();
        }

        public int AddExperience(float exp)
        {
            if (exp <= 0f)
            {
                return 0;
            }

            CurrentExp += exp;
            var levelUpCount = 0;
            while (CurrentExp >= NextLevelExp)
            {
                CurrentExp -= NextLevelExp;
                CurrentLevel++;
                levelUpCount++;
            }

            return levelUpCount;
        }

        public List<OptionConfig> RollOptions(int count)
        {
            var table = GameApp.Config.GetConfig<TbOptionConfig>();
            if (table == null || table.Count <= 0)
            {
                Log.Warning($"[LevelUpOption] TbOptionConfig invalid. tableNull={table == null} count={table?.Count ?? 0}");
                return new List<OptionConfig>();
            }

            var requestedCount = Math.Max(1, count);
            var allOptions = table.ToList();
            var supportedOptions = allOptions.Where(option => option != null && IsSupportedGroup(option.OptionGroupId)).ToList();
            var selectedBlocked = supportedOptions.Count(option => _selectedOptionIds.Contains(option.OptionId));
            var awakenedBlocked = supportedOptions.Count(option => _awakenedGroupIds.Contains(option.OptionGroupId));
            var awakenLocked = supportedOptions.Count(option =>
            {
                if (!option.IsAwaken)
                {
                    return false;
                }

                _groupPickCounts.TryGetValue(option.OptionGroupId, out var pickCount);
                return pickCount < option.AwakenNeedCount;
            });

            var candidates = PickDistinctGroups(allOptions.Where(IsOptionAvailable), requestedCount);

            Log.Info($"[LevelUpOption] roll summary total={allOptions.Count} supported={supportedOptions.Count} selectedBlocked={selectedBlocked} awakenedBlocked={awakenedBlocked} awakenLocked={awakenLocked} candidates={candidates.Count} requested={requestedCount}");

            if (candidates.Count >= requestedCount)
            {
                return candidates;
            }

            var usedGroupIds = new HashSet<int>(candidates.Select(option => option.OptionGroupId));
            var fallbackCandidates = PickDistinctGroups(
                allOptions.Where(option => IsFallbackOptionAvailable(option) && !usedGroupIds.Contains(option.OptionGroupId)),
                requestedCount - candidates.Count);

            foreach (var option in fallbackCandidates)
            {
                if (candidates.Any(item => item.OptionId == option.OptionId))
                {
                    continue;
                }

                candidates.Add(option);
                if (candidates.Count >= requestedCount)
                {
                    break;
                }
            }

            Log.Warning($"[LevelUpOption] candidate count less than requested. requested={requestedCount} actual={candidates.Count} fallbackPool={fallbackCandidates.Count}");
            return candidates;
        }

        private List<OptionConfig> PickDistinctGroups(IEnumerable<OptionConfig> options, int count)
        {
            if (count <= 0)
            {
                return new List<OptionConfig>();
            }

            return options
                .GroupBy(option => option.OptionGroupId)
                .OrderBy(_ => _random.Next())
                .Take(count)
                .Select(group => group.OrderBy(_ => _random.Next()).First())
                .ToList();
        }

        public void RecordSelection(OptionConfig option)
        {
            if (option == null)
            {
                return;
            }

            _selectedOptionIds.Add(option.OptionId);
            _groupPickCounts.TryGetValue(option.OptionGroupId, out var currentCount);
            _groupPickCounts[option.OptionGroupId] = currentCount + 1;

            if (option.IsAwaken)
            {
                _awakenedGroupIds.Add(option.OptionGroupId);
            }
        }

        private bool IsOptionAvailable(OptionConfig option)
        {
            if (option == null || !IsSupportedGroup(option.OptionGroupId))
            {
                return false;
            }

            if (_selectedOptionIds.Contains(option.OptionId))
            {
                return false;
            }

            if (_awakenedGroupIds.Contains(option.OptionGroupId))
            {
                return false;
            }

            _groupPickCounts.TryGetValue(option.OptionGroupId, out var pickCount);
            if (option.IsAwaken)
            {
                return pickCount >= option.AwakenNeedCount;
            }

            return true;
        }

        private bool IsFallbackOptionAvailable(OptionConfig option)
        {
            if (option == null || !IsSupportedGroup(option.OptionGroupId))
            {
                return false;
            }

            if (_awakenedGroupIds.Contains(option.OptionGroupId))
            {
                return false;
            }

            return !option.IsAwaken;
        }

        private static bool IsSupportedGroup(int optionGroupId)
        {
            return SupportedGroupIds.Contains(optionGroupId);
        }

        private static float CalculateNextLevelExp(int currentLevel)
        {
            return 100f + Math.Max(0, currentLevel - 1) * 35f;
        }
    }
}
