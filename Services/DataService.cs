using System.Globalization;
using UnityEngine.InputSystem.Utilities;

namespace Eclipse.Services;

internal static class DataService
{
    public enum BloodType
    {
        Worker,
        Warrior,
        Scholar,
        Rogue,
        Mutant,
        VBlood,
        Frailed,
        GateBoss,
        Draculin,
        Immortal,
        Creature,
        Brute
    }
    public enum WeaponType
    {
        Sword,
        Axe,
        Mace,
        Spear,
        Crossbow,
        GreatSword,
        Slashers,
        Pistols,
        Reaper,
        Longbow,
        Whip,
        Unarmed,
        FishingPole
    }

    public static Dictionary<WeaponStatType, float> WeaponStatValues = [];
    public enum WeaponStatType
    {
        None, // 0 not shifting the rest right now :p
        MaxHealth, // 0
        MovementSpeed, // 1
        PrimaryAttackSpeed, // 2
        PhysicalLifeLeech, // 3
        SpellLifeLeech, // 4
        PrimaryLifeLeech, // 5
        PhysicalPower, // 6
        SpellPower, // 7
        PhysicalCritChance, // 8
        PhysicalCritDamage, // 9
        SpellCritChance, // 10
        SpellCritDamage // 11
    }

    public static readonly Dictionary<WeaponStatType, string> WeaponStatTypeAbbreviations = new()
    {
        { WeaponStatType.MaxHealth, "HP" },
        { WeaponStatType.MovementSpeed, "MS" },
        { WeaponStatType.PrimaryAttackSpeed, "PAS" },
        { WeaponStatType.PhysicalLifeLeech, "PLL" },
        { WeaponStatType.SpellLifeLeech, "SLL" },
        { WeaponStatType.PrimaryLifeLeech, "PLL" },
        { WeaponStatType.PhysicalPower, "PP" },
        { WeaponStatType.SpellPower, "SP" },
        { WeaponStatType.PhysicalCritChance, "PCC" },
        { WeaponStatType.PhysicalCritDamage, "PCD" },
        { WeaponStatType.SpellCritChance, "SCC" },
        { WeaponStatType.SpellCritDamage, "SCD" }
    };

    public static readonly Dictionary<string, string> WeaponStatStringAbbreviations = new()
    {
        { "MaxHealth", "HP" },
        { "MovementSpeed", "MS" },
        { "PrimaryAttackSpeed", "PAS" },
        { "PhysicalLifeLeech", "PLL" },
        { "SpellLifeLeech", "SLL" },
        { "PrimaryLifeLeech", "PLL" },
        { "PhysicalPower", "PP" },
        { "SpellPower", "SP" },
        { "PhysicalCritChance", "PCC" },
        { "PhysicalCritDamage", "PCD" },
        { "SpellCritChance", "SCC" },
        { "SpellCritDamage", "SCD" }
    };

    public static readonly Dictionary<WeaponStatType, string> WeaponStatFormats = new()
    {
        { WeaponStatType.MaxHealth, "integer" },
        { WeaponStatType.MovementSpeed, "decimal" },
        { WeaponStatType.PrimaryAttackSpeed, "percentage" },
        { WeaponStatType.PhysicalLifeLeech, "percentage" },
        { WeaponStatType.SpellLifeLeech, "percentage" },
        { WeaponStatType.PrimaryLifeLeech, "percentage" },
        { WeaponStatType.PhysicalPower, "integer" },
        { WeaponStatType.SpellPower, "integer" },
        { WeaponStatType.PhysicalCritChance, "percentage" },
        { WeaponStatType.PhysicalCritDamage, "percentage" },
        { WeaponStatType.SpellCritChance, "percentage" },
        { WeaponStatType.SpellCritDamage, "percentage" }
    };

    public static Dictionary<BloodStatType, float> BloodStatValues = [];
    public enum BloodStatType
    {
        None, // 0
        HealingReceived, // 0
        DamageReduction, // 1
        PhysicalResistance, // 2
        SpellResistance, // 3
        ResourceYield, // 4
        CCReduction, // 5
        SpellCooldownRecoveryRate, // 6
        WeaponCooldownRecoveryRate, // 7
        UltimateCooldownRecoveryRate, // 8
        MinionDamage, // 9
        ShieldAbsorb, // 10
        BloodEfficiency // 11
    }

    public static readonly Dictionary<BloodStatType, string> BloodStatTypeAbbreviations = new()
    {
        { BloodStatType.HealingReceived, "HR" },
        { BloodStatType.DamageReduction, "DR" },
        { BloodStatType.PhysicalResistance, "PR" },
        { BloodStatType.SpellResistance, "SR" },
        { BloodStatType.ResourceYield, "RY" },
        { BloodStatType.CCReduction, "CCR" },
        { BloodStatType.SpellCooldownRecoveryRate, "SCR" },
        { BloodStatType.WeaponCooldownRecoveryRate, "WCR" },
        { BloodStatType.UltimateCooldownRecoveryRate, "UCR" },
        { BloodStatType.MinionDamage, "MD" },
        { BloodStatType.ShieldAbsorb, "SA" },
        { BloodStatType.BloodEfficiency, "BE" }
    };

    public static readonly Dictionary<string, string> BloodStatStringAbbreviations = new()
    {
        { "HealingReceived", "HR" },
        { "DamageReduction", "DR" },
        { "PhysicalResistance", "PR" },
        { "SpellResistance", "SR" },
        { "ResourceYield", "RY" },
        { "CCReduction", "CCR" },
        { "SpellCooldownRecoveryRate", "SCR" },
        { "WeaponCooldownRecoveryRate", "WCR" },
        { "UltimateCooldownRecoveryRate", "UCR" },
        { "MinionDamage", "MD" },
        { "ShieldAbsorb", "SA" },
        { "BloodEfficiency", "BE" }
    };
    public enum PlayerClass // for now subtract 1 when processing this enum, if 0 no class (otherwise 0 would be bloodknight. should probably add a None enum)
    {
        None,
        BloodKnight,
        DemonHunter,
        VampireLord,
        ShadowBlade,
        ArcaneSorcerer,
        DeathMage
    }

    public static Dictionary<PlayerClass, (List<WeaponStatType> WeaponStats, List<BloodStatType> BloodStats)> ClassStatSynergies = [];

    public static float PrestigeStatMultiplier;
    public static float ClassStatMultiplier;
    internal class ExperienceData(string percent, string level, string prestige, string playerClass)
    {
        public float Progress { get; set; } = float.Parse(percent, CultureInfo.InvariantCulture) / 100f;
        public int Level { get; set; } = int.Parse(level);
        public int Prestige { get; set; } = int.Parse(prestige);
        public PlayerClass Class { get; set; } = (PlayerClass)int.Parse(playerClass);
    }
    internal class LegacyData(string percent, string level, string prestige, string legacyType, string bonusStats) : ExperienceData(percent, level, prestige, legacyType)
    {
        public string LegacyType { get; set; } = ((BloodType)int.Parse(legacyType)).ToString();
        public List<string> BonusStats { get; set; } = Enumerable.Range(0, bonusStats.Length / 2).Select(i => ((BloodStatType)int.Parse(bonusStats.Substring(i * 2, 2))).ToString()).ToList();
    }
    internal class ExpertiseData(string percent, string level, string prestige, string expertiseType, string bonusStats) : ExperienceData(percent, level, prestige, expertiseType)
    {
        public string ExpertiseType { get; set; } = ((WeaponType)int.Parse(expertiseType)).ToString();
        public List<string> BonusStats { get; set; } = Enumerable.Range(0, bonusStats.Length / 2).Select(i => ((WeaponStatType)int.Parse(bonusStats.Substring(i * 2, 2))).ToString()).ToList();
    }
    internal class QuestData(string progress, string goal, string target, string level)
    {
        public int Progress { get; set; } = int.Parse(progress);
        public int Goal { get; set; } = int.Parse(goal);
        public string Target { get; set; } = target;
        public int Level { get; set; } = int.Parse(level);
    }
    internal class  ConfigData
    {
        public float PrestigeStatMultiplier;

        public float ClassStatMultiplier;

        public int MaxPlayerLevel;

        public int MaxLegacyLevel;

        public int MaxExpertiseLevel;

        public Dictionary<WeaponStatType, float> WeaponStatValues;

        public Dictionary<BloodStatType, float> BloodStatValues;

        public Dictionary<PlayerClass, (List<WeaponStatType> WeaponStats, List<BloodStatType> bloodStats)> ClassStatSynergies;

        public ConfigData(string prestigeMultiplier, string statSynergyMultiplier, string maxPlayerLevel, string maxLegacyLevel, string maxExpertiseLevel, string weaponStatValues, string bloodStatValues, string classStatSynergies)
        {
            //Core.Log.LogInfo($"ConfigData: {prestigeMultiplier}, {statSynergyMultiplier}, {weaponStatValues}, {bloodStatValues}, {classStatSynergies}");

            PrestigeStatMultiplier = float.Parse(prestigeMultiplier, CultureInfo.InvariantCulture);
            ClassStatMultiplier = float.Parse(statSynergyMultiplier, CultureInfo.InvariantCulture);

            MaxPlayerLevel = int.Parse(maxPlayerLevel);
            MaxLegacyLevel = int.Parse(maxLegacyLevel);
            MaxExpertiseLevel = int.Parse(maxExpertiseLevel);

            WeaponStatValues = weaponStatValues.Split(',')
            .Select((value, index) => new { Index = index + 1, Value = float.Parse(value, CultureInfo.InvariantCulture) })
            .ToDictionary(x => (WeaponStatType)x.Index, x => x.Value);

            BloodStatValues = bloodStatValues.Split(',')
            .Select((value, index) => new { Index = index + 1, Value = float.Parse(value, CultureInfo.InvariantCulture) })
            .ToDictionary(x => (BloodStatType)x.Index, x => x.Value);

            ClassStatSynergies = classStatSynergies
            .Split(',')
            .Select((value, index) => new { Value = value, Index = index })
            .GroupBy(x => x.Index / 3)
            .ToDictionary(
                g => (PlayerClass)int.Parse(g.ElementAt(0).Value),
                g => (
                    Enumerable.Range(0, g.ElementAt(1).Value.Length / 2)
                        .Select(j => (WeaponStatType)int.Parse(g.ElementAt(1).Value.Substring(j * 2, 2)))
                        .ToList(),
                    Enumerable.Range(0, g.ElementAt(2).Value.Length / 2)
                        .Select(j => (BloodStatType)int.Parse(g.ElementAt(2).Value.Substring(j * 2, 2)))
                        .ToList()
                )
            );
        }
    }
    public static List<string> ParseMessageString(string configString)
    {
        if (string.IsNullOrEmpty(configString))
        {
            return [];
        }
        return [.. configString.Split(',')];
    }
    public static void ParseConfigData(List<string> configData)
    {
        int index = 0;

        ConfigData parsedConfigData = new(
            configData[index++], // prestigeMultiplier
            configData[index++], // statSynergyMultiplier
            configData[index++], // maxPlayerLevel
            configData[index++], // maxLegacyLevel
            configData[index++], // maxExpertiseLevel
            string.Join(",", configData.Skip(index).Take(12)), // Combine the next 11 elements for weaponStatValues
            string.Join(",", configData.Skip(index += 12).Take(12)), // Combine the following 11 elements for bloodStatValues
            string.Join(",", configData.Skip(index += 12)) // Combine all remaining elements for classStatSynergies
        );

        PrestigeStatMultiplier = parsedConfigData.PrestigeStatMultiplier;
        ClassStatMultiplier = parsedConfigData.ClassStatMultiplier;

        CanvasService.ExperienceMaxLevel = parsedConfigData.MaxPlayerLevel;
        CanvasService.LegacyMaxLevel = parsedConfigData.MaxLegacyLevel;
        CanvasService.ExpertiseMaxLevel = parsedConfigData.MaxExpertiseLevel;

        WeaponStatValues = parsedConfigData.WeaponStatValues;

        BloodStatValues = parsedConfigData.BloodStatValues;

        ClassStatSynergies = parsedConfigData.ClassStatSynergies;
    }
    public static void ParsePlayerData(List<string> playerData)
    {
        int index = 0;

        ExperienceData experienceData = new(playerData[index++], playerData[index++], playerData[index++], playerData[index++]);
        LegacyData legacyData = new(playerData[index++], playerData[index++], playerData[index++], playerData[index++], playerData[index++]);
        ExpertiseData expertiseData = new(playerData[index++], playerData[index++], playerData[index++], playerData[index++], playerData[index++]);
        QuestData dailyQuestData = new(playerData[index++], playerData[index++], playerData[index++], playerData[index++]);
        QuestData weeklyQuestData = new(playerData[index++], playerData[index++], playerData[index++], playerData[index]);

        CanvasService.ExperienceProgress = experienceData.Progress;
        CanvasService.ExperienceLevel = experienceData.Level;
        CanvasService.ExperiencePrestige = experienceData.Prestige;
        CanvasService.ClassType = experienceData.Class;

        CanvasService.LegacyProgress = legacyData.Progress;
        CanvasService.LegacyLevel = legacyData.Level;
        CanvasService.LegacyPrestige = legacyData.Prestige;
        CanvasService.LegacyType = legacyData.LegacyType;
        CanvasService.LegacyBonusStats = legacyData.BonusStats;

        CanvasService.ExpertiseProgress = expertiseData.Progress;
        CanvasService.ExpertiseLevel = expertiseData.Level;
        CanvasService.ExpertisePrestige = expertiseData.Prestige;
        CanvasService.ExpertiseType = expertiseData.ExpertiseType;
        CanvasService.ExpertiseBonusStats = expertiseData.BonusStats;

        CanvasService.DailyProgress = dailyQuestData.Progress;
        CanvasService.DailyGoal = dailyQuestData.Goal;
        CanvasService.DailyTarget = dailyQuestData.Target;
        CanvasService.DailyLevel = dailyQuestData.Level;

        CanvasService.WeeklyProgress = weeklyQuestData.Progress;
        CanvasService.WeeklyGoal = weeklyQuestData.Goal;
        CanvasService.WeeklyTarget = weeklyQuestData.Target;
        CanvasService.WeeklyLevel = weeklyQuestData.Level;
    }
}
