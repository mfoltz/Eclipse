using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using ProjectM.UI;
using Eclipse.Elements.States;
using Eclipse.Services;

namespace Eclipse.Elements;

internal class Leveling : IReactiveElement
{
    readonly LevelingState _state;
    GameObject _barGameObject;
    GameObject _informationPanel;
    LocalizedText _header;
    LocalizedText _text;
    LocalizedText _firstText;
    LocalizedText _classText;
    LocalizedText _secondText;
    Image _fill;

    public Leveling(LevelingState state)
    {
        _state = state;
    }

    public void Awake()
    {
        if (CanvasService.LevelingEnabled)
        {
            CanvasService.ConfigureHorizontalProgressBar(ref _barGameObject, ref _informationPanel,
                ref _fill, ref _text, ref _header, CanvasService.Element.Leveling, Color.green,
                ref _firstText, ref _classText, ref _secondText);
        }
    }

    public IEnumerator OnUpdate()
    {
        while (true)
        {
            if (CanvasService.LevelingEnabled)
            {
                CanvasService.UpdateBar(_state.Progress, _state.Level,
                    _state.MaxLevel, _state.Prestige,
                    _text, _header, _fill, CanvasService.Element.Leveling);
                CanvasService.UpdateClass(_state.Class, _classText);
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
