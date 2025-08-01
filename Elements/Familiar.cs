using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using ProjectM.UI;
using Eclipse.Services;

namespace Eclipse.Elements;

internal class Familiar : IReactiveElement
{
    GameObject _barGameObject;
    GameObject _informationPanel;
    LocalizedText _maxHealth;
    LocalizedText _physicalPower;
    LocalizedText _spellPower;
    LocalizedText _header;
    LocalizedText _text;
    Image _fill;

    public void Awake()
    {
        if (CanvasService.FamiliarEnabled)
        {
            CanvasService.ConfigureHorizontalProgressBar(ref _barGameObject, ref _informationPanel,
                ref _fill, ref _text, ref _header, CanvasService.Element.Familiars, Color.yellow,
                ref _maxHealth, ref _physicalPower, ref _spellPower);
        }
    }

    public IEnumerator OnUpdate()
    {
        while (true)
        {
            if (CanvasService.FamiliarEnabled)
            {
                CanvasService.UpdateBar(CanvasService._familiarProgress, CanvasService._familiarLevel,
                    CanvasService._familiarMaxLevel, CanvasService._familiarPrestige,
                    _text, _header, _fill, CanvasService.Element.Familiars, CanvasService._familiarName);
                CanvasService.UpdateFamiliarStats(CanvasService._familiarStats,
                    [_maxHealth, _physicalPower, _spellPower]);
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
