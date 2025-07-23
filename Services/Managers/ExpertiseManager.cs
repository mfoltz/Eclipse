using System.Collections;

namespace Eclipse.Services.Managers;

internal class ExpertiseManager : IReactiveElement
{
    public void Awake()
    {
        CanvasService.Expertise?.Awake();
    }

    public IEnumerator OnUpdate()
    {
        return CanvasService.Expertise?.OnUpdate() ?? Dummy();

        static IEnumerator Dummy()
        {
            yield break;
        }
    }
}
