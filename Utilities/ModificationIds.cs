using UnityEngine;
using static Eclipse.Services.DataService;

namespace Eclipse.Utilities;
internal static class ModificationIds
{
    const int SOURCE_SHIFT = 30; 
    const int STAT_SHIFT = 20;   
    const int MAX_SOURCE = 0b11;
    const int MAX_STAT = 0x3FF;
    const int SIGN_SHIFT = 19;
    const int SIGN_MASK = 1 << SIGN_SHIFT;
    const int MAX_MAGNITUDE = SIGN_MASK - 1;
    const int MAX_VALUE = SIGN_MASK | MAX_MAGNITUDE;
    public enum StatSourceType
    {
        Weapon = 0,
        Blood = 1,
        Class = 2
    }
    public static int GenerateId(int sourceType, int statType, float value)
    {
        int quantizedValue = Mathf.RoundToInt(value * 1000f);
        int clampedMagnitude = Mathf.Clamp(Mathf.Abs(quantizedValue), 0, MAX_MAGNITUDE);
        int signComponent = quantizedValue < 0 ? SIGN_MASK : 0;
        int encodedValue = signComponent | clampedMagnitude;

        return (sourceType & MAX_SOURCE) << SOURCE_SHIFT |
               (statType & MAX_STAT) << STAT_SHIFT |
               (encodedValue & MAX_VALUE);
    }
    public static bool TryParseId(int id, out string description)
    {
        int source = (id >> SOURCE_SHIFT) & MAX_SOURCE;
        int stat = (id >> STAT_SHIFT) & MAX_STAT;
        int quantizedValue = id & MAX_VALUE;
        bool isNegative = (quantizedValue & SIGN_MASK) != 0;
        int magnitude = quantizedValue & MAX_MAGNITUDE;

        float value = magnitude / 1000f;
        if (isNegative)
            value = -value;
        description = $"Unknown (raw id: {id})";

        switch ((StatSourceType)source)
        {
            case StatSourceType.Weapon:
                if (Enum.IsDefined(typeof(WeaponStatType), stat))
                {
                    description = $"Weapon Stat: {(WeaponStatType)stat}, Value: {value:F3}";
                    return true;
                }
                break;
            case StatSourceType.Blood:
                if (Enum.IsDefined(typeof(BloodStatType), stat))
                {
                    description = $"Blood Stat: {(BloodStatType)stat}, Value: {value:F3}";
                    return true;
                }
                break;
            case StatSourceType.Class:
                if (Enum.IsDefined(typeof(WeaponStatType), stat)) // Assuming class uses same stats
                {
                    description = $"Class Stat: {(WeaponStatType)stat}, Value: {value:F3}";
                    return true;
                }
                break;
        }

        return false;
    }
}
