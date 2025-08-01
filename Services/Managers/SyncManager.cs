using Eclipse.Elements;
using System.Collections;

namespace Eclipse.Services.Managers;
internal class SyncManager : IReactiveElement
{
    public void Awake()
    {
        CanvasService.SyncAdaptives?.Awake();
    }
    public IEnumerator OnUpdate()
    {
        return CanvasService.SyncAdaptives?.OnUpdate() ?? OnBreak();

        static IEnumerator OnBreak()
        {
            yield break;
        }
    }
}
