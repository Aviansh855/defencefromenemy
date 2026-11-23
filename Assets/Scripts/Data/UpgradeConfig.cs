using UnityEngine;

namespace MobRush
{
    public static class UpgradeConfig
    {
        public const int MaxLevel = 10;

        // Upgrade Costs (Levels 0 -> 10)
        private static readonly int[] Costs = { 50, 100, 180, 280, 420, 600, 850, 1200, 1700, 2500 };

        public static int GetUpgradeCost(int currentLevel)
        {
            if (currentLevel >= MaxLevel) return -1;
            int index = Mathf.Clamp(currentLevel, 0, Costs.Length - 1);
            return Costs[index];
        }

        // Fire rate in shots/second (Level 0: 5/s, Level 10: 11.5/s)
        public static float GetFireRate(int level)
        {
            return 5.0f + Mathf.Max(0, level) * 0.65f;
        }

        // Unit Combat Strength / Base Damage (Level 0: 1, Level 10: 6)
        public static int GetUnitStrength(int level)
        {
            return 1 + Mathf.Max(0, level) / 2;
        }

        // Starting crowd burst count (Level 0: 5, Level 10: 35)
        public static int GetStartingPush(int level)
        {
            return 5 + Mathf.Max(0, level) * 3;
        }

        // Backward compatibility
        public static int GetStartingCrowd(int level) => GetStartingPush(level);

        // Base Brick Costs: level 1 costs 5, then each next base level costs previous + 5
        public static int GetBaseUpgradeCost(int currentBaseLevel)
        {
            return (Mathf.Max(0, currentBaseLevel) + 1) * 5;
        }

        // Daily Reward amounts, in order, then repeat day 7: 50, 75, 100, 125, 150, 200, 300 coins
        public static readonly int[] DailyRewards = { 50, 75, 100, 125, 150, 200, 300 };

        public static int GetDailyReward(int streakIndex)
        {
            if (streakIndex < 0) streakIndex = 0;
            int index = Mathf.Min(streakIndex, DailyRewards.Length - 1);
            return DailyRewards[index];
        }
    }
}
