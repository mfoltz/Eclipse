using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using ProjectM.UI;

namespace Eclipse.Services;

internal class Expertise : IReactiveElement
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
        if (CanvasService.ExpertiseEnabled)
        {
            CanvasService.ConfigureHorizontalProgressBar(ref _barGameObject, ref _informationPanel,
                ref _fill, ref _text, ref _header, CanvasService.Element.Expertise, Color.grey,
                ref _firstStat, ref _secondStat, ref _thirdStat);
        }
    }

    public IEnumerator OnUpdate()
    {
        while (true)
        {
            if (CanvasService.ExpertiseEnabled)
            {
                CanvasService.UpdateBar(CanvasService._expertiseProgress, CanvasService._expertiseLevel,
                    CanvasService._expertiseMaxLevel, CanvasService._expertisePrestige,
                    _text, _header, _fill, CanvasService.Element.Expertise, CanvasService._expertiseType);
                CanvasService.UpdateWeaponStats(CanvasService._expertiseBonusStats,
                    new List<LocalizedText> { _firstStat, _secondStat, _thirdStat }, CanvasService.GetWeaponStatInfo);
                CanvasService.GetAndUpdateWeaponStatBuffer(CanvasService.LocalCharacter);
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
