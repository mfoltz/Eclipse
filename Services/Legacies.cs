using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using ProjectM.UI;

namespace Eclipse.Services;

internal class Legacies : IReactiveElement
{
    GameObject _barGameObject;
    GameObject _informationPanel;
    LocalizedText _firstStat;
    LocalizedText _secondStat;
    LocalizedText _thirdStat;
    LocalizedText _header;
    LocalizedText _text;
    Image _fill;

    public void Awake()
    {
        if (CanvasService.LegacyEnabled)
        {
            CanvasService.ConfigureHorizontalProgressBar(ref _barGameObject, ref _informationPanel,
                ref _fill, ref _text, ref _header, CanvasService.Element.Legacy, Color.red,
                ref _firstStat, ref _secondStat, ref _thirdStat);
        }
    }

    public IEnumerator OnUpdate()
    {
        while (true)
        {
            if (CanvasService.LegacyEnabled)
            {
                CanvasService.UpdateBar(CanvasService._legacyProgress, CanvasService._legacyLevel,
                    CanvasService._legacyMaxLevel, CanvasService._legacyPrestige,
                    _text, _header, _fill, CanvasService.Element.Legacy, CanvasService._legacyType);
                CanvasService.UpdateBloodStats(CanvasService._legacyBonusStats,
                    new List<LocalizedText> { _firstStat, _secondStat, _thirdStat }, CanvasService.GetBloodStatInfo);
            }
            yield return CanvasService.Delay;
        }
    }

    public void Toggle()
    {
        bool active = !_barGameObject.activeSelf;
        _barGameObject.SetActive(active);
        CanvasService.SetElementState(_barGameObject, active);
    }
}
