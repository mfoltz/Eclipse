using System.Collections;

namespace Eclipse.Services.Managers;

internal class FamiliarManager : IReactiveElement
{
    public void Awake()
    {
    }

    public IEnumerator OnUpdate()
    {
        while (true)
        {
            if (CanvasService.FamiliarEnabled)
            {
                CanvasService.UpdateFamiliar();
            }
            yield return CanvasService.Delay;
        }
    }
}
