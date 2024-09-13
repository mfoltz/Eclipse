using Il2CppInterop.Runtime;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using ProjectM.UI;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using static Eclipse.Services.CanvasService.GameObjectUtilities;
using static Eclipse.Services.DataService;
using Image = UnityEngine.UI.Image;
using StringComparison = System.StringComparison;

// UI ideas
// how to summon familiars, choose class spells, etc with a menu on the client? could just have the various UI elements correspond to respective familiar lists, class spells, etc.
// like if menu 1 has 4 buttons they choose the spell to request from the server if the server detects a change in a certain component which the client would do when they click a button
// display class somewhere in UI

namespace Eclipse.Services;
internal class CanvasService
{
    static readonly bool ExperienceBar = Plugin.Leveling;
    //static readonly bool ShowPrestige = Plugin.Prestige;
    static readonly bool LegacyBar = Plugin.Legacies;
    static readonly bool ExpertiseBar = Plugin.Expertise;
    static readonly bool QuestTracker = Plugin.Quests;
    public enum UIElement
    {
        Experience,
        Legacy,
        Expertise,
        QuestTracker
    }

    static readonly Dictionary<int, string> RomanNumerals = new()
    {
        {100, "C"}, {90, "XC"}, {50, "L"}, {40, "XL"},
        {10, "X"}, {9, "IX"}, {5, "V"}, {4, "IV"},
        {1, "I"}
    };

    static readonly WaitForSeconds Delay = new(1f); // won't ever update faster than 2.5s intervals since that's roughly how often the server sends updates which I find acceptable for now

    // object references for UI elements
    static UICanvasBase UICanvasBase;
    static Canvas Canvas;

    static GameObject ExperienceBarGameObject;
    static GameObject ExperienceInformationPanel;
    static LocalizedText ExperienceHeader;
    static LocalizedText ExperienceText;
    static LocalizedText ExperienceFirstText;
    static LocalizedText ExperienceClassText;
    static LocalizedText ExperienceSecondText;
    static Image ExperienceFill;
    public static float ExperienceProgress = 0f;
    public static int ExperienceLevel = 0;
    public static int ExperiencePrestige = 0;
    public static int ExperienceMaxLevel = 90;
    public static PlayerClass ClassType = PlayerClass.None;

    static GameObject LegacyBarGameObject;
    static GameObject LegacyInformationPanel;
    static LocalizedText FirstLegacyStat;
    static LocalizedText SecondLegacyStat;
    static LocalizedText ThirdLegacyStat;
    static LocalizedText LegacyHeader;
    static LocalizedText LegacyText;
    static Image LegacyFill;
    public static string LegacyType;
    public static float LegacyProgress = 0f;
    public static int LegacyLevel = 0;
    public static int LegacyPrestige = 0;
    public static int LegacyMaxLevel = 100;
    public static List<string> LegacyBonusStats = ["","",""];

    static GameObject ExpertiseBarGameObject;
    static GameObject ExpertiseInformationPanel;
    static LocalizedText FirstExpertiseStat;
    static LocalizedText SecondExpertiseStat;
    static LocalizedText ThirdExpertiseStat;
    static LocalizedText ExpertiseHeader;
    static LocalizedText ExpertiseText;
    static Image ExpertiseFill;
    public static string ExpertiseType;
    public static float ExpertiseProgress = 0f;
    public static int ExpertiseLevel = 0;
    public static int ExpertisePrestige = 0;
    public static int ExpertiseMaxLevel = 100;
    public static List<string> ExpertiseBonusStats = ["","",""];

    static GameObject DailyQuestObject;
    static LocalizedText DailyQuestHeader;
    static LocalizedText DailyQuestSubHeader;
    public static int DailyProgress = 0;
    public static int DailyGoal = 0;
    public static string DailyTarget = "";
    public static int DailyLevel = 0;

    static GameObject WeeklyQuestObject;
    static LocalizedText WeeklyQuestHeader;
    static LocalizedText WeeklyQuestSubHeader;
    public static int WeeklyProgress = 0;
    public static int WeeklyGoal = 0;
    public static string WeeklyTarget = "";
    public static int WeeklyLevel = 0;

    static readonly float ScreenWidth = Screen.width;
    static readonly float ScreenHeight = Screen.height;

    static int Layer;
    static int BarNumber;
    static float WindowOffset;

    const float BarHeightSpacing = 0.075f;
    const float BarWidthSpacing = 0.075f;

    public static readonly List<GameObject> ActiveObjects = [];

    public static bool Active = false;
    public static bool KillSwitch = false;
    public CanvasService(UICanvasBase canvas)
    {
        UICanvasBase = canvas;
        Canvas = GameObject.Find("HUDCanvas(Clone)/BottomBarCanvas").GetComponent<Canvas>();

        Layer = Canvas.gameObject.layer;
        BarNumber = 0;
        WindowOffset = 0f;

        InitializeBloodButton();
        InitializeUI();
    }
    static void InitializeUI()
    {
        if (ExperienceBar) ConfigureProgressBar(ref ExperienceBarGameObject, ref ExperienceInformationPanel, ref ExperienceFill, ref ExperienceText, ref ExperienceHeader, UIElement.Experience, Color.green, ref ExperienceFirstText, ref ExperienceClassText, ref ExperienceSecondText);

        if (LegacyBar) ConfigureProgressBar(ref LegacyBarGameObject, ref LegacyInformationPanel, ref LegacyFill, ref LegacyText, ref LegacyHeader, UIElement.Legacy, Color.red, ref FirstLegacyStat, ref SecondLegacyStat, ref ThirdLegacyStat);

        if (ExpertiseBar) ConfigureProgressBar(ref ExpertiseBarGameObject, ref ExpertiseInformationPanel, ref ExpertiseFill, ref ExpertiseText, ref ExpertiseHeader, UIElement.Expertise, Color.grey, ref FirstExpertiseStat, ref SecondExpertiseStat, ref ThirdExpertiseStat);

        if (QuestTracker)
        {
            ConfigureQuestWindow(ref DailyQuestObject, "Daily Quest", Color.green, ref DailyQuestHeader, ref DailyQuestSubHeader);
            ConfigureQuestWindow(ref WeeklyQuestObject, "Weekly Quest", Color.magenta, ref WeeklyQuestHeader, ref WeeklyQuestSubHeader);
        }
    }
    static void InitializeBloodButton()
    {
        // Find blood (the one with raycasting) to add button for UI toggling
        GameObject bloodObject = GameObject.Find("HUDCanvas(Clone)/BottomBarCanvas/BottomBar(Clone)/Content/BloodOrbParent/BloodOrb/BlackBackground/Blood");

        // Add button
        SimpleStunButton stunButton = bloodObject.AddComponent<SimpleStunButton>();
        stunButton.onClick.AddListener(new Action(ToggleUIObjects));
    }
    public static IEnumerator CanvasUpdateLoop()
    {
        while (true)
        {
            if (KillSwitch) // stop running if player leaves game
            {
                Active = false;
                break;
            }
            else if (!Active) // don't update if not active from blood orb click
            {
                yield return Delay;
                continue;
            }

            if (ExperienceBar)
            {
                UpdateBar(ExperienceProgress, ExperienceLevel, ExperienceMaxLevel, ExperiencePrestige, ExperienceText, ExperienceHeader, ExperienceFill, UIElement.Experience);
                UpdateClass(ClassType, ExperienceClassText);
            }

            if (LegacyBar)
            {
                UpdateBar(LegacyProgress, LegacyLevel, LegacyMaxLevel, LegacyPrestige, LegacyText, LegacyHeader, LegacyFill, UIElement.Legacy, LegacyType);
                UpdateStats(LegacyBonusStats, [FirstLegacyStat, SecondLegacyStat, ThirdLegacyStat], BloodStatStringAbbreviations, GetBloodStatInfo);
            }

            if (ExpertiseBar)
            {
                UpdateBar(ExpertiseProgress, ExpertiseLevel, ExpertiseMaxLevel, ExpertisePrestige, ExpertiseText, ExpertiseHeader, ExpertiseFill, UIElement.Expertise, ExpertiseType);
                UpdateStats(ExpertiseBonusStats, [FirstExpertiseStat, SecondExpertiseStat, ThirdExpertiseStat], WeaponStatStringAbbreviations, GetWeaponStatInfo);
            }

            if (QuestTracker)
            {
                UpdateQuests(DailyQuestObject, DailyQuestSubHeader, DailyTarget, DailyProgress, DailyGoal, DailyLevel);
                UpdateQuests(WeeklyQuestObject, WeeklyQuestSubHeader, WeeklyTarget, WeeklyProgress, WeeklyGoal, WeeklyLevel);
            }

            yield return Delay;
        }
    }
    static void UpdateBar(float progress, int level, int maxLevel, int prestiges, LocalizedText levelText, LocalizedText barHeader, Image fill, UIElement element, string type = "")
    {
        string levelString = level.ToString();
        if (type == "Frailed")
        {
            levelString = "N/A";
        }

        if (level == maxLevel)
        {
            fill.fillAmount = 1f;
        }
        else
        {
            fill.fillAmount = progress;
        }

        if (levelText.GetText() != levelString)
        {
            levelText.ForceSet(levelString);
        }

        if (prestiges != 0)
        {
            string header = "";

            if (element.Equals(UIElement.Experience))
            {
                header = $"{element} {IntegerToRoman(prestiges)}";
            }
            else if (element.Equals(UIElement.Legacy))
            {
                header = $"{type} {IntegerToRoman(prestiges)}";
            }
            else if (element.Equals(UIElement.Expertise))
            {
                header = $"{type} {IntegerToRoman(prestiges)}";
            }

            barHeader.ForceSet(header);
        }
        else if (!string.IsNullOrEmpty(type))
        {
            if (barHeader.GetText() != type)
            {
                barHeader.ForceSet(type);
            }
        }
    }
    static void UpdateClass(PlayerClass classType, LocalizedText classText)
    {
        if (classType != PlayerClass.None)
        {
            if (!classText.enabled) classText.enabled = true;
            classText.ForceSet(classType.ToString());
        }
        else
        {
            classText.ForceSet("");
            classText.enabled = false;
        }
    }
    static void UpdateStats(List<string> bonusStats, List<LocalizedText> statTexts, Dictionary<string, string> abbreviations, Func<string, string> getStatInfo)
    {
        for (int i = 0; i < bonusStats.Count; i++)
        {
            if (bonusStats[i] != "None" && !statTexts[i].GetText().Contains(abbreviations[bonusStats[i]]))
            {
                if (!statTexts[i].enabled) statTexts[i].enabled = true;
                string statInfo = getStatInfo(bonusStats[i]);
                statTexts[i].ForceSet(statInfo);
            }
            else if (bonusStats[i] == "None" && statTexts[i].enabled)
            {
                statTexts[i].ForceSet("");
                statTexts[i].enabled = false;
            }
        }
    }
    static void UpdateQuests(GameObject questObject, LocalizedText questSubHeader, string target, int progress, int goal, int level)
    {
        if (progress != goal)
        {
            if (!questObject.gameObject.active) questObject.gameObject.active = true;
            questSubHeader.ForceSet($"<color=white>Lv{level} {target}</color>: {progress}/<color=yellow>{goal}</color>");
        }
        else
        {
            questObject.gameObject.active = false;
        }
    }
    static string GetWeaponStatInfo(string statType)
    {
        if (Enum.TryParse(statType, out WeaponStatType weaponStat))
        {
            if (WeaponStatValues.TryGetValue(weaponStat, out float statValue))
            {
                float classMultiplier = ClassSynergy(weaponStat, ClassType, ClassStatSynergies);
                statValue *= ((1 + (PrestigeStatMultiplier * ExpertisePrestige)) * classMultiplier * ((float)ExpertiseLevel / ExpertiseMaxLevel));
                return FormatWeaponStat(weaponStat, statValue);
            }
        }
        return "";
    }
    static string GetBloodStatInfo(string statType)
    {
        if (Enum.TryParse(statType, out BloodStatType bloodStat))
        {
            if (BloodStatValues.TryGetValue(bloodStat, out float statValue))
            {
                float classMultiplier = ClassSynergy(bloodStat, ClassType, ClassStatSynergies);
                statValue *= ((1 + (PrestigeStatMultiplier * LegacyPrestige)) * classMultiplier * ((float)LegacyLevel / LegacyMaxLevel));

                string displayString = $"<color=#00FFFF>{BloodStatTypeAbbreviations[bloodStat]}</color>: <color=#90EE90>{statValue.ToString("F0") + "%"}</color>";
                return displayString;
            }
        }
        return "";
    }
    static void ConfigureQuestWindow(ref GameObject questObject, string questType, Color headerColor, ref LocalizedText header, ref LocalizedText subHeader)
    {
        // Instantiate quest tooltip
        questObject = GameObject.Instantiate(UICanvasBase.BottomBarParentPrefab.FakeTooltip.gameObject);
        RectTransform questTransform = questObject.GetComponent<RectTransform>();

        // Prevent quest window from being destroyed on scene load and move to scene
        GameObject.DontDestroyOnLoad(questObject);
        SceneManager.MoveGameObjectToScene(questObject, SceneManager.GetSceneByName("VRisingWorld"));

        // Set parent and activate quest window
        questTransform.SetParent(Canvas.transform, false);
        questTransform.gameObject.layer = Layer;
        questObject.SetActive(true);

        // Deactivate unwanted objects in quest tooltips
        GameObject entries = FindTargetUIObject(questObject.transform, "InformationEntries");
        DeactivateChildrenExceptNamed(entries.transform, "TooltipHeader");

        // Activate TooltipHeader
        GameObject tooltipHeader = FindTargetUIObject(questObject.transform, "TooltipHeader");
        tooltipHeader.SetActive(true);

        // Activate Icon&Name container
        GameObject iconNameObject = FindTargetUIObject(tooltipHeader.transform, "Icon&Name");
        iconNameObject.SetActive(true);

        // Deactivate LevelFrames and ReforgeCosts
        GameObject levelFrame = FindTargetUIObject(iconNameObject.transform, "LevelFrame");
        levelFrame.SetActive(false);
        GameObject reforgeCost = FindTargetUIObject(questObject.transform, "Tooltip_ReforgeCost");
        reforgeCost.SetActive(false);

        // Deactivate TooltipIcon
        GameObject tooltipIcon = FindTargetUIObject(tooltipHeader.transform, "TooltipIcon");
        tooltipIcon.SetActive(false);

        // Set LocalizedText for QuestHeaders
        GameObject subHeaderObject = FindTargetUIObject(iconNameObject.transform, "TooltipSubHeader");
        header = FindTargetUIObject(iconNameObject.transform, "TooltipHeader").GetComponent<LocalizedText>();
        header.Text.fontSize *= 2f;
        header.Text.color = headerColor;
        subHeader = subHeaderObject.GetComponent<LocalizedText>();
        subHeader.Text.enableAutoSizing = false;
        subHeader.Text.autoSizeTextContainer = false;
        subHeader.Text.enableWordWrapping = false;

        // Configure the subheader's content size fitter
        ContentSizeFitter subHeaderFitter = subHeaderObject.GetComponent<ContentSizeFitter>();
        subHeaderFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        subHeaderFitter.verticalFit = ContentSizeFitter.FitMode.Unconstrained;

        // Size window and set anchors
        questTransform.sizeDelta = new Vector2(questTransform.sizeDelta.x * 0.6f, questTransform.sizeDelta.y);
        questTransform.anchorMin = new Vector2(1, WindowOffset); // Anchored to bottom-right
        questTransform.anchorMax = new Vector2(1, WindowOffset);
        questTransform.pivot = new Vector2(1, WindowOffset);
        questTransform.anchoredPosition = new Vector2(0, WindowOffset);

        // Set header text
        header.ForceSet(questType);
        subHeader.ForceSet("UnitName: 0/0"); // For testing, can be updated later

        // Add to active objects
        ActiveObjects.Add(questObject);
        WindowOffset += 0.075f;
    }
    static void ConfigureProgressBar(ref GameObject barGameObject, ref GameObject informationPanelObject, ref Image fill, ref LocalizedText level, ref LocalizedText header, UIElement element, Color fillColor, ref LocalizedText firstText, ref LocalizedText secondText, ref LocalizedText thirdText)
    {
        // Instantiate the bar object from the prefab
        barGameObject = GameObject.Instantiate(UICanvasBase.TargetInfoParent.gameObject);

        // DontDestroyOnLoad, change scene
        GameObject.DontDestroyOnLoad(barGameObject);
        SceneManager.MoveGameObjectToScene(barGameObject, SceneManager.GetSceneByName("VRisingWorld"));

        RectTransform barRectTransform = barGameObject.GetComponent<RectTransform>();
        barRectTransform.SetParent(Canvas.transform, false);
        barRectTransform.gameObject.layer = Layer;

        // Set anchor and pivot to middle-upper-right
        barRectTransform.anchorMin = new Vector2(1, 0.6f); // Middle-upper-right anchor
        barRectTransform.anchorMax = new Vector2(1, 0.6f);
        barRectTransform.pivot = new Vector2(1, 0.6f); // Middle-upper-right pivot

        // Adjust the position so the bar is pushed into the screen by a certain amount
        float padding = ScreenHeight * BarHeightSpacing;
        float offsetY = (barRectTransform.rect.height + padding) * BarNumber; // Spacing between bars
        float spacing = ScreenWidth * BarWidthSpacing;
        barRectTransform.anchoredPosition = new Vector2(-spacing, -offsetY); // width of bar units from the right, adjust Y for each bar

        // Best scale found so far for different resolutions
        barRectTransform.localScale = new Vector3(0.7f, 0.7f, 1f);

        // Assign fill, header, and level text components
        fill = FindTargetUIObject(barRectTransform.transform, "Fill").GetComponent<Image>();
        level = FindTargetUIObject(barRectTransform.transform, "LevelText").GetComponent<LocalizedText>();
        header = FindTargetUIObject(barRectTransform.transform, "Name").GetComponent<LocalizedText>();

        // Set initial values
        fill.fillAmount = 0f;
        fill.color = fillColor;
        level.ForceSet("0");
        header.ForceSet(element.ToString());
        header.Text.fontSize *= 1.5f;

        // Set these to 0 so don't appear, deactivating instead seemed funky
        FindTargetUIObject(barRectTransform.transform, "DamageTakenFill").GetComponent<Image>().fillAmount = 0f;
        FindTargetUIObject(barRectTransform.transform, "AbsorbFill").GetComponent<Image>().fillAmount = 0f;

        // Configure informationPanels
        informationPanelObject = FindTargetUIObject(barRectTransform.transform, "InformationPanel");
        ConfigureInformationPanel(ref informationPanelObject, ref firstText, ref secondText, ref thirdText, element);

        // Increment for spacing
        BarNumber++;
        barGameObject.SetActive(true);
        ActiveObjects.Add(barGameObject);
    }
    static void ConfigureInformationPanel(ref GameObject informationPanelObject, ref LocalizedText firstText, ref LocalizedText secondText, ref LocalizedText thirdText, UIElement element)
    {
        switch (element)
        {
            case UIElement.Experience:
                ConfigureExperiencePanel(ref informationPanelObject, ref firstText, ref secondText, ref thirdText);
                break;
            default:
                ConfigureDefaultPanel(ref informationPanelObject, ref firstText, ref secondText, ref thirdText);
                break;
        }
    }
    static void ConfigureExperiencePanel(ref GameObject panel, ref LocalizedText firstText, ref LocalizedText secondText, ref LocalizedText thirdText)
    {
        secondText = FindTargetUIObject(panel.transform, "ProffesionInfo").GetComponent<LocalizedText>();
        secondText.Text.fontSize *= 1.2f;
        secondText.ForceSet("");
        secondText.enabled = false;

        firstText = FindTargetUIObject(panel.transform, "BloodInfo").GetComponent<LocalizedText>();
        firstText.ForceSet("");
        firstText.enabled = false;

        thirdText = FindTargetUIObject(panel.transform, "PlatformUserName").GetComponent<LocalizedText>();
        thirdText.ForceSet("");
        thirdText.enabled = false;
    }
    static void ConfigureDefaultPanel(ref GameObject panel, ref LocalizedText firstText, ref LocalizedText secondText, ref LocalizedText thirdText)
    {
        firstText = FindTargetUIObject(panel.transform, "BloodInfo").GetComponent<LocalizedText>();
        firstText.Text.fontSize *= 1.1f;
        firstText.ForceSet("");
        firstText.enabled = false;

        secondText = FindTargetUIObject(panel.transform, "ProffesionInfo").GetComponent<LocalizedText>();
        secondText.Text.fontSize *= 1.1f;
        secondText.ForceSet("");
        secondText.enabled = false;

        thirdText = FindTargetUIObject(panel.transform, "PlatformUserName").GetComponent<LocalizedText>();
        thirdText.Text.fontSize *= 1.1f;
        thirdText.ForceSet("");
        thirdText.enabled = false;
    }
    static float ClassSynergy<T>(T statType, PlayerClass classType, Dictionary<PlayerClass, (List<WeaponStatType> WeaponStatTypes, List<BloodStatType> BloodStatTypes)> classStatSynergy)
    {
        if (classType.Equals(PlayerClass.None))
            return 1f;

        // Check if the stat type exists in the class synergies for the current class
        if (typeof(T) == typeof(WeaponStatType) && classStatSynergy[classType].WeaponStatTypes.Contains((WeaponStatType)(object)statType))
        {
            return ClassStatMultiplier;
        }
        else if (typeof(T) == typeof(BloodStatType) && classStatSynergy[classType].BloodStatTypes.Contains((BloodStatType)(object)statType))
        {
            return ClassStatMultiplier;
        }

        return 1f; // Return default multiplier if stat is not present in the class synergy
    }
    static string FormatWeaponStat(WeaponStatType weaponStat, float statValue)
    {
        string statValueString = WeaponStatFormats[weaponStat] switch
        {
            "integer" => ((int)statValue).ToString(),
            "decimal" => statValue.ToString("F2"),
            "percentage" => (statValue * 100f).ToString("F0") + "%",
            _ => statValue.ToString(),
        };

        string displayString = $"<color=#00FFFF>{WeaponStatTypeAbbreviations[weaponStat]}</color>: <color=#90EE90>{statValueString}</color>";
        return displayString;
    }
    static string IntegerToRoman(int num)
    {
        string result = string.Empty;

        foreach (var item in RomanNumerals)
        {
            while (num >= item.Key)
            {
                result += item.Value;
                num -= item.Key;
            }
        }

        return result;
    }
    static void ToggleUIObjects()
    {
        Active = !Active;
        foreach (GameObject gameObject in ActiveObjects)
        {
            gameObject.active = Active;
        }
    }
    public static class GameObjectUtilities
    {
        public static GameObject FindTargetUIObject(Transform root, string targetName)
        {
            // Stack to hold the transforms to be processed
            Stack<(Transform transform, int indentLevel)> transformStack = new();
            transformStack.Push((root, 0));

            // HashSet to keep track of visited transforms to avoid cyclic references
            HashSet<Transform> visited = [];

            Il2CppArrayBase<Transform> children = root.GetComponentsInChildren<Transform>(true);

            List<Transform> transforms = [.. children];

            while (transformStack.Count > 0)
            {
                var (current, indentLevel) = transformStack.Pop();

                if (!visited.Add(current))
                {
                    // If we have already visited this transform, skip it
                    continue;
                }

                if (current.gameObject.name.Equals(targetName, StringComparison.OrdinalIgnoreCase))
                {
                    // Return the transform if the name matches
                    return current.gameObject;
                }

                // Create an indentation string based on the indent level
                //string indent = new('|', indentLevel);

                // Print the current GameObject's name and some basic info
                //Core.Log.LogInfo($"{indent}{current.gameObject.name} ({current.gameObject.scene.name})");

                // Add all children to the stack
                foreach (Transform child in transforms)
                {
                    if (child.parent == current)
                    {
                        transformStack.Push((child, indentLevel + 1));
                    }
                }
            }
            return null;
        }
        public static void FindLoadedObjects<T>() where T : UnityEngine.Object
        {
            Il2CppReferenceArray<UnityEngine.Object> resources = Resources.FindObjectsOfTypeAll(Il2CppType.Of<T>());
            Core.Log.LogInfo($"Found {resources.Length} {Il2CppType.Of<T>().FullName}'s!");
            foreach (UnityEngine.Object resource in resources)
            {
                Core.Log.LogInfo($"Sprite: {resource.name}");
            }
        }
        public static void DeactivateChildrenExceptNamed(Transform root, string targetName)
        {
            // Stack to hold the transforms to be processed
            Stack<(Transform transform, int indentLevel)> transformStack = new();
            transformStack.Push((root, 0));

            // HashSet to keep track of visited transforms to avoid cyclic references
            HashSet<Transform> visited = [];

            Il2CppArrayBase<Transform> children = root.GetComponentsInChildren<Transform>();
            List<Transform> transforms = [.. children];

            while (transformStack.Count > 0)
            {
                var (current, indentLevel) = transformStack.Pop();

                if (!visited.Add(current))
                {
                    // If we have already visited this transform, skip it
                    continue;
                }

                // Add all children to the stack
                foreach (Transform child in transforms)
                {
                    if (child.parent == current)
                    {
                        transformStack.Push((child, indentLevel + 1));
                    }

                    if (!child.name.Equals(targetName)) child.gameObject.SetActive(false);
                }
            }
        }
        public static void FindGameObjects(Transform root, string filePath = "", bool includeInactive = false)
        {
            // Stack to hold the transforms to be processed
            Stack<(Transform transform, int indentLevel)> transformStack = new();
            transformStack.Push((root, 0));

            // HashSet to keep track of visited transforms to avoid cyclic references
            HashSet<Transform> visited = [];

            Il2CppArrayBase<Transform> children = root.GetComponentsInChildren<Transform>(includeInactive);
            List<Transform> transforms = [..children];

            if (string.IsNullOrEmpty(filePath))
            {
                while (transformStack.Count > 0)
                {
                    var (current, indentLevel) = transformStack.Pop();

                    if (!visited.Add(current))
                    {
                        // If we have already visited this transform, skip it
                        continue;
                    }

                    List<string> objectComponents = FindGameObjectComponents(current.gameObject);

                    // Create an indentation string based on the indent level
                    string indent = new('|', indentLevel);

                    // Write the current GameObject's name and some basic info to the file

                    // Add all children to the stack
                    foreach (Transform child in transforms)
                    {
                        if (child.parent == current)
                        {
                            transformStack.Push((child, indentLevel + 1));
                        }
                    }
                }
                return;
            }

            if (!File.Exists(filePath)) File.Create(filePath).Dispose();

            using StreamWriter writer = new(filePath, false);
            while (transformStack.Count > 0)
            {
                var (current, indentLevel) = transformStack.Pop();

                if (!visited.Add(current))
                {
                    // If we have already visited this transform, skip it
                    continue;
                }

                List<string> objectComponents = FindGameObjectComponents(current.gameObject);

                // Create an indentation string based on the indent level
                string indent = new('|', indentLevel);

                // Write the current GameObject's name and some basic info to the file
                writer.WriteLine($"{indent}{current.gameObject.name} | {string.Join(",", objectComponents)} | [{current.gameObject.scene.name}]");

                // Add all children to the stack
                foreach (Transform child in transforms)
                {
                    if (child.parent == current)
                    {
                        transformStack.Push((child, indentLevel + 1));
                    }
                }
            }
        }
        public static List<string> FindGameObjectComponents(GameObject parentObject)
        {
            List<string> components = [];

            int componentCount = parentObject.GetComponentCount();
            for (int i = 0; i < componentCount; i++)
            {
                components.Add($"{parentObject.GetComponentAtIndex(i).GetIl2CppType().FullName}({i})");
            }

            return components;
        }
    }
}
/*

//static GameObject DropdownMenu;
//static TMP_Dropdown DropdownSelection;
//static List<string> Selections = ["1","2","3"];
//static LocalizedText DropdownText;

static void OnDropDownChanged(int optionIndex)
{
    Core.Log.LogInfo($"Selected {Selections[optionIndex]}");
}

if (Familiars) // drop down menu testing
{
    try
    {
        Core.Log.LogInfo("Creating dropdown menu...");
        // might need to use own canvas for this if BottomBarParent throws a fit which it probably will
        GameObject dropdownMenuObject = new("DropdownMenu");
        DropdownMenu = dropdownMenuObject;
        RectTransform dropdownRectTransform = dropdownMenuObject.AddComponent<RectTransform>();

        Core.Log.LogInfo("Making persistent and moving to scene before setting parent...");
        // DontDestroyOnLoad, move to proper scene, set canvas as parent
        GameObject.DontDestroyOnLoad(dropdownMenuObject);
        SceneManager.MoveGameObjectToScene(dropdownMenuObject, SceneManager.GetSceneByName("VRisingWorld"));
        dropdownRectTransform.SetParent(bottomBarCanvas.transform, false);

        Core.Log.LogInfo("Adding Dropdown component...");
        // Add dropdown components and configure position, starting state, etc
        TMP_Dropdown dropdownMenu = dropdownMenuObject.AddComponent<TMP_Dropdown>();
        DropdownSelection = dropdownMenu;

        Core.Log.LogInfo("Setting dropdown position/anchors...");

        // set anchors/pivot

        //dropdownRectTransform.anchorMin = new Vector2(1, 0.2f); // Anchored to bottom-right
        //dropdownRectTransform.anchorMax = new Vector2(1, 0.2f);
        //dropdownRectTransform.pivot = new Vector2(1, 0.2f);
        //dropdownRectTransform.anchoredPosition = new Vector2(0f, 0.2f); // Anchored to the bottom right corner above quest windows
        //dropdownRectTransform.sizeDelta = new Vector2(80f, 30f);        


        dropdownRectTransform.anchorMin = new Vector2(0.5f, 0.5f);  // Centered
        dropdownRectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        dropdownRectTransform.pivot = new Vector2(0.5f, 0.5f);
        dropdownRectTransform.anchoredPosition = Vector2.zero;  // Centered on screen
        dropdownRectTransform.sizeDelta = new Vector2(160f, 30f);  // Test with a larger size

        // captionText
        GameObject captionObject = new("CaptionText");
        TMP_Text dropdownText = captionObject.AddComponent<TMP_Text>();
        dropdownText.text = "FamiliarBoxes";
        RectTransform captionRectTransform = captionObject.GetComponent<RectTransform>();

        // DontDestroyOnLoad, move to proper scene, set canvas as parent
        GameObject.DontDestroyOnLoad(captionObject);
        SceneManager.MoveGameObjectToScene(captionObject, SceneManager.GetSceneByName("VRisingWorld"));
        captionObject.transform.SetParent(dropdownMenu.transform, false);

        // Anchor the text to stretch across the width of the dropdown
        captionRectTransform.anchorMin = new Vector2(0, 0.5f);  // Anchored to the middle of the dropdown
        captionRectTransform.anchorMax = new Vector2(1, 0.5f);  // Stretched horizontally
        captionRectTransform.pivot = new Vector2(0.5f, 0.5f);   // Center pivot

        // Set size and position relative to dropdown
        captionRectTransform.sizeDelta = new Vector2(0, 30f);   // Matches dropdown height (30 units)
        captionRectTransform.anchoredPosition = new Vector2(0, 0);  // Centered within dropdown

        // Configure the font, font size, and other properties
        dropdownText.font = ExperienceClassText.Text.font;
        dropdownText.fontSize = (int)ExperienceClassText.Text.fontSize;
        dropdownText.color = ExperienceClassText.Text.color;
        dropdownText.alignment = TextAlignmentOptions.Center;

        // Create Dropdown Template (needed for displaying options)
        GameObject templateObject = new("Template");
        RectTransform templateRectTransform = templateObject.AddComponent<RectTransform>();
        templateObject.transform.SetParent(dropdownMenuObject.transform, false);

        // Set up the template’s size and positioning
        templateRectTransform.anchorMin = new Vector2(0, 0);
        templateRectTransform.anchorMax = new Vector2(1, 0);
        templateRectTransform.pivot = new Vector2(0.5f, 1f);
        templateRectTransform.sizeDelta = new Vector2(0, 90f);  // Size for showing options

        // Add a background to the template (optional for styling)
        Image templateBackground = templateObject.AddComponent<Image>();
        templateBackground.color = new Color(0, 0, 0, 0.5f);  // Semi-transparent background

        // Create Viewport for scrolling within the template
        GameObject viewportObject = new("Viewport");
        RectTransform viewportRectTransform = viewportObject.AddComponent<RectTransform>();
        viewportObject.transform.SetParent(templateObject.transform, false);

        viewportRectTransform.anchorMin = Vector2.zero;
        viewportRectTransform.anchorMax = Vector2.one;
        viewportRectTransform.sizeDelta = Vector2.zero;

        Mask viewportMask = viewportObject.AddComponent<Mask>();
        viewportMask.showMaskGraphic = false;  // Hide the mask graphic

        // Create Content for the options list
        GameObject contentObject = new("Content");
        RectTransform contentRectTransform = contentObject.AddComponent<RectTransform>();
        contentObject.transform.SetParent(viewportObject.transform, false);

        contentRectTransform.anchorMin = new Vector2(0, 1);
        contentRectTransform.anchorMax = new Vector2(1, 1);
        contentRectTransform.pivot = new Vector2(0.5f, 1f);
        contentRectTransform.sizeDelta = new Vector2(0, 90f);

        // Create Item (this will be duplicated for each dropdown option)
        GameObject itemObject = new("Item");
        itemObject.transform.SetParent(contentObject.transform, false);

        RectTransform itemRectTransform = itemObject.AddComponent<RectTransform>();
        itemRectTransform.anchorMin = new Vector2(0, 0.5f);
        itemRectTransform.anchorMax = new Vector2(1, 0.5f);
        itemRectTransform.sizeDelta = new Vector2(0, 25f);

        // Add Toggle to the item (this is required for each option)
        Toggle itemToggle = itemObject.AddComponent<Toggle>();
        itemToggle.isOn = false;  // Default to off

        // Add TextMeshProUGUI for the option text
        TMP_Text itemText = itemObject.AddComponent<TMP_Text>();
        //itemText.text = "Option";  // Placeholder text for the option
        itemText.font = ExperienceClassText.Text.font;
        itemText.fontSize = (int)ExperienceClassText.Text.fontSize;
        itemText.color = ExperienceClassText.Text.color;
        itemText.alignment = TextAlignmentOptions.Center;

        Image dropdownImage = dropdownMenuObject.AddComponent<Image>();
        dropdownImage.color = new Color(0, 0, 0, 0.5f);
        dropdownImage.type = Image.Type.Sliced;

        dropdownMenu.template = templateRectTransform;
        dropdownMenu.targetGraphic = dropdownImage;
        dropdownMenu.captionText = dropdownText;
        dropdownMenu.itemText = itemText;

        Core.Log.LogInfo("Setting initial dropdown options...");
        // clear defaults, set empty options
        dropdownMenu.ClearOptions();
        Il2CppSystem.Collections.Generic.List<string> selections = new(Selections.Count);
        foreach (string selection in Selections)
        {
            selections.Add(selection);
        }

        Core.Log.LogInfo("Adding dropdown options and listener...");
        dropdownMenu.AddOptions(selections);
        dropdownMenu.RefreshShownValue();
        dropdownMenu.onValueChanged.AddListener(new Action<int>(OnDropDownChanged));

        Core.Log.LogInfo("Setting layer and activating...");
        dropdownMenuObject.layer = CanvasObject.layer;
        dropdownMenuObject.SetActive(true);
    }
    catch (Exception e)
    {
        Core.Log.LogError(e);
    }
}
*/