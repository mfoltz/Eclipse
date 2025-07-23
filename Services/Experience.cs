using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using ProjectM.UI;

namespace Eclipse.Services;

internal class Experience : IReactiveElement
{
    GameObject _barGameObject;
    GameObject _informationPanel;
    LocalizedText _header;
    LocalizedText _text;
    LocalizedText _firstText;
    LocalizedText _classText;
    LocalizedText _secondText;
    Image _fill;

    public void Awake()
    {
        if (CanvasService.ExperienceEnabled)
        {
            CanvasService.ConfigureHorizontalProgressBar(ref _barGameObject, ref _informationPanel,
                ref _fill, ref _text, ref _header, CanvasService.Element.Experience, Color.green,
                ref _firstText, ref _classText, ref _secondText);
        }
    }

    public IEnumerator OnUpdate()
    {
        while (true)
        {
            if (CanvasService.ExperienceEnabled)
            {
                CanvasService.UpdateBar(CanvasService._experienceProgress, CanvasService._experienceLevel,
                    CanvasService._experienceMaxLevel, CanvasService._experiencePrestige,
                    _text, _header, _fill, CanvasService.Element.Experience);
                CanvasService.UpdateClass(CanvasService._classType, _classText);
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
