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
        CanvasService.InitializeShiftSlot();
    }

    public IEnumerator OnUpdate()
    {
        while (true)
        {
            if (CanvasService.ShiftSlotEnabled)
            {
                if (!CanvasService._shiftActive && Core.LocalCharacter.TryGetComponent(out AbilityBar_Shared abilityBarShared))
                {
                    Entity abilityGroupEntity = abilityBarShared.CastGroup.GetEntityOnServer();
                    if (abilityGroupEntity.TryGetComponent(out AbilityGroupState abilityGroupState) && abilityGroupState.SlotIndex == 3)
                    {
                        if (CanvasService._shiftRoutine == null)
                        {
                            CanvasService._shiftRoutine = CanvasService.ShiftUpdateLoop().Start();
                            CanvasService._shiftActive = true;
                        }
                    }
                }

                CanvasService.UpdateShiftSlot();
            }
            yield return CanvasService.Delay;
        }
    }
}
