using System.Collections;

namespace Eclipse.Services.Managers;

internal class ExperienceManager : IReactiveElement
{
    public void Awake()
    {
        CanvasService.Experience?.Awake();
    }

    public IEnumerator OnUpdate()
    {
        return CanvasService.Experience?.OnUpdate() ?? Dummy();

        static IEnumerator Dummy()
        {
            yield break;
        }
    }
}
