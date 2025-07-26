using Eclipse.Elements;
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
        return CanvasService.Familiar?.OnUpdate() ?? OnBreak();

        static IEnumerator OnBreak()
        {
            yield break;
        }
    }
}
