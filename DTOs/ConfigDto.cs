using Eclipse.Services;

namespace Eclipse.DTOs;
internal class StatSynergyDto
{
    public List<DataService.WeaponStatType> WeaponStats { get; set; } = [];
    public List<DataService.BloodStatType> BloodStats { get; set; } = [];
}
internal class ConfigDto
{
    public float PrestigeStatMultiplier { get; set; }
    public float ClassStatMultiplier { get; set; }
    public int MaxPlayerLevel { get; set; }
    public int MaxLegacyLevel { get; set; }
    public int MaxExpertiseLevel { get; set; }
    public int MaxFamiliarLevel { get; set; }
    public int ReservedFlags { get; set; }
    public bool ExtraRecipes { get; set; }
    public int PrimalCost { get; set; }
    public Dictionary<DataService.WeaponStatType, float> WeaponStatValues { get; set; } = [];
    public Dictionary<DataService.BloodStatType, float> BloodStatValues { get; set; } = [];
    public Dictionary<DataService.PlayerClass, StatSynergyDto> ClassStatSynergies { get; set; } = [];
}
