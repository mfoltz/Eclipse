using Eclipse.Elements;
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
        return CanvasService.Quests?.OnUpdate() ?? OnBreak();

        static IEnumerator OnBreak()
        {
            yield break;
        }
    }
}
