using System.Collections;

namespace Eclipse.Services.Managers;

internal class LevelingManager : IReactiveElement
{
    public void Awake()
    {
        CanvasService.Leveling?.Awake();
    }

    public IEnumerator OnUpdate()
    {
        return CanvasService.Leveling?.OnUpdate() ?? Dummy();

        static IEnumerator Dummy()
        {
            yield break;
        }
    }
}
