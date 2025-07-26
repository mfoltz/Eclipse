using Eclipse.Services;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace Eclipse.Elements;

internal class Quests : IReactiveElement
{
    GameObject _dailyQuestObject;
    LocalizedText _dailyQuestHeader;
    LocalizedText _dailyQuestSubHeader;
    Image _dailyQuestIcon;

    GameObject _weeklyQuestObject;
    LocalizedText _weeklyQuestHeader;
    LocalizedText _weeklyQuestSubHeader;
    Image _weeklyQuestIcon;

    public void Awake()
    {
        if (CanvasService.QuestsEnabled)
        {
            CanvasService.ConfigureQuestWindow(ref _dailyQuestObject, CanvasService.Element.Daily, Color.green,
                ref _dailyQuestHeader, ref _dailyQuestSubHeader, ref _dailyQuestIcon);
            CanvasService.ConfigureQuestWindow(ref _weeklyQuestObject, CanvasService.Element.Weekly, Color.magenta,
                ref _weeklyQuestHeader, ref _weeklyQuestSubHeader, ref _weeklyQuestIcon);
        }
    }

    public IEnumerator OnUpdate()
    {
        while (true)
        {
            if (CanvasService.QuestsEnabled)
            {
                CanvasService.UpdateQuests(_dailyQuestObject, _dailyQuestSubHeader, _dailyQuestIcon,
                    CanvasService._dailyTargetType, CanvasService._dailyTarget,
                    CanvasService._dailyProgress, CanvasService._dailyGoal, CanvasService._dailyVBlood);
                CanvasService.UpdateQuests(_weeklyQuestObject, _weeklyQuestSubHeader, _weeklyQuestIcon,
                    CanvasService._weeklyTargetType, CanvasService._weeklyTarget,
                    CanvasService._weeklyProgress, CanvasService._weeklyGoal, CanvasService._weeklyVBlood);
            }

            yield return CanvasService.Delay;
        }
    }
    public void Toggle()
    {
        bool active = !_dailyQuestObject.activeSelf;

        _dailyQuestObject.SetActive(active);
        _weeklyQuestObject.SetActive(active);

        CanvasService.SetElementState(_dailyQuestObject, active);
        CanvasService.SetElementState(_weeklyQuestObject, active);
    }
}
