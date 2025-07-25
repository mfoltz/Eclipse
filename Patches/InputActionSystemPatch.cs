using HarmonyLib;
using ProjectM;

namespace Eclipse.Patches;

[HarmonyPatch]
internal static class InputActionSystemPatch
{
    public static bool IsGamepad => _isGamepad;
    static bool _isGamepad = false;

    [HarmonyPatch(typeof(InputActionSystem), nameof(InputActionSystem.OnUpdate))]
    [HarmonyPostfix]
    static void OnUpdatePostfix(InputActionSystem __instance)
    {
       _isGamepad = __instance.UsingGamepad;
    }
}
