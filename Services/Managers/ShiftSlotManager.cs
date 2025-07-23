using System.Collections;

namespace Eclipse.Services.Managers;

internal class ShiftSlotManager : IReactiveElement
{
    public void Awake()
    {
    }

    public IEnumerator OnUpdate()
    {
        while (true)
        {
            if (CanvasService.ShiftSlotEnabled)
            {
                CanvasService.UpdateShiftSlot();
            }
            yield return CanvasService.Delay;
        }
    }
}
