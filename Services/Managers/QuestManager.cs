using System.Collections;

namespace Eclipse.Services.Managers;

internal class QuestManager : IReactiveElement
{
    public void Awake()
    {
        CanvasService.InitializeQuestTracker();
    }

    public IEnumerator OnUpdate()
    {
        while (true)
        {
            if (CanvasService.QuestsEnabled)
            {
                CanvasService.UpdateQuests();
            }
            yield return CanvasService.Delay;
        }
    }
}
