using System.Collections.Generic;
using Eclipse.Services;

namespace Eclipse.DTOs;

internal class PlayerDataDto
{
    internal ExperienceDto Experience { get; set; } = new();
    internal LegacyDto Legacy { get; set; } = new();
    internal ExpertiseDto Expertise { get; set; } = new();
    internal FamiliarDto Familiar { get; set; } = new();
    internal ProfessionDto Professions { get; set; } = new();
    internal QuestDto DailyQuest { get; set; } = new();
    internal QuestDto WeeklyQuest { get; set; } = new();
    internal int ShiftSpellIndex { get; set; }
}

internal class ExperienceDto
{
    internal float Progress { get; set; }
    internal int Level { get; set; }
    internal int Prestige { get; set; }
    internal DataService.PlayerClass Class { get; set; }
}

internal class LegacyDto : ExperienceDto
{
    internal string LegacyType { get; set; } = string.Empty;
    internal List<string> BonusStats { get; set; } = new();
}

internal class ExpertiseDto : ExperienceDto
{
    internal string ExpertiseType { get; set; } = string.Empty;
    internal List<string> BonusStats { get; set; } = new();
}

internal class FamiliarDto
{
    internal float Progress { get; set; }
    internal int Level { get; set; }
    internal int Prestige { get; set; }
    internal string FamiliarName { get; set; } = string.Empty;
    internal List<string> FamiliarStats { get; set; } = new();
}

internal class ProfessionDto
{
    internal float EnchantingProgress { get; set; }
    internal int EnchantingLevel { get; set; }
    internal float AlchemyProgress { get; set; }
    internal int AlchemyLevel { get; set; }
    internal float HarvestingProgress { get; set; }
    internal int HarvestingLevel { get; set; }
    internal float BlacksmithingProgress { get; set; }
    internal int BlacksmithingLevel { get; set; }
    internal float TailoringProgress { get; set; }
    internal int TailoringLevel { get; set; }
    internal float WoodcuttingProgress { get; set; }
    internal int WoodcuttingLevel { get; set; }
    internal float MiningProgress { get; set; }
    internal int MiningLevel { get; set; }
    internal float FishingProgress { get; set; }
    internal int FishingLevel { get; set; }
}

internal class QuestDto
{
    internal DataService.TargetType TargetType { get; set; }
    internal int Progress { get; set; }
    internal int Goal { get; set; }
    internal string Target { get; set; } = string.Empty;
    internal bool IsVBlood { get; set; }
}
