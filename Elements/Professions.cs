using Eclipse.Services;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace Eclipse.Elements;
internal class Professions : IReactiveElement
{
    readonly Dictionary<DataService.Profession, GameObject> _bars = [];
    readonly Dictionary<DataService.Profession, LocalizedText> _levelTexts = [];
    readonly Dictionary<DataService.Profession, Image> _progressFills = [];
    readonly Dictionary<DataService.Profession, Image> _fillImages = [];
    const int MAX_LEVEL = 100;

    public void Awake()
    {
        if (!CanvasService.ProfessionsEnabled) return;

        foreach (DataService.Profession profession in Enum.GetValues(typeof(DataService.Profession)))
        {
            GameObject bar = null;
            LocalizedText level = null;
            Image progress = null;
            Image fill = null;
            CanvasService.ConfigureVerticalProgressBar(ref bar, ref progress, ref fill, ref level, profession);
            _bars[profession] = bar;
            _levelTexts[profession] = level;
            _progressFills[profession] = progress;
            _fillImages[profession] = fill;
        }
    }

    public IEnumerator OnUpdate()
    {
        while (true)
        {
            if (CanvasService.ProfessionsEnabled)
            {
                Update(DataService.Profession.Alchemy, CanvasService._alchemyProgress, CanvasService._alchemyLevel);
                Update(DataService.Profession.Blacksmithing, CanvasService._blacksmithingProgress, CanvasService._blacksmithingLevel);
                Update(DataService.Profession.Enchanting, CanvasService._enchantingProgress, CanvasService._enchantingLevel);
                Update(DataService.Profession.Tailoring, CanvasService._tailoringProgress, CanvasService._tailoringLevel);
                Update(DataService.Profession.Fishing, CanvasService._fishingProgress, CanvasService._fishingLevel);
                Update(DataService.Profession.Harvesting, CanvasService._harvestingProgress, CanvasService._harvestingLevel);
                Update(DataService.Profession.Mining, CanvasService._miningProgress, CanvasService._miningLevel);
                Update(DataService.Profession.Woodcutting, CanvasService._woodcuttingProgress, CanvasService._woodcuttingLevel);
            }
            yield return CanvasService.Delay;
        }
    }

    void Update(DataService.Profession profession, float progress, int level)
    {
        if (_bars.TryGetValue(profession, out GameObject bar) &&
            _levelTexts.TryGetValue(profession, out LocalizedText levelText) &&
            _progressFills.TryGetValue(profession, out Image progressFill) &&
            _fillImages.TryGetValue(profession, out Image fill))
        {
            if (CanvasService._killSwitch) return;

            if (level == MAX_LEVEL)
            {
                progressFill.fillAmount = 1f;
                fill.fillAmount = 1f;
            }
            else
            {
                progressFill.fillAmount = progress;
                fill.fillAmount = level / (float)MAX_LEVEL;
            }
        }
    }

    public void Toggle()
    {
        foreach (var kvp in _bars)
        {
            bool active = !kvp.Value.activeSelf;
            kvp.Value.SetActive(active);
            CanvasService.SetElementState(kvp.Value, active);
        }
    }
}
