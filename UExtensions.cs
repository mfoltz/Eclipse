using ProjectM;
using Stunlock.Core;
using System.Collections;
using UnityEngine;
using static Eclipse.Services.LocalizationService;

namespace Eclipse;

internal static class UExtensions
{
    const string EMPTY_KEY = "LocalizationKey.Empty";

    public static string GetPrefabName(this PrefabGUID prefabGUID)
    {
        return PrefabGuidsToNames.TryGetValue(prefabGUID, out string prefabName)
            ? $"{prefabName} {prefabGUID}"
            : "String.Empty";
    }

    public static string GetLocalizedName(this PrefabGUID prefabGUID)
    {
        string localizedName = GetNameFromGuidString(GetGuidString(prefabGUID));

        if (!string.IsNullOrEmpty(localizedName))
        {
            return localizedName;
        }

        return EMPTY_KEY;
    }

    public static Coroutine Start(this IEnumerator routine)
    {
        return Core.StartCoroutine(routine);
    }

    public static void Stop(this Coroutine routine)
    {
        if (routine != null) Core.StopCoroutine(routine);
    }
}
