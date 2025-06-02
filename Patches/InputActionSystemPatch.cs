using HarmonyLib;
using ProjectM;
using UnityEngine.InputSystem;

namespace Eclipse.Patches;

[HarmonyPatch]
internal static class InputActionSystemPatch
{
    [HarmonyPatch(typeof(InputActionSystem), nameof(InputActionSystem.OnInputDeviceChange))]
    [HarmonyPostfix]
    static void OnInputDeviceChangePostfix(InputDevice inputDevice, InputDeviceChange inputDeviceChange)
    {
        Core.Log.LogWarning($"Input device changed: {inputDevice.name}, Change type: {inputDeviceChange}");
    }
}
