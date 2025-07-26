using ProjectM;
using ProjectM.UI;
using Stunlock.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Eclipse.Elements.States;

internal class ShiftState
{
    public PrefabGUID AbilityGroupPrefabGuid { get; set; }
    public AbilityTooltipData AbilityTooltipData { get; set; }

    public GameObject AbilityDummyObject { get; set; }
    public AbilityBarEntry AbilityBarEntry { get; set; }
    public AbilityBarEntry.UIState UiState { get; set; }

    public GameObject CooldownParentObject { get; set; }
    public TextMeshProUGUI CooldownText { get; set; }
    public GameObject ChargeCooldownImageObject { get; set; }
    public GameObject ChargesTextObject { get; set; }
    public TextMeshProUGUI ChargesText { get; set; }
    public Image CooldownFillImage { get; set; }
    public Image ChargeCooldownFillImage { get; set; }

    public GameObject AbilityEmptyIcon { get; set; }
    public GameObject AbilityIcon { get; set; }
    public GameObject KeybindObject { get; set; }

    public int ShiftSpellIndex { get; set; } = -1;
    public double CooldownEndTime { get; set; }
    public float CooldownRemaining { get; set; }
    public float CooldownTime { get; set; }
    public int CurrentCharges { get; set; }
    public int MaxCharges { get; set; }
    public double ChargeUpEndTime { get; set; }
    public float ChargeUpTime { get; set; }
    public float ChargeUpTimeRemaining { get; set; }
    public float ChargeCooldownTime { get; set; }
}
