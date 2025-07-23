using System.Collections;

namespace Eclipse.Services.Managers;

internal class LegacyManager : IReactiveElement
{
    public void Awake()
    {
        CanvasService.InitializeLegacyBar();
    }

    public IEnumerator OnUpdate()
    {
        while (true)
        {
            if (CanvasService.LegacyEnabled)
            {
                CanvasService.UpdateLegacy();
            }
            yield return CanvasService.Delay;
        }
    }
}
