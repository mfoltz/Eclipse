using System.Runtime.CompilerServices;
using UnityEngine;

namespace Eclipse.Utilities;
internal static class ModificationIds
{
    // Bit layout (32-bit):
    // [31:30] Source (2 bits)
    // [29:20] Stat   (10 bits)
    // [19]    Sign   (1 bit)
    // [18:0]  Magnitude (quantized to 0.001f increments)

    const int SOURCE_SHIFT = 30;
    const int STAT_SHIFT = 20;

    const int SOURCE_MASK = 0b11;    // up to 4 sources
    const int STAT_MASK = 0x3FF;   // up to 1024 stats

    const int SIGN_SHIFT = 19;
    const int SIGN_MASK = 1 << SIGN_SHIFT;
    const int MAG_MASK = SIGN_MASK - 1; // 0..(2^19-1) = 524,287 (→ 524.287 at 1e-3)
    public enum StatSourceType { Weapon = 0, Blood = 1, Class = 2 }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int GenerateId(int sourceType, int statType, float value)
    {
        if (float.IsNaN(value) || float.IsInfinity(value)) value = 0f;

        int q = Mathf.RoundToInt(value * 1000f); // 1e-3 quantization
        int sign = (q < 0) ? SIGN_MASK : 0;
        int mag = Mathf.Clamp(Mathf.Abs(q), 0, MAG_MASK);
        int payload = sign | mag;

        int id =
            (((sourceType & SOURCE_MASK) << SOURCE_SHIFT) |
             ((statType & STAT_MASK) << STAT_SHIFT) |
             (payload & (SIGN_MASK | MAG_MASK)));

        return id;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int GenerateId(StatSourceType source, int statType, float value)
        => GenerateId((int)source, statType, value);
}
