using System.Collections;

namespace Eclipse.Services.Managers;

internal class ProfessionManager : IReactiveElement
{
    public void Awake()
    {
        CanvasService.Professions?.Awake();
    }

    public IEnumerator OnUpdate()
    {
        return CanvasService.Professions?.OnUpdate() ?? Dummy();

        static IEnumerator Dummy()
        {
            yield break;
        }
    }
}
