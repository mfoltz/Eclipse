using System.Collections;

namespace Eclipse.Services.Managers;

internal class LegacyManager : IReactiveElement
{
    public void Awake()
    {
        CanvasService.Legacies?.Awake();
    }

    public IEnumerator OnUpdate()
    {
        return CanvasService.Legacies?.OnUpdate() ?? Dummy();

        static IEnumerator Dummy()
        {
            yield break;
        }
    }
}
