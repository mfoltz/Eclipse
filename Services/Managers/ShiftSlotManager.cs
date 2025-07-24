using System.Collections;
using ProjectM;
using Eclipse;
using Eclipse.Utilities.Extensions;
using Unity.Entities;

namespace Eclipse.Services.Managers;

internal class ShiftSlotManager : IReactiveElement
{
    public void Awake()
    {
        CanvasService.ShiftSlot?.Awake();
    }

    public IEnumerator OnUpdate()
    {
        return CanvasService.ShiftSlot?.OnUpdate() ?? Dummy();

        static IEnumerator Dummy()
        {
            yield break;
        }
    }
}
