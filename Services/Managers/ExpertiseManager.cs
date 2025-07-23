using System.Collections;

namespace Eclipse.Services.Managers;

internal class ExpertiseManager : IReactiveElement
{
    public void Awake()
    {
    }

    public IEnumerator OnUpdate()
    {
        while (true)
        {
            if (CanvasService.ExpertiseEnabled)
            {
                CanvasService.UpdateExpertise();
            }
            yield return CanvasService.Delay;
        }
    }
}
