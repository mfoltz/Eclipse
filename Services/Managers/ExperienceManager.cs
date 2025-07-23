using System.Collections;

namespace Eclipse.Services.Managers;

internal class ExperienceManager : IReactiveElement
{
    public void Awake()
    {
        CanvasService.InitializeExperienceBar();
    }

    public IEnumerator OnUpdate()
    {
        while (true)
        {
            if (CanvasService.ExperienceEnabled)
            {
                CanvasService.UpdateExperience();
            }
            yield return CanvasService.Delay;
        }
    }
}
