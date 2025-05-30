using Eclipse.Services;
using HarmonyLib;
using ProjectM;
using ProjectM.UI;

namespace Eclipse.Patches;

[HarmonyPatch]
internal static class InitializationPatches
{
    static readonly bool _shouldInitialize = Plugin.Leveling || Plugin.Expertise || Plugin.Legacies || Plugin.Familiars || Plugin.Quests;
    static bool _setCanvas = false;

    [HarmonyPatch(typeof(GameDataManager), nameof(GameDataManager.OnUpdate))]
    [HarmonyPostfix]
    static void OnUpdatePostfix(GameDataManager __instance)
    {
        if (!__instance.GameDataInitialized || !__instance.World.IsCreated) return;

        try
        {
            if (_shouldInitialize && !Core._initialized)
            {
                Core.Initialize(__instance);

                if (Core._initialized)
                {
                    Core.Log.LogInfo($"{MyPluginInfo.PLUGIN_NAME}[{MyPluginInfo.PLUGIN_VERSION}] initialized on client!");
                }
            }
        }
        catch (Exception ex)
        {
            Core.Log.LogError($"{MyPluginInfo.PLUGIN_NAME}[{MyPluginInfo.PLUGIN_VERSION}] failed to initialize on client, exiting on try-catch... {ex}");
        }
    }

    [HarmonyPatch(typeof(UICanvasSystem), nameof(UICanvasSystem.UpdateHideIfDisabled))]
    [HarmonyPostfix]
    static void OnUpdatePostfix(UICanvasBase canvas)
    {
        if (!_setCanvas && Core._initialized)
        {
            _setCanvas = true;
            Core.SetCanvas(canvas);
        }
    }

    /*
    [HarmonyPatch(typeof(CommonClientDataSystem), nameof(CommonClientDataSystem.OnUpdate))]
    [HarmonyPostfix]
    static void OnUpdatePostfix(CommonClientDataSystem __instance)
    {
        if (!Core._initialized) return;

        NativeArray<Entity> entities = __instance.EntityQueries[0].ToEntityArray(Allocator.Temp);

        try
        {
            foreach (Entity entity in entities)
            {
                if (entity.Has<LocalUser>()) ClientChatSystemPatch._localUser = entity;
                break;
            }
        }
        finally
        {
            entities.Dispose();
        }

        entities = __instance.EntityQueries[1].ToEntityArray(Allocator.Temp);

        try
        {
            foreach (Entity entity in entities)
            {
                if (entity.Has<LocalCharacter>()) ClientChatSystemPatch._localCharacter = entity;
                CanvasService._localCharacter = entity;

                break;
            }
        }
        finally
        {
            entities.Dispose();
        }
    }
    */

    [HarmonyPatch(typeof(ClientBootstrapSystem), nameof(ClientBootstrapSystem.OnDestroy))]
    [HarmonyPrefix]
    static void OnUpdatePrefix(ClientBootstrapSystem __instance)
    {
        CanvasService._killSwitch = true;

        CanvasService._shiftRoutine.Stop();
        CanvasService._canvasRoutine.Stop();

        CanvasService._shiftRoutine = null;
        CanvasService._canvasRoutine = null;

        CanvasService._active = false;
        CanvasService._shiftActive = false;
        CanvasService._ready = false;

        ClientChatSystemPatch._userRegistered = false;
        ClientChatSystemPatch._pending = false;

        CanvasService._version = string.Empty;

        _setCanvas = false;

        CanvasService._abilityTooltipData = null;
        CanvasService._dailyQuestIcon = null;
        CanvasService._weeklyQuestIcon = null;

        CanvasService.ResetState(); // need to add the rest here, just sprites for now

        Core.Reset();
    }
}
