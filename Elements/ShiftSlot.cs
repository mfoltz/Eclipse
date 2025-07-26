using Eclipse.Services;
using Eclipse.Utilities.Extensions;
using ProjectM;
using Stunlock.Core;
using System.Collections;
using Unity.Entities;

namespace Eclipse.Elements;

internal class ShiftSlot : IReactiveElement
{
    public void Awake()
    {
        if (CanvasService.ShiftSlotEnabled)
        {
            CanvasService.ConfigureShiftSlot(ref CanvasService._abilityDummyObject, ref CanvasService._abilityBarEntry,
                ref CanvasService._uiState, ref CanvasService._cooldownParentObject, ref CanvasService._cooldownText,
                ref CanvasService._chargesTextObject, ref CanvasService._cooldownFillImage, ref CanvasService._chargesText,
                ref CanvasService._chargeCooldownFillImage, ref CanvasService._chargeCooldownImageObject,
                ref CanvasService._abilityEmptyIcon, ref CanvasService._abilityIcon, ref CanvasService._keybindObject);
        }
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

                if (!CanvasService._killSwitch && CanvasService._shiftActive && Core.LocalCharacter.TryGetComponent(out AbilityBar_Shared bar))
                {
                    Entity abilityGroupEntity = bar.CastGroup.GetEntityOnServer();
                    Entity abilityCastEntity = bar.CastAbility.GetEntityOnServer();

                    if (abilityGroupEntity.TryGetComponent(out AbilityGroupState abilityGroupState) && abilityGroupState.SlotIndex == 3)
                    {
                        PrefabGUID currentPrefabGUID = abilityGroupEntity.GetPrefabGUID();

                        if (CanvasService.TryUpdateTooltipData(abilityGroupEntity, currentPrefabGUID))
                        {
                            CanvasService.UpdateAbilityData(CanvasService._abilityTooltipData, abilityGroupEntity, abilityCastEntity, currentPrefabGUID);
                        }
                        else if (CanvasService._abilityTooltipData != null)
                        {
                            CanvasService.UpdateAbilityData(CanvasService._abilityTooltipData, abilityGroupEntity, abilityCastEntity, currentPrefabGUID);
                        }
                    }

                    if (CanvasService._abilityTooltipData != null)
                    {
                        CanvasService.UpdateAbilityState(abilityGroupEntity, abilityCastEntity);
                    }
                }
            }
            yield return CanvasService.Delay;
        }
    }

    public void Toggle()
    {
        bool active = !CanvasService._abilityDummyObject.activeSelf;
        CanvasService._abilityDummyObject.SetActive(active);
        CanvasService.SetElementState(CanvasService._abilityDummyObject, active);
    }
}
