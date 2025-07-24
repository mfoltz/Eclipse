using System.Collections;

namespace Eclipse.Services.Managers;

internal class QuestManager : IReactiveElement
{
    public void Awake()
    {
        CanvasService.Quests?.Awake();
    }

    public IEnumerator OnUpdate()
    {
        return CanvasService.Quests?.OnUpdate() ?? Dummy();

        static IEnumerator Dummy()
        {
            yield break;
        }
    }
}
