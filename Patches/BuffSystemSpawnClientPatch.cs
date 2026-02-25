using Eclipse.Utilities;
using HarmonyLib;
using ProjectM;
using Stunlock.Core;
using Unity.Collections;
using static Eclipse.Utilities.ShadowMatter;

namespace Eclipse.Patches;

[HarmonyPatch]
internal static class BuffSystemSpawnClientPatch
{
    [HarmonyPatch(typeof(BuffSystem_Spawn_Client), nameof(BuffSystem_Spawn_Client.OnUpdate))]
    [HarmonyPostfix]
    static void OnUpdate(BuffSystem_Spawn_Client __instance)
    {
        if (!Core.HasInitialized)
            return;

        var prefabGuids = __instance._Query.ToComponentDataArray<PrefabGUID>(Allocator.Temp);

        try
        {
            foreach(PrefabGUID prefabGuid in prefabGuids)
            {
                if (ShouldOverride(prefabGuid))
                    YieldChange(prefabGuid).Run();
            }
        }
        finally
        {
            prefabGuids.Dispose();
        }
    }
}
