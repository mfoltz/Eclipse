using Eclipse.Elements;
using System.Collections;

namespace Eclipse.Services.Managers;
internal class ShiftManager : IReactiveElement
{
    public void Awake()
    {
        CanvasService.ShiftSlot?.Awake();
    }
    public IEnumerator OnUpdate()
    {
        return CanvasService.ShiftSlot?.OnUpdate() ?? OnBreak();

        static IEnumerator OnBreak()
        {
            yield break;
        }
    }
}
