using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using ProjectM.UI;
using Eclipse.Elements.States;
using Eclipse.Services;

namespace Eclipse.Elements;

internal class Legacies : IReactiveElement
{
    readonly LegacyState _state;
    GameObject _barGameObject;
    GameObject _informationPanel;
    LocalizedText _firstStat;
    LocalizedText _secondStat;
    LocalizedText _thirdStat;
    LocalizedText _header;
    LocalizedText _text;
    Image _fill;

    public Legacies(LegacyState state)
    {
        _state = state;
    }

    public void Awake()
    {
        if (CanvasService.LegaciesEnabled)
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
            if (CanvasService.LegaciesEnabled)
            {
                CanvasService.UpdateBar(_state.Progress, _state.Level,
                    _state.MaxLevel, _state.Prestige,
                    _text, _header, _fill, CanvasService.Element.Legacy, _state.LegacyType);
                CanvasService.UpdateBloodStats(_state.BonusStats,
                    [_firstStat, _secondStat, _thirdStat], CanvasService.GetBloodStatInfo);
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
