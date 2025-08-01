using Eclipse.Patches;
using Eclipse.Services;
using ProjectM;
using System.Collections;

namespace Eclipse.Elements;
internal class SyncAdaptives : IReactiveElement
{
    static bool IsSynced => InputActionSystemPatch.IsGamepad
        ? CanvasService.ControllerType.Equals(ControllerType.Gamepad)
        : CanvasService.ControllerType.Equals(ControllerType.KeyboardAndMouse);
    static bool BuffActive { get; } = CanvasService.LegaciesEnabled || CanvasService.ExpertiseEnabled;
    public void Awake()
    {

    }
    public IEnumerator OnUpdate()
    {
        while (true)
        {
            if (!IsSynced) CanvasService.SyncAdaptiveElements();
            if (BuffActive) CanvasService.UpdateBuffStatBuffer();

            yield return CanvasService.Delay;
        }
    }
}
