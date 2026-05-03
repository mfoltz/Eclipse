using HarmonyLib;
using ProjectM.Hybrid;
using Unity.Entities;
using UnityEngine;
namespace Eclipse.Patches;

/*
[HarmonyPatch]
internal static class HybridPatches
{
    [HarmonyPatch(typeof(HybridModelSystem), nameof(HybridModelSystem.CreateHybridEntity))]
    [HarmonyPostfix]
    static void CreateHybridEntity(Entity forEntity, GameObject go)
    {
        Core.Log.LogWarning($"{forEntity}:{go.name}");
    }
}
*/

