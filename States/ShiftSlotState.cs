using ProjectM;
using ProjectM.UI;
using Stunlock.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Eclipse.States;

internal class ShiftSlotState
{
    internal PrefabGUID AbilityGroupPrefabGuid { get; set; }
    internal AbilityTooltipData AbilityTooltipData { get; set; }

    internal GameObject AbilityDummyObject { get; set; }
    internal AbilityBarEntry AbilityBarEntry { get; set; }
    internal AbilityBarEntry.UIState UiState { get; set; }

    internal GameObject CooldownParentObject { get; set; }
    internal TextMeshProUGUI CooldownText { get; set; }
    internal GameObject ChargeCooldownImageObject { get; set; }
    internal GameObject ChargesTextObject { get; set; }
    internal TextMeshProUGUI ChargesText { get; set; }
    internal Image CooldownFillImage { get; set; }
    internal Image ChargeCooldownFillImage { get; set; }

    internal GameObject AbilityEmptyIcon { get; set; }
    internal GameObject AbilityIcon { get; set; }
    internal GameObject KeybindObject { get; set; }

    internal int ShiftSpellIndex { get; set; } = -1;
    internal double CooldownEndTime { get; set; }
    internal float CooldownRemaining { get; set; }
    internal float CooldownTime { get; set; }
    internal int CurrentCharges { get; set; }
    internal int MaxCharges { get; set; }
    internal double ChargeUpEndTime { get; set; }
    internal float ChargeUpTime { get; set; }
    internal float ChargeUpTimeRemaining { get; set; }
    internal float ChargeCooldownTime { get; set; }
}
