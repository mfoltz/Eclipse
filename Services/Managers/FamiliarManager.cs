using System.Collections;

namespace Eclipse.Services.Managers;

internal class FamiliarManager : IReactiveElement
{
    public void Awake()
    {
        CanvasService.Familiar?.Awake();
    }

    public IEnumerator OnUpdate()
    {
        return CanvasService.Familiar?.OnUpdate() ?? Dummy();

        static IEnumerator Dummy()
        {
            yield break;
        }
    }
}
