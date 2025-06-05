using HarmonyLib;
using ProjectM;
using UnityEngine.InputSystem;

namespace Eclipse.Patches;

[HarmonyPatch]
internal static class InputActionSystemPatch
{
    [HarmonyPatch(typeof(InputActionSystem), nameof(InputActionSystem.OnInputDeviceChange))]
    [HarmonyPostfix]
    static void OnInputDeviceChangePostfix(InputActionSystem __instance, InputDevice device, InputDeviceChange change)
    {
        // Core.Log.LogWarning($"Input device changed: {device.name}, Change type: {change}");

        if (__instance.ControllerType != ControllerType.KeyboardAndMouse)
        {
            // CanvasService.HandleAdaptiveElement(true);
        }
        else
        {
            // CanvasService.HandleAdaptiveElement(false);
        }
    }
}
