using Stunlock.Core;
using System.Globalization;
using System.IO;
using Eclipse.DTOs;
using UnityEngine;
using Eclipse.Utilities.Extensions;
using Eclipse.States;

namespace Eclipse.Services;
internal static class DataService
{
    public static IDataParser Parser { get; private set; } = new LegacyDataParser();

    public static void UseJsonParser() => Parser = new JsonDataParser();
    public static void UseLegacyParser() => Parser = new LegacyDataParser();

    internal static LevelingState Leveling { get; } = new();
    internal static LegacyState Legacy { get; } = new();
    internal static ExpertiseState Expertise { get; } = new();
    internal static FamiliarState Familiar { get; } = new();
    internal static ProfessionsState Professions { get; } = new();
    internal static QuestState Quests { get; } = new();
    internal static ShiftSlotState ShiftSlot { get; } = new();

    [Flags]
    public enum ReservedFlags : int
    {
        None = 0,
        // ExampleFlag = 1 << 0,                             
    }
    public enum TargetType
    {
        Kill,
        Craft,
        Gather,
        Fish
    }
    public enum Profession
    {
        Enchanting,
        Alchemy,
        Harvesting,
        Blacksmithing,
        Tailoring,
        Woodcutting,
        Mining,
        Fishing
    }
    public enum PlayerClass
    {
        None,
        BloodKnight,
        DemonHunter,
        VampireLord,
        ShadowBlade,
        ArcaneSorcerer,
        DeathMage
    }
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
        Brute,
        Corruption
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
        FishingPole,
        TwinBlades,
        Daggers,
        Claws
    }

    public static Dictionary<WeaponStatType, float> _weaponStatValues = [];
    public enum WeaponStatType
    {
        None,
        BonusMaxHealth,
        BonusMovementSpeed,
        PrimaryAttackSpeed,
        PhysicalLifeLeech,
        SpellLifeLeech,
        PrimaryLifeLeech,
        BonusPhysicalPower,
        BonusSpellPower,
        PhysicalCriticalStrikeChance,
        PhysicalCriticalStrikeDamage,
        SpellCriticalStrikeChance,
        SpellCriticalStrikeDamage
    }

    public static readonly Dictionary<WeaponStatType, string> WeaponStatTypeAbbreviations = new()
    {
        { WeaponStatType.BonusMaxHealth, "HP" },
        { WeaponStatType.BonusMovementSpeed, "MS" },
        { WeaponStatType.PrimaryAttackSpeed, "PAS" },
        { WeaponStatType.PhysicalLifeLeech, "PLL" },
        { WeaponStatType.SpellLifeLeech, "SLL" },
        { WeaponStatType.PrimaryLifeLeech, "PAL" },
        { WeaponStatType.BonusPhysicalPower, "PP" },
        { WeaponStatType.BonusSpellPower, "SP" },
        { WeaponStatType.PhysicalCriticalStrikeChance, "PCC" },
        { WeaponStatType.PhysicalCriticalStrikeDamage, "PCD" },
        { WeaponStatType.SpellCriticalStrikeChance, "SCC" },
        { WeaponStatType.SpellCriticalStrikeDamage, "SCD" }
    };

    public static readonly Dictionary<string, string> WeaponStatStringAbbreviations = new()
    {
        { "MaxHealth", "HP" },
        { "MovementSpeed", "MS" },
        { "PrimaryAttackSpeed", "PAS" },
        { "PhysicalLifeLeech", "PLL" },
        { "SpellLifeLeech", "SLL" },
        { "PrimaryLifeLeech", "PAL" },
        { "PhysicalPower", "PP" },
        { "SpellPower", "SP" },
        { "PhysicalCriticalStrikeChance", "PCC" },
        { "PhysicalCriticalStrikeDamage", "PCD" },
        { "SpellCriticalStrikeChance", "SCC" },
        { "SpellCriticalStrikeDamage", "SCD" }
    };

    public static readonly Dictionary<WeaponStatType, string> WeaponStatFormats = new()
    {
        { WeaponStatType.BonusMaxHealth, "integer" },
        { WeaponStatType.BonusMovementSpeed, "decimal" },
        { WeaponStatType.PrimaryAttackSpeed, "percentage" },
        { WeaponStatType.PhysicalLifeLeech, "percentage" },
        { WeaponStatType.SpellLifeLeech, "percentage" },
        { WeaponStatType.PrimaryLifeLeech, "percentage" },
        { WeaponStatType.BonusPhysicalPower, "integer" },
        { WeaponStatType.BonusSpellPower, "integer" },
        { WeaponStatType.PhysicalCriticalStrikeChance, "percentage" },
        { WeaponStatType.PhysicalCriticalStrikeDamage, "percentage" },
        { WeaponStatType.SpellCriticalStrikeChance, "percentage" },
        { WeaponStatType.SpellCriticalStrikeDamage, "percentage" }
    };

    public static Dictionary<BloodStatType, float> _bloodStatValues = [];
    public enum BloodStatType
    {
        None,
        HealingReceived,
        DamageReduction,
        PhysicalResistance,
        SpellResistance,
        ResourceYield,
        ReducedBloodDrain,
        SpellCooldownRecoveryRate,
        WeaponCooldownRecoveryRate,
        UltimateCooldownRecoveryRate,
        MinionDamage,
        AbilityAttackSpeed,
        CorruptionDamageReduction
    }

    public static readonly Dictionary<BloodStatType, string> BloodStatTypeAbbreviations = new()
    {
        { BloodStatType.HealingReceived, "HR" },
        { BloodStatType.DamageReduction, "DR" },
        { BloodStatType.PhysicalResistance, "PR" },
        { BloodStatType.SpellResistance, "SR" },
        { BloodStatType.ResourceYield, "RY" },
        { BloodStatType.ReducedBloodDrain, "RBD" },
        { BloodStatType.SpellCooldownRecoveryRate, "SCR" },
        { BloodStatType.WeaponCooldownRecoveryRate, "WCR" },
        { BloodStatType.UltimateCooldownRecoveryRate, "UCR" },
        { BloodStatType.MinionDamage, "MD" },
        { BloodStatType.AbilityAttackSpeed, "AAS" },
        { BloodStatType.CorruptionDamageReduction, "CDR" }
    };

    public static readonly Dictionary<string, string> BloodStatStringAbbreviations = new()
    {
        { "HealingReceived", "HR" },
        { "DamageReduction", "DR" },
        { "PhysicalResistance", "PR" },
        { "SpellResistance", "SR" },
        { "ResourceYield", "RY" },
        { "ReducedBloodDrain", "RBD" },
        { "SpellCooldownRecoveryRate", "SCR" },
        { "WeaponCooldownRecoveryRate", "WCR" },
        { "UltimateCooldownRecoveryRate", "UCR" },
        { "MinionDamage", "MD" },
        { "AbilityAttackSpeed", "AAS" },
        { "CorruptionDamageReduction", "CDR" }
    };

    public static Dictionary<FamiliarStatType, float> _familiarStatValues = [];
    public enum FamiliarStatType
    {
        MaxHealth,
        PhysicalPower,
        SpellPower
    }

    public static readonly Dictionary<FamiliarStatType, string> FamiliarStatTypeAbbreviations = new()
    {
        { FamiliarStatType.MaxHealth, "HP" },
        { FamiliarStatType.PhysicalPower, "PP" },
        { FamiliarStatType.SpellPower, "SP" }
    };

    public static readonly List<string> FamiliarStatStringAbbreviations = new()
    {
        { "HP" },
        { "PP" },
        { "SP" }
    };

    public static readonly Dictionary<Profession, Color> ProfessionColors = new()
    {
        { Profession.Enchanting,    new Color(0.5f, 0.1f, 0.8f, 0.5f) },
        { Profession.Alchemy,       new Color(0.1f, 0.9f, 0.7f, 0.5f) },
        { Profession.Harvesting,    new Color(0f, 0.5f, 0f, 0.5f) },
        { Profession.Blacksmithing, new Color(0.2f, 0.2f, 0.3f, 0.5f) },
        { Profession.Tailoring,     new Color(0.9f, 0.6f, 0.5f, 0.5f) },
        { Profession.Woodcutting,   new Color(0.5f, 0.3f, 0.1f, 0.5f) },
        { Profession.Mining,        new Color(0.5f, 0.5f, 0.5f, 0.5f) },
        { Profession.Fishing,       new Color(0f, 0.5f, 0.7f, 0.5f) }
    };

    public static Dictionary<PlayerClass, (List<WeaponStatType> WeaponStats, List<BloodStatType> BloodStats)> _classStatSynergies = [];

    public static float _prestigeStatMultiplier;
    public static float _classStatMultiplier;
    public static ReservedFlags _reservedFlags;
    public static bool _extraRecipes;
    public static PrefabGUID _primalCost;
    public class ProfessionData(string enchantingProgress, string enchantingLevel, string alchemyProgress, string alchemyLevel,
        string harvestingProgress, string harvestingLevel, string blacksmithingProgress, string blacksmithingLevel,
        string tailoringProgress, string tailoringLevel, string woodcuttingProgress, string woodcuttingLevel, string miningProgress,
        string miningLevel, string fishingProgress, string fishingLevel)
    {
        public float EnchantingProgress { get; set; } = float.Parse(enchantingProgress, CultureInfo.InvariantCulture) / 100f;
        public int EnchantingLevel { get; set; } = int.Parse(enchantingLevel, CultureInfo.InvariantCulture);
        public float AlchemyProgress { get; set; } = float.Parse(alchemyProgress, CultureInfo.InvariantCulture) / 100f;
        public int AlchemyLevel { get; set; } = int.Parse(alchemyLevel, CultureInfo.InvariantCulture);
        public float HarvestingProgress { get; set; } = float.Parse(harvestingProgress, CultureInfo.InvariantCulture) / 100f;
        public int HarvestingLevel { get; set; } = int.Parse(harvestingLevel, CultureInfo.InvariantCulture);
        public float BlacksmithingProgress { get; set; } = float.Parse(blacksmithingProgress, CultureInfo.InvariantCulture) / 100f;
        public int BlacksmithingLevel { get; set; } = int.Parse(blacksmithingLevel, CultureInfo.InvariantCulture);
        public float TailoringProgress { get; set; } = float.Parse(tailoringProgress, CultureInfo.InvariantCulture) / 100f;
        public int TailoringLevel { get; set; } = int.Parse(tailoringLevel, CultureInfo.InvariantCulture);
        public float WoodcuttingProgress { get; set; } = float.Parse(woodcuttingProgress, CultureInfo.InvariantCulture) / 100f;
        public int WoodcuttingLevel { get; set; } = int.Parse(woodcuttingLevel, CultureInfo.InvariantCulture);
        public float MiningProgress { get; set; } = float.Parse(miningProgress, CultureInfo.InvariantCulture) / 100f;
        public int MiningLevel { get; set; } = int.Parse(miningLevel, CultureInfo.InvariantCulture);
        public float FishingProgress { get; set; } = float.Parse(fishingProgress, CultureInfo.InvariantCulture) / 100f;
        public int FishingLevel { get; set; } = int.Parse(fishingLevel, CultureInfo.InvariantCulture);
    }
    public class ExperienceData(string percent, string level, string prestige, string playerClass)
    {
        public float Progress { get; set; } = float.Parse(percent, CultureInfo.InvariantCulture) / 100f;
        public int Level { get; set; } = int.Parse(level, CultureInfo.InvariantCulture);
        public int Prestige { get; set; } = int.Parse(prestige, CultureInfo.InvariantCulture);
        public PlayerClass Class { get; set; } = (PlayerClass)int.Parse(playerClass, CultureInfo.InvariantCulture);
    }
    public class LegacyData(string percent, string level, string prestige, string legacyType, string bonusStats) : ExperienceData(percent, level, prestige, legacyType)
    {
        public string LegacyType { get; set; } = ((BloodType)int.Parse(legacyType, CultureInfo.InvariantCulture)).ToString();
        public List<string> BonusStats { get; set; } = [..Enumerable.Range(0, bonusStats.Length / 2).Select(i => ((BloodStatType)int.Parse(bonusStats.Substring(i * 2, 2), CultureInfo.InvariantCulture)).ToString())];
    }
    public class ExpertiseData(string percent, string level, string prestige, string expertiseType, string bonusStats) : ExperienceData(percent, level, prestige, expertiseType)
    {
        public string ExpertiseType { get; set; } = ((WeaponType)int.Parse(expertiseType)).ToString();
        public List<string> BonusStats { get; set; } = [..Enumerable.Range(0, bonusStats.Length / 2).Select(i => ((WeaponStatType)int.Parse(bonusStats.Substring(i * 2, 2), CultureInfo.InvariantCulture)).ToString())];
    }
    public class QuestData(string type, string progress, string goal, string target, string isVBlood)
    {
        public TargetType TargetType { get; set; } = (TargetType)int.Parse(type, CultureInfo.InvariantCulture);
        public int Progress { get; set; } = int.Parse(progress, CultureInfo.InvariantCulture);
        public int Goal { get; set; } = int.Parse(goal, CultureInfo.InvariantCulture);
        public string Target { get; set; } = target;
        public bool IsVBlood { get; set; } = bool.Parse(isVBlood);
    }
    public class FamiliarData(string percent, string level, string prestige, string familiarName, string familiarStats)
    {
        public float Progress { get; set; } = float.Parse(percent, CultureInfo.InvariantCulture) / 100f;
        public int Level { get; set; } = int.TryParse(level, out int parsedLevel) && parsedLevel > 0 ? parsedLevel : 1;
        public int Prestige { get; set; } = int.Parse(prestige, CultureInfo.InvariantCulture);
        public string FamiliarName { get; set; } = !string.IsNullOrEmpty(familiarName) ? familiarName : "Familiar";
        public List<string> FamiliarStats { get; set; } = !string.IsNullOrEmpty(familiarStats) ? [..new List<string> { familiarStats[..4], familiarStats[4..7], familiarStats[7..] }.Select(stat => int.Parse(stat, CultureInfo.InvariantCulture).ToString())] : ["", "", ""];
    }
    public class ShiftSpellData(string index)
    {
        public int ShiftSpellIndex { get; set; } = int.Parse(index, CultureInfo.InvariantCulture);
    }
    public class ConfigDataV1_3
    {
        public float PrestigeStatMultiplier;

        public float ClassStatMultiplier;

        public int MaxPlayerLevel;

        public int MaxLegacyLevel;

        public int MaxExpertiseLevel;

        public int MaxFamiliarLevel;

        public ReservedFlags ReservedFlags;

        public bool ExtraRecipes;

        public int PrimalCost;

        public Dictionary<WeaponStatType, float> WeaponStatValues;

        public Dictionary<BloodStatType, float> BloodStatValues;

        public Dictionary<PlayerClass, (List<WeaponStatType> WeaponStats, List<BloodStatType> bloodStats)> ClassStatSynergies;
        public ConfigDataV1_3(string prestigeMultiplier, string statSynergyMultiplier, string maxPlayerLevel, 
            string maxLegacyLevel, string maxExpertiseLevel, string maxFamiliarLevel, 
            string reservedFlags, string extraRecipes, string primalCost, 
            string weaponStatValues, string bloodStatValues, string classStatSynergies)
        {
            PrestigeStatMultiplier = float.Parse(prestigeMultiplier, CultureInfo.InvariantCulture);
            ClassStatMultiplier = float.Parse(statSynergyMultiplier, CultureInfo.InvariantCulture);

            MaxPlayerLevel = int.Parse(maxPlayerLevel, CultureInfo.InvariantCulture);
            MaxLegacyLevel = int.Parse(maxLegacyLevel, CultureInfo.InvariantCulture);
            MaxExpertiseLevel = int.Parse(maxExpertiseLevel, CultureInfo.InvariantCulture);
            MaxFamiliarLevel = int.Parse(maxFamiliarLevel, CultureInfo.InvariantCulture);
            // MaxProfessionLevel = int.Parse(maxProfessionLevel, CultureInfo.InvariantCulture);
            ReservedFlags = (ReservedFlags)int.Parse(reservedFlags, CultureInfo.InvariantCulture);

            ExtraRecipes = bool.Parse(extraRecipes);
            PrimalCost = int.Parse(primalCost, CultureInfo.InvariantCulture);

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
                g => (PlayerClass)int.Parse(g.ElementAt(0).Value, CultureInfo.InvariantCulture),
                g => (
                    Enumerable.Range(0, g.ElementAt(1).Value.Length / 2)
                        .Select(j => (WeaponStatType)int.Parse(g.ElementAt(1).Value.Substring(j * 2, 2), CultureInfo.InvariantCulture))
                        .ToList(),
                    Enumerable.Range(0, g.ElementAt(2).Value.Length / 2)
                        .Select(j => (BloodStatType)int.Parse(g.ElementAt(2).Value.Substring(j * 2, 2), CultureInfo.InvariantCulture))
                        .ToList()
                )
            );
        }
    }
    public static List<string> ParseMessageString(string serverMessage)
    {
        if (string.IsNullOrEmpty(serverMessage))
        {
            return [];
        }

        return [..serverMessage.Split(',')];
    }
    public static void LoadConfigFromFile(string filePath)
    {
        if (!File.Exists(filePath)) return;
        string json = File.ReadAllText(filePath);
        Parser.ParseConfig(json);
    }

    public static void LoadPlayerDataFromFile(string filePath)
    {
        if (!File.Exists(filePath)) return;
        string json = File.ReadAllText(filePath);
        Parser.ParsePlayer(json);
    }

    public static void ParseConfigData(List<string> configData)
    {
        int index = 0;

        try
        {
            ConfigDataV1_3 parsedConfigData = new(
                configData[index++], // prestigeMultiplier
                configData[index++], // statSynergyMultiplier
                configData[index++], // maxPlayerLevel
                configData[index++], // maxLegacyLevel
                configData[index++], // maxExpertiseLevel
                configData[index++], // maxFamiliarLevel
                configData[index++], // reservedFlags, nice to have for later without breaking compatibility
                configData[index++], // extraRecipes
                configData[index++], // primalCost
                string.Join(",", configData.Skip(index).Take(12)), // Combine the next 11 elements for weaponStatValues
                string.Join(",", configData.Skip(index += 12).Take(12)), // Combine the following 11 elements for bloodStatValues
                string.Join(",", configData.Skip(index += 12)) // Combine all remaining elements for classStatSynergies
            );

            _prestigeStatMultiplier = parsedConfigData.PrestigeStatMultiplier;
            _classStatMultiplier = parsedConfigData.ClassStatMultiplier;

            Leveling.MaxLevel = parsedConfigData.MaxPlayerLevel;
            Legacy.MaxLevel = parsedConfigData.MaxLegacyLevel;
            Expertise.MaxLevel = parsedConfigData.MaxExpertiseLevel;
            Familiar.MaxLevel = parsedConfigData.MaxFamiliarLevel;

            _reservedFlags = parsedConfigData.ReservedFlags;
            // Core.Log.LogWarning($"Flags: {_reservedFlags}");

            _extraRecipes = parsedConfigData.ExtraRecipes;
            _primalCost = new(parsedConfigData.PrimalCost);

            _weaponStatValues = parsedConfigData.WeaponStatValues;
            _bloodStatValues = parsedConfigData.BloodStatValues;

            _classStatSynergies = parsedConfigData.ClassStatSynergies;

            try
            {
                if (_extraRecipes) Recipes.ModifyRecipes();
            }
            catch (Exception ex)
            {
                Core.Log.LogWarning($"Failed to modify recipes: {ex}");
            }
        }
        catch (Exception ex)
        {
            Core.Log.LogWarning($"Failed to parse config data: {ex}");
        }
    }
    public static void ParsePlayerData(List<string> playerData)
    {
        int index = 0;

        ExperienceData experienceData = new(playerData[index++], playerData[index++], playerData[index++], playerData[index++]);
        LegacyData legacyData = new(playerData[index++], playerData[index++], playerData[index++], playerData[index++], playerData[index++]);
        ExpertiseData expertiseData = new(playerData[index++], playerData[index++], playerData[index++], playerData[index++], playerData[index++]);
        FamiliarData familiarData = new(playerData[index++], playerData[index++], playerData[index++], playerData[index++], playerData[index++]);
        ProfessionData professionData = new(playerData[index++], playerData[index++], playerData[index++], playerData[index++], playerData[index++], playerData[index++], playerData[index++], playerData[index++], playerData[index++], playerData[index++], playerData[index++], playerData[index++], playerData[index++], playerData[index++], playerData[index++], playerData[index++]);
        QuestData dailyQuestData = new(playerData[index++], playerData[index++], playerData[index++], playerData[index++], playerData[index++]);
        QuestData weeklyQuestData = new(playerData[index++], playerData[index++], playerData[index++], playerData[index++], playerData[index++]);

        Leveling.Progress = experienceData.Progress;
        Leveling.Level = experienceData.Level;
        Leveling.Prestige = experienceData.Prestige;
        Leveling.Class = experienceData.Class;

        Legacy.Progress = legacyData.Progress;
        Legacy.Level = legacyData.Level;
        Legacy.Prestige = legacyData.Prestige;
        Legacy.LegacyType = legacyData.LegacyType;
        Legacy.BonusStats = legacyData.BonusStats;

        Expertise.Progress = expertiseData.Progress;
        Expertise.Level = expertiseData.Level;
        Expertise.Prestige = expertiseData.Prestige;
        Expertise.ExpertiseType = expertiseData.ExpertiseType;
        Expertise.BonusStats = expertiseData.BonusStats;

        Familiar.Progress = familiarData.Progress;
        Familiar.Level = familiarData.Level;
        Familiar.Prestige = familiarData.Prestige;
        Familiar.Name = familiarData.FamiliarName;
        Familiar.Stats = familiarData.FamiliarStats;

        Professions.EnchantingProgress = professionData.EnchantingProgress;
        Professions.EnchantingLevel = professionData.EnchantingLevel;
        Professions.AlchemyProgress = professionData.AlchemyProgress;
        Professions.AlchemyLevel = professionData.AlchemyLevel;
        Professions.HarvestingProgress = professionData.HarvestingProgress;
        Professions.HarvestingLevel = professionData.HarvestingLevel;
        Professions.BlacksmithingProgress = professionData.BlacksmithingProgress;
        Professions.BlacksmithingLevel = professionData.BlacksmithingLevel;
        Professions.TailoringProgress = professionData.TailoringProgress;
        Professions.TailoringLevel = professionData.TailoringLevel;
        Professions.WoodcuttingProgress = professionData.WoodcuttingProgress;
        Professions.WoodcuttingLevel = professionData.WoodcuttingLevel;
        Professions.MiningProgress = professionData.MiningProgress;
        Professions.MiningLevel = professionData.MiningLevel;
        Professions.FishingProgress = professionData.FishingProgress;
        Professions.FishingLevel = professionData.FishingLevel;

        Quests.DailyTargetType = dailyQuestData.TargetType;
        Quests.DailyProgress = dailyQuestData.Progress;
        Quests.DailyGoal = dailyQuestData.Goal;
        Quests.DailyTarget = dailyQuestData.Target;
        Quests.DailyVBlood = dailyQuestData.IsVBlood;

        Quests.WeeklyTargetType = weeklyQuestData.TargetType;
        Quests.WeeklyProgress = weeklyQuestData.Progress;
        Quests.WeeklyGoal = weeklyQuestData.Goal;
        Quests.WeeklyTarget = weeklyQuestData.Target;
        Quests.WeeklyVBlood = weeklyQuestData.IsVBlood;

        ShiftSpellData shiftSpellData = new(playerData[index]);
        ShiftSlot.ShiftSpellIndex = shiftSpellData.ShiftSpellIndex;
    }
    public static void ApplyConfigDto(ConfigDto dto)
    {
        _prestigeStatMultiplier = dto.PrestigeStatMultiplier;
        _classStatMultiplier = dto.ClassStatMultiplier;
        Leveling.MaxLevel = dto.MaxPlayerLevel;
        Legacy.MaxLevel = dto.MaxLegacyLevel;
        Expertise.MaxLevel = dto.MaxExpertiseLevel;
        Familiar.MaxLevel = dto.MaxFamiliarLevel;
        _reservedFlags = (ReservedFlags)dto.ReservedFlags;
        _extraRecipes = dto.ExtraRecipes;
        _primalCost = new(dto.PrimalCost);
        _weaponStatValues = dto.WeaponStatValues;
        _bloodStatValues = dto.BloodStatValues;
        _classStatSynergies = dto.ClassStatSynergies.ToDictionary(k => k.Key, v => (v.Value.WeaponStats, v.Value.BloodStats));

        try
        {
            if (_extraRecipes) Recipes.ModifyRecipes();
        }
        catch (Exception ex)
        {
            Core.Log.LogWarning($"Failed to modify recipes: {ex}");
        }
    }

    public static void ApplyPlayerDto(PlayerDataDto dto)
    {
        var leveling = dto.Experience;
        Leveling.Progress = leveling.Progress;
        Leveling.Level = leveling.Level;
        Leveling.Prestige = leveling.Prestige;
        Leveling.Class = leveling.Class;

        var legacies = dto.Legacy;
        Legacy.Progress = legacies.Progress;
        Legacy.Level = legacies.Level;
        Legacy.Prestige = legacies.Prestige;
        Legacy.LegacyType = legacies.LegacyType;
        Legacy.BonusStats = legacies.BonusStats;

        var expertise = dto.Expertise;
        Expertise.Progress = expertise.Progress;
        Expertise.Level = expertise.Level;
        Expertise.Prestige = expertise.Prestige;
        Expertise.ExpertiseType = expertise.ExpertiseType;
        Expertise.BonusStats = expertise.BonusStats;

        var familiar = dto.Familiar;
        Familiar.Progress = familiar.Progress;
        Familiar.Level = familiar.Level;
        Familiar.Prestige = familiar.Prestige;
        Familiar.Name = familiar.FamiliarName;
        Familiar.Stats = familiar.FamiliarStats;

        var profession = dto.Professions;
        Professions.EnchantingProgress = profession.EnchantingProgress;
        Professions.EnchantingLevel = profession.EnchantingLevel;
        Professions.AlchemyProgress = profession.AlchemyProgress;
        Professions.AlchemyLevel = profession.AlchemyLevel;
        Professions.HarvestingProgress = profession.HarvestingProgress;
        Professions.HarvestingLevel = profession.HarvestingLevel;
        Professions.BlacksmithingProgress = profession.BlacksmithingProgress;
        Professions.BlacksmithingLevel = profession.BlacksmithingLevel;
        Professions.TailoringProgress = profession.TailoringProgress;
        Professions.TailoringLevel = profession.TailoringLevel;
        Professions.WoodcuttingProgress = profession.WoodcuttingProgress;
        Professions.WoodcuttingLevel = profession.WoodcuttingLevel;
        Professions.MiningProgress = profession.MiningProgress;
        Professions.MiningLevel = profession.MiningLevel;
        Professions.FishingProgress = profession.FishingProgress;
        Professions.FishingLevel = profession.FishingLevel;

        var daily = dto.DailyQuest;
        Quests.DailyTargetType = daily.TargetType;
        Quests.DailyProgress = daily.Progress;
        Quests.DailyGoal = daily.Goal;
        Quests.DailyTarget = daily.Target;
        Quests.DailyVBlood = daily.IsVBlood;

        var weekly = dto.WeeklyQuest;
        Quests.WeeklyTargetType = weekly.TargetType;
        Quests.WeeklyProgress = weekly.Progress;
        Quests.WeeklyGoal = weekly.Goal;
        Quests.WeeklyTarget = weekly.Target;
        Quests.WeeklyVBlood = weekly.IsVBlood;

        ShiftSlot.ShiftSpellIndex = dto.ShiftSpellIndex;
    }
}
