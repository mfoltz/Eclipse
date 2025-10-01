using Eclipse.Services;
using Eclipse.Utilities;
using ProjectM;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using static Eclipse.Services.DataService;

namespace Eclipse.Tests;

public class CanvasServiceStatBufferTests
{
    static readonly FieldInfo WeaponStatsField = typeof(CanvasService).GetField("_weaponStats", BindingFlags.NonPublic | BindingFlags.Static);
    static readonly FieldInfo BloodStatsField = typeof(CanvasService).GetField("_bloodStats", BindingFlags.NonPublic | BindingFlags.Static);

    [Fact]
    public void GetWeaponStatInfo_RefreshesCachedBuff_WhenQuantizedIdRemainsStable()
    {
        Assert.NotNull(WeaponStatsField);

        var weaponStats = (Dictionary<int, ModifyUnitStatBuff_DOTS>)WeaponStatsField!.GetValue(null)!;
        var originalStats = weaponStats.ToDictionary(static kvp => kvp.Key, static kvp => kvp.Value);
        var originalWeaponValues = _weaponStatValues;
        var originalClassSynergies = _classStatSynergies;
        var originalClass = Leveling.Class;
        float originalPrestigeMultiplier = _prestigeStatMultiplier;
        int originalExpertisePrestige = CanvasService._expertisePrestige;
        int originalExpertiseLevel = CanvasService._expertiseLevel;
        int originalExpertiseMaxLevel = CanvasService._expertiseMaxLevel;

        try
        {
            weaponStats.Clear();

            _weaponStatValues = new Dictionary<WeaponStatType, float>
            {
                [WeaponStatType.BonusSpellPower] = 1.23441f
            };

            _classStatSynergies = new Dictionary<PlayerClass, (List<WeaponStatType>, List<BloodStatType>)>
            {
                [PlayerClass.None] = (new List<WeaponStatType>(), new List<BloodStatType>())
            };

            Leveling.Class = PlayerClass.None;
            _prestigeStatMultiplier = 0f;
            CanvasService._expertisePrestige = 0;
            CanvasService._expertiseLevel = 100;
            CanvasService._expertiseMaxLevel = 100;

            string statName = WeaponStatType.BonusSpellPower.ToString();
            CanvasService.GetWeaponStatInfo(statName);

            float updatedValue = 1.23449f;
            _weaponStatValues[WeaponStatType.BonusSpellPower] = updatedValue;

            CanvasService.GetWeaponStatInfo(statName);

            int statId = ModificationIds.GenerateId((int)ModificationIds.StatSourceType.Weapon, (int)WeaponStatType.BonusSpellPower, updatedValue);
            Assert.True(weaponStats.ContainsKey(statId), "Updated stat id should exist in cache.");
            Assert.Single(weaponStats);
            Assert.Equal(updatedValue, weaponStats[statId].Value, 6);
        }
        finally
        {
            weaponStats.Clear();
            foreach (var kvp in originalStats)
            {
                weaponStats[kvp.Key] = kvp.Value;
            }

            _weaponStatValues = originalWeaponValues;
            _classStatSynergies = originalClassSynergies;
            Leveling.Class = originalClass;
            _prestigeStatMultiplier = originalPrestigeMultiplier;
            CanvasService._expertisePrestige = originalExpertisePrestige;
            CanvasService._expertiseLevel = originalExpertiseLevel;
            CanvasService._expertiseMaxLevel = originalExpertiseMaxLevel;
        }
    }

    [Fact]
    public void GetBloodStatInfo_RefreshesCachedBuff_WhenQuantizedIdRemainsStable()
    {
        Assert.NotNull(BloodStatsField);

        var bloodStats = (Dictionary<int, ModifyUnitStatBuff_DOTS>)BloodStatsField!.GetValue(null)!;
        var originalStats = bloodStats.ToDictionary(static kvp => kvp.Key, static kvp => kvp.Value);
        var originalBloodValues = _bloodStatValues;
        var originalClassSynergies = _classStatSynergies;
        var originalClass = Leveling.Class;
        int originalLegacyLevel = Legacy.Level;
        int originalLegacyMaxLevel = Legacy.MaxLevel;
        int originalLegacyPrestige = Legacy.Prestige;
        float originalPrestigeMultiplier = _prestigeStatMultiplier;

        try
        {
            bloodStats.Clear();

            _bloodStatValues = new Dictionary<BloodStatType, float>
            {
                [BloodStatType.ReducedBloodDrain] = 0.12341f
            };

            _classStatSynergies = new Dictionary<PlayerClass, (List<WeaponStatType>, List<BloodStatType>)>
            {
                [PlayerClass.None] = (new List<WeaponStatType>(), new List<BloodStatType>())
            };

            Leveling.Class = PlayerClass.None;
            Legacy.Level = 100;
            Legacy.MaxLevel = 100;
            Legacy.Prestige = 0;
            _prestigeStatMultiplier = 0f;

            string statName = BloodStatType.ReducedBloodDrain.ToString();
            CanvasService.GetBloodStatInfo(statName);

            float updatedValue = 0.12349f;
            _bloodStatValues[BloodStatType.ReducedBloodDrain] = updatedValue;

            CanvasService.GetBloodStatInfo(statName);

            int statId = ModificationIds.GenerateId((int)ModificationIds.StatSourceType.Blood, (int)BloodStatType.ReducedBloodDrain, updatedValue);
            Assert.True(bloodStats.ContainsKey(statId), "Updated stat id should exist in cache.");
            Assert.Single(bloodStats);
            Assert.Equal(updatedValue, bloodStats[statId].Value, 6);
        }
        finally
        {
            bloodStats.Clear();
            foreach (var kvp in originalStats)
            {
                bloodStats[kvp.Key] = kvp.Value;
            }

            _bloodStatValues = originalBloodValues;
            _classStatSynergies = originalClassSynergies;
            Leveling.Class = originalClass;
            Legacy.Level = originalLegacyLevel;
            Legacy.MaxLevel = originalLegacyMaxLevel;
            Legacy.Prestige = originalLegacyPrestige;
            _prestigeStatMultiplier = originalPrestigeMultiplier;
        }
    }
}
