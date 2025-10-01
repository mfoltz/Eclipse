using System;
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
    const int METADATA_BITS = 32;
    const ulong METADATA_MASK = (1UL << METADATA_BITS) - 1;
    const int SALT_SHIFT = METADATA_BITS;
    const ulong SALT_MASK = ~METADATA_MASK;
    static readonly ulong SALT_RANGE_MASK = (1UL << (64 - METADATA_BITS)) - 1;

    /// <summary>
    /// Optional session-specific seed added to the deterministic salt generator.
    /// </summary>
    public static int SessionSeed { get; set; }
    
    public enum StatSourceType
    {
        Weapon = 0,
        Blood = 1,
        Class = 2
    }
    /// <summary>
    /// Generates a 64-bit identifier using the following bit layout:
    /// <code>
    /// [63:32] Deterministic salt derived from the source, stat, quantized magnitude, and <see cref="SessionSeed"/>.
    /// [31:30] Source (2 bits)
    /// [29:20] Stat (10 bits)
    /// [19]    Sign (1 bit)
    /// [18:0]  Magnitude (quantized to 0.001f increments)
    /// </code>
    /// </summary>
    public static ulong GenerateId(int sourceType, int statType, float value)
    {
        int quantizedValue = Mathf.RoundToInt(value * 1000f);
        int clampedMagnitude = Mathf.Clamp(Mathf.Abs(quantizedValue), 0, MAX_MAGNITUDE);
        int signComponent = quantizedValue < 0 ? SIGN_MASK : 0;
        int encodedValue = signComponent | clampedMagnitude;
        uint metadata = (uint)(((sourceType & MAX_SOURCE) << SOURCE_SHIFT) |
               ((statType & MAX_STAT) << STAT_SHIFT) |
               (encodedValue & MAX_VALUE));

        ulong seed = unchecked((uint)HashCode.Combine(SessionSeed, sourceType, statType, encodedValue));
        ulong salt = SplitMix64(seed) & SALT_RANGE_MASK;

        return (salt << SALT_SHIFT) | metadata;
    }
    /// <summary>
    /// Removes the salt component from a generated identifier, leaving only the metadata payload.
    /// </summary>
    public static uint ExtractMetadata(ulong id) => (uint)(id & METADATA_MASK);

    /// <summary>
    /// Extracts only the salt component from a generated identifier.
    /// </summary>
    public static ulong ExtractSalt(ulong id) => (id & SALT_MASK) >> SALT_SHIFT;

    public static bool TryParseId(ulong id, out string description)
    {
        uint metadata = ExtractMetadata(id);
        int source = (int)((metadata >> SOURCE_SHIFT) & MAX_SOURCE);
        int stat = (int)((metadata >> STAT_SHIFT) & MAX_STAT);
        int quantizedValue = (int)(metadata & MAX_VALUE);
        bool isNegative = (quantizedValue & SIGN_MASK) != 0;
        int magnitude = quantizedValue & MAX_MAGNITUDE;

        float value = magnitude / 1000f;
        if (isNegative)
            value = -value;
        description = $"Unknown (raw id: {metadata})";

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

    static ulong SplitMix64(ulong seed)
    {
        ulong z = seed + 0x9E3779B97F4A7C15UL;
        z = (z ^ (z >> 30)) * 0xBF58476D1CE4E5B9UL;
        z = (z ^ (z >> 27)) * 0x94D049BB133111EBUL;
        return z ^ (z >> 31);
    }
}
