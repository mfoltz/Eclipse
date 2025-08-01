using Eclipse.Services;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace Eclipse.Elements;
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
                    [_firstStat, _secondStat, _thirdStat], CanvasService.GetWeaponStatInfo);
                CanvasService.GetAndUpdateWeaponStatBuffer(Core.LocalCharacter);
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
