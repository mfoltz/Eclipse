using HarmonyLib;
using ProjectM;

namespace Eclipse.Patches;

[HarmonyPatch]
internal static class InputActionSystemPatch
{
    public static bool IsGamepad => _isGamepad;
    static bool _isGamepad;

    /*
    [HarmonyPatch(typeof(InputActionSystem), nameof(InputActionSystem.OnInputDeviceChange))]
    [HarmonyPostfix]
    static void OnInputDeviceChangePostfix(InputActionSystem __instance, InputDevice device, InputDeviceChange change)
    {
        Core.Log.LogWarning($"Input device changed: {device.name}, Change type: {change}");
        string deviceName = device.name.ToLower();

        if (deviceName.Contains("gamepad") || deviceName.Contains("controller") || deviceName.Contains("xinput") || deviceName.Contains("dualshock") || deviceName.Contains("ps4") || deviceName.Contains("xbox"))
        {
            if (change.Equals(InputDeviceChange.Removed, InputDeviceChange.Disconnected))
            {
                Core.Log.LogWarning($"Detected keyboard + mouse (gamepad disconnected or removed): {device.name} | {change}");
                CanvasService.HandleAdaptiveElement(false);
            }
            else
            {
                Core.Log.LogWarning($"Detected gamepad: {device.name} | {change}");
                CanvasService.HandleAdaptiveElement(true);
            }
        }
        else
        {
            Core.Log.LogWarning($"Detected keyboard + mouse: {device.name} | {change}");
            CanvasService.HandleAdaptiveElement(false);
        }
    }
    */

    [HarmonyPatch(typeof(InputActionSystem), nameof(InputActionSystem.OnUpdate))]
    [HarmonyPostfix]
    static void OnUpdatePostfix(InputActionSystem __instance)
    {
       _isGamepad = __instance.UsingGamepad;
    }
}
