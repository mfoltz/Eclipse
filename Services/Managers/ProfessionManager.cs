using System.Collections;

namespace Eclipse.Services.Managers;

internal class ProfessionManager : IReactiveElement
{
    public void Awake()
    {
        CanvasService.InitializeProfessions();
    }

    public IEnumerator OnUpdate()
    {
        while (true)
        {
            if (CanvasService.ProfessionsEnabled)
            {
                CanvasService.UpdateProfessions();
            }
            yield return CanvasService.Delay;
        }
    }
}
