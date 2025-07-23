using System.Collections.Generic;
using Eclipse.Services;

namespace Eclipse.DTOs;

internal class StatSynergyDto
{
    internal List<DataService.WeaponStatType> WeaponStats { get; set; } = new();
    internal List<DataService.BloodStatType> BloodStats { get; set; } = new();
}

internal class ConfigDto
{
    internal float PrestigeStatMultiplier { get; set; }
    internal float ClassStatMultiplier { get; set; }
    internal int MaxPlayerLevel { get; set; }
    internal int MaxLegacyLevel { get; set; }
    internal int MaxExpertiseLevel { get; set; }
    internal int MaxFamiliarLevel { get; set; }
    internal int ReservedFlags { get; set; }
    internal bool ExtraRecipes { get; set; }
    internal int PrimalCost { get; set; }
    internal Dictionary<DataService.WeaponStatType, float> WeaponStatValues { get; set; } = new();
    internal Dictionary<DataService.BloodStatType, float> BloodStatValues { get; set; } = new();
    internal Dictionary<DataService.PlayerClass, StatSynergyDto> ClassStatSynergies { get; set; } = new();
}
