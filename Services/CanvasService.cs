using Bloodcraft.Resources;
using Eclipse.Patches;
using Eclipse.Utilities;
using Il2CppInterop.Runtime;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using ProjectM;
using ProjectM.UI;
using Stunlock.Core;
using System.Collections;
using System.Text.RegularExpressions;
using TMPro;
using Unity.Entities;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using static Eclipse.Services.DataService;
using static Eclipse.Utilities.GameObjects;
using Image = UnityEngine.UI.Image;

namespace Eclipse.Services;
internal class CanvasService
{
    static EntityManager EntityManager => Core.EntityManager;
    static SystemService SystemService => Core.SystemService;
    static ManagedDataRegistry ManagedDataRegistry => SystemService.ManagedDataSystem.ManagedDataRegistry;
    static Entity LocalCharacter => Core.LocalCharacter;

    static readonly bool _experienceBar = Plugin.Leveling;
    static readonly bool _showPrestige = Plugin.Prestige;
    static readonly bool _legacyBar = Plugin.Legacies;
    static readonly bool _expertiseBar = Plugin.Expertise;
    static readonly bool _familiarBar = Plugin.Familiars;
    static readonly bool _professionBars = Plugin.Professions;
    static readonly bool _questTracker = Plugin.Quests;
    static readonly bool _shiftSlot = Plugin.ShiftSlot;
    public enum UIElement
    {
        Experience,
        Legacy,
        Expertise,
        Familiars,
        Professions,
        Daily,
        Weekly,
        ShiftSlot
    }

    static readonly Dictionary<int, string> _romanNumerals = new()
    {
        {100, "C"}, {90, "XC"}, {50, "L"}, {40, "XL"},
        {10, "X"}, {9, "IX"}, {5, "V"}, {4, "IV"},
        {1, "I"}
    };

    static readonly List<string> _spriteNames =
    [
        "BloodIcon_Cursed",
        "BloodIcon_Small_Cursed",
        "BloodIcon_Small_Holy",
        "BloodIcon_Warrior",
        "BloodIcon_Small_Warrior",
        "Poneti_Icon_Hammer_30",
        "Poneti_Icon_Bag",
        "Poneti_Icon_Res_93",
        SHIFT_SPRITE, // still no idea why this just refuses to work like every other sprite after setting, something with the base material? idk
        "Stunlock_Icon_Item_Jewel_Collection4",
        "Stunlock_Icon_Bag_Background_Alchemy",     
        "Poneti_Icon_Alchemy_02_mortar",
        "Stunlock_Icon_Bag_Background_Jewel",       
        "Poneti_Icon_runic_tablet_12",
        "Stunlock_Icon_Bag_Background_Woodworking", 
        "Stunlock_Icon_Bag_Background_Herbs",       
        "Poneti_Icon_Herbalism_35_fellherb",        
        "Stunlock_Icon_Bag_Background_Fish",        
        "Poneti_Icon_Cooking_28_fish",              
        "Poneti_Icon_Cooking_60_oceanfish",
        "Stunlock_Icon_Bag_Background_Armor",       
        "Poneti_Icon_Tailoring_38_fiercloth",       
        "FantasyIcon_ResourceAndCraftAddon (56)",
        "Stunlock_Icon_Bag_Background_Weapon",     
        "Poneti_Icon_Sword_v2_48",                  
        "Poneti_Icon_Hammer_30",
        "Stunlock_Icon_Bag_Background_Consumable",  
        "Poneti_Icon_Quest_131",
        "FantasyIcon_Wood_Hallow",
        "Poneti_Icon_Engineering_59_mega_fishingrod",
        "Poneti_Icon_Axe_v2_04",
        "Poneti_Icon_Blacksmith_21_big_grindstone",
        "FantasyIcon_Flowers (11)",
        "FantasyIcon_MagicItem (105)",
        "Item_MagicSource_General_T05_Relic",
        "Stunlock_Icon_BloodRose",
        "Poneti_Icon_Blacksmith_24_bigrune_grindstone",
        "Item_MagicSource_General_T04_FrozenEye",
        "Stunlock_Icon_SpellPoint_Blood1",
        "Stunlock_Icon_SpellPoint_Unholy1",
        "Stunlock_Icon_SpellPoint_Frost1",
        "Stunlock_Icon_SpellPoint_Chaos1",
        "Stunlock_Icon_SpellPoint_Frost1",
        "Stunlock_Icon_SpellPoint_Storm1",
        "Stunlock_Icon_SpellPoint_Illusion1",
        "spell_level_icon"
    ];

    public const string ABILITY_ICON = "Stunlock_Icon_Ability_Spell_";
    public const string NPC_ABILITY = "Ashka_M1_64";

    static readonly Dictionary<Profession, string> _professionIcons = new()
    {
        { Profession.Enchanting, "Item_MagicSource_General_T04_FrozenEye" },
        { Profession.Alchemy, "FantasyIcon_MagicItem (105)" },
        { Profession.Harvesting, "Stunlock_Icon_BloodRose" },
        { Profession.Blacksmithing, "Poneti_Icon_Blacksmith_24_bigrune_grindstone" },
        { Profession.Tailoring, "FantasyIcon_ResourceAndCraftAddon (56)" },
        { Profession.Woodcutting, "Poneti_Icon_Axe_v2_04" },
        { Profession.Mining, "Poneti_Icon_Hammer_30" },
        { Profession.Fishing, "Poneti_Icon_Engineering_59_mega_fishingrod" }
    };
    public static IReadOnlyDictionary<string, Sprite> Sprites => _sprites;
    static readonly Dictionary<string, Sprite> _sprites = [];

    static Sprite _questKillStandardUnit;
    static Sprite _questKillVBloodUnit;

    static readonly Regex _classNameRegex = new("(?<!^)([A-Z])");
    public static readonly Regex AbilitySpellRegex = new(@"(?<=AB_).*(?=_Group)");

    static readonly Dictionary<PlayerClass, Color> _classColorHexMap = new()
    {
        { PlayerClass.ShadowBlade, new Color(0.6f, 0.1f, 0.9f) },  // ignite purple
        { PlayerClass.DemonHunter, new Color(1f, 0.8f, 0f) },        // static yellow
        { PlayerClass.BloodKnight, new Color(1f, 0f, 0f) },           // leech red
        { PlayerClass.ArcaneSorcerer, new Color(0f, 0.5f, 0.5f) },    // weaken teal
        { PlayerClass.VampireLord, new Color(0f, 1f, 1f) },           // chill cyan
        { PlayerClass.DeathMage, new Color(0f, 1f, 0f) }              // condemn green
    };

    public const string V1_3 = "1.3";

    static readonly WaitForSeconds _delay = new(1f);
    static readonly WaitForSeconds _shiftDelay = new(0.1f);

    static UICanvasBase _canvasBase;
    static Canvas _bottomBarCanvas;
    static Canvas _targetInfoPanelCanvas;
    public static string _version = string.Empty;

    static GameObject _experienceBarGameObject;
    static GameObject _experienceInformationPanel;
    static LocalizedText _experienceHeader;
    static LocalizedText _experienceText;
    static LocalizedText _experienceFirstText;
    static LocalizedText _experienceClassText;
    static LocalizedText _experienceSecondText;
    static Image _experienceFill;
    public static float _experienceProgress = 0f;
    public static int _experienceLevel = 0;
    public static int _experiencePrestige = 0;
    public static int _experienceMaxLevel = 90;
    public static PlayerClass _classType = PlayerClass.None;

    static GameObject _legacyBarGameObject;
    static GameObject _legacyInformationPanel;
    static LocalizedText _firstLegacyStat;
    static LocalizedText _secondLegacyStat;
    static LocalizedText _thirdLegacyStat;
    static LocalizedText _legacyHeader;
    static LocalizedText _legacyText;
    static Image _legacyFill;
    public static string _legacyType;
    public static float _legacyProgress = 0f;
    public static int _legacyLevel = 0;
    public static int _legacyPrestige = 0;
    public static int _legacyMaxLevel = 100;
    public static List<string> _legacyBonusStats = ["", "", ""];

    static GameObject _expertiseBarGameObject;
    static GameObject _expertiseInformationPanel;
    static LocalizedText _firstExpertiseStat;
    static LocalizedText _secondExpertiseStat;
    static LocalizedText _thirdExpertiseStat;
    static LocalizedText _expertiseHeader;
    static LocalizedText _expertiseText;
    static Image _expertiseFill;
    public static string _expertiseType;
    public static float _expertiseProgress = 0f;
    public static int _expertiseLevel = 0;
    public static int _expertisePrestige = 0;
    public static int _expertiseMaxLevel = 100;
    public static List<string> _expertiseBonusStats = ["", "", ""];

    static GameObject _familiarBarGameObject;
    static GameObject _familiarInformationPanel;
    static LocalizedText _familiarMaxHealth;
    static LocalizedText _familiarPhysicalPower;
    static LocalizedText _familiarSpellPower;
    static LocalizedText _familiarHeader;
    static LocalizedText _familiarText;
    static Image _familiarFill;
    public static float _familiarProgress = 0f;
    public static int _familiarLevel = 1;
    public static int _familiarPrestige = 0;
    public static int _familiarMaxLevel = 90;
    public static string _familiarName = "";
    public static List<string> _familiarStats = ["", "", ""];

    public static bool _equipmentBonus = false;
    const float MAX_PROFESSION_LEVEL = 100f;
    const float EQUIPMENT_BONUS = 0.1f;

    static GameObject _enchantingBarGameObject;
    static LocalizedText _enchantingLevelText;
    static Image _enchantingProgressFill;
    static Image _enchantingFill;
    public static float _enchantingProgress = 0f;
    public static int _enchantingLevel = 0;

    static GameObject _alchemyBarGameObject;
    static LocalizedText _alchemyLevelText;
    static Image _alchemyProgressFill;
    static Image _alchemyFill;
    public static float _alchemyProgress = 0f;
    public static int _alchemyLevel = 0;

    static GameObject _harvestingGameObject;
    static LocalizedText _harvestingLevelText;
    static Image _harvestingProgressFill;
    static Image _harvestingFill;
    public static float _harvestingProgress = 0f;
    public static int _harvestingLevel = 0;

    static GameObject _blacksmithingBarGameObject;
    static LocalizedText _blacksmithingLevelText;
    static Image _blacksmithingProgressFill;
    static Image _blacksmithingFill;
    public static float _blacksmithingProgress = 0f;
    public static int _blacksmithingLevel = 0;

    static GameObject _tailoringBarGameObject;
    static LocalizedText _tailoringLevelText;
    static Image _tailoringProgressFill;
    static Image _tailoringFill;
    public static float _tailoringProgress = 0f;
    public static int _tailoringLevel = 0;

    static GameObject _woodcuttingBarGameObject;
    static LocalizedText _woodcuttingLevelText;
    static Image _woodcuttingProgressFill;
    static Image _woodcuttingFill;
    public static float _woodcuttingProgress = 0f;
    public static int _woodcuttingLevel = 0;

    static GameObject _miningBarGameObject;
    static LocalizedText _miningLevelText;
    static Image _miningProgressFill;
    static Image _miningFill;
    public static float _miningProgress = 0f;
    public static int _miningLevel = 0;

    static GameObject _fishingBarGameObject;
    static LocalizedText _fishingLevelText;
    static Image _fishingProgressFill;
    static Image _fishingFill;
    public static float _fishingProgress = 0f;
    public static int _fishingLevel = 0;

    static GameObject _dailyQuestObject;
    static LocalizedText _dailyQuestHeader;
    static LocalizedText _dailyQuestSubHeader;
    public static Image _dailyQuestIcon;
    public static TargetType _dailyTargetType = TargetType.Kill;
    public static int _dailyProgress = 0;
    public static int _dailyGoal = 0;
    public static string _dailyTarget = "";
    public static bool _dailyVBlood = false;

    static GameObject _weeklyQuestObject;
    static LocalizedText _weeklyQuestHeader;
    static LocalizedText _weeklyQuestSubHeader;
    public static Image _weeklyQuestIcon;
    public static TargetType _weeklyTargetType = TargetType.Kill;
    public static int _weeklyProgress = 0;
    public static int _weeklyGoal = 0;
    public static string _weeklyTarget = "";
    public static bool _weeklyVBlood = false;

    static PrefabGUID _abilityGroupPrefabGUID;

    public static AbilityTooltipData _abilityTooltipData;
    static readonly ComponentType _abilityTooltipDataComponent = ComponentType.ReadOnly(Il2CppType.Of<AbilityTooltipData>());

    public static GameObject _abilityDummyObject;
    public static AbilityBarEntry _abilityBarEntry;
    public static AbilityBarEntry.UIState _uiState;

    public static GameObject _cooldownParentObject;
    public static TextMeshProUGUI _cooldownText;
    public static GameObject _chargeCooldownImageObject;
    public static GameObject _chargesTextObject;
    public static TextMeshProUGUI _chargesText;
    public static Image _cooldownFillImage;
    public static Image _chargeCooldownFillImage;

    static GameObject _abilityEmptyIcon;
    static GameObject _abilityIcon;

    static GameObject _keybindObject;

    public static int _shiftSpellIndex = -1;
    const float COOLDOWN_FACTOR = 8f;

    public static double _cooldownEndTime = 0;
    public static float _cooldownRemaining = 0f;
    public static float _cooldownTime = 0f;
    public static int _currentCharges = 0;
    public static int _maxCharges = 0;
    public static double _chargeUpEndTime = 0;
    public static float _chargeUpTime = 0f;
    public static float _chargeUpTimeRemaining = 0f;
    public static float _chargeCooldownTime = 0f;

    static int _layer;
    static int _barNumber;
    static int _graphBarNumber;
    static float _horizontalBarHeaderFontSize;
    static float _windowOffset;
    static readonly Color _brightGold = new(1f, 0.8f, 0f, 1f);

    const float BAR_HEIGHT_SPACING = 0.075f;
    const float BAR_WIDTH_SPACING = 0.065f;

    static readonly Dictionary<UIElement, GameObject> _gameObjects = [];
    static readonly Dictionary<GameObject, bool> _objectStates = [];
    static readonly List<GameObject> _professionObjects = [];

    static readonly Dictionary<int, Action> _actionToggles = new()
    {
        {0, ExperienceToggle},
        {1, LegacyToggle},
        {2, ExpertiseToggle},
        {3, FamiliarToggle},
        {4, ProfessionToggle},
        {5, DailyQuestToggle},
        {6, WeeklyQuestToggle},
        {7, ShiftSlotToggle}
    };

    static readonly Dictionary<UIElement, string> _abilitySlotNamePaths = new()
    {
        { UIElement.Experience, "HUDCanvas(Clone)/BottomBarCanvas/BottomBar(Clone)/Content/Background/AbilityBar/AbilityBarEntry_Primary/" },
        { UIElement.Legacy, "HUDCanvas(Clone)/BottomBarCanvas/BottomBar(Clone)/Content/Background/AbilityBar/AbilityBarEntry_WeaponSkill1/" },
        { UIElement.Expertise, "HUDCanvas(Clone)/BottomBarCanvas/BottomBar(Clone)/Content/Background/AbilityBar/AbilityBarEntry_WeaponSkill2/" },
        { UIElement.Familiars, "HUDCanvas(Clone)/BottomBarCanvas/BottomBar(Clone)/Content/Background/AbilityBar/AbilityBarEntry_Travel/" },
        { UIElement.Professions, "HUDCanvas(Clone)/BottomBarCanvas/BottomBar(Clone)/Content/Background/AbilityBar/AbilityBarEntry_Spell1/" },
        { UIElement.Weekly, "HUDCanvas(Clone)/BottomBarCanvas/BottomBar(Clone)/Content/Background/AbilityBar/AbilityBarEntry_Spell2/" },
        { UIElement.Daily, "HUDCanvas(Clone)/BottomBarCanvas/BottomBar(Clone)/Content/Background/AbilityBar/AbilityBarEntry_Ultimate/" },
    };

    static readonly Dictionary<UIElement, bool> _uiElementsConfigured = new()
    {
        { UIElement.Experience, _experienceBar },
        { UIElement.Legacy, _legacyBar },
        { UIElement.Expertise, _expertiseBar },
        { UIElement.Familiars, _familiarBar },
        { UIElement.Professions, _professionBars },
        { UIElement.Daily, _questTracker },
        { UIElement.Weekly, _questTracker },
        { UIElement.ShiftSlot, _shiftSlot }
    };

    static readonly Dictionary<UIElement, int> _uiElementIndices = new()
    {
        { UIElement.Experience, 0 },
        { UIElement.Legacy, 1 },
        { UIElement.Expertise, 2 },
        { UIElement.Familiars, 3 },
        { UIElement.Professions, 4 },
        { UIElement.Daily, 5 },
        { UIElement.Weekly, 6 },
        { UIElement.ShiftSlot, 7 }
    };

    static readonly List<EquipmentType> _equipmentTypes = 
    [
        EquipmentType.Chest,
        EquipmentType.Gloves,
        EquipmentType.Legs,
        EquipmentType.Footgear
    ];

    const int EXPERIENCE = 0;
    const int LEGACY = 1;
    const int EXPERTISE = 2;
    const int FAMILIARS = 3;
    const int PROFESSION = 4;
    const int DAILY = 5;
    const int WEEKLY = 6;
    const int SHIFT_SLOT = 7;

    const string SHIFT_SPRITE = "KeyboardGlyphs_Smaller_36";
    const string SHIFT_TEXTURE = "KeyboardGlyphs_Smaller";

    public static bool _ready = false;
    public static bool _active = false;
    public static bool _shiftActive = false;
    public static bool _killSwitch = false;

    public static Coroutine _canvasRoutine;
    public static Coroutine _shiftRoutine;

    // modifying active buff/item entity sufficient, can leave prefab alone
    static readonly PrefabGUID _statsBuff = PrefabGUIDs.SetBonus_AllLeech_T09;
    static readonly bool _statsBuffActive = _legacyBar || _expertiseBar; // in loop can check for if has a class for those stats

    static readonly Dictionary<int, ModifyUnitStatBuff_DOTS> _weaponStats = [];
    static readonly Dictionary<int, ModifyUnitStatBuff_DOTS> _bloodStats = [];
    static bool _isGamepad => InputActionSystemPatch.IsGamepad;
    public CanvasService(UICanvasBase canvas)
    {
        _canvasBase = canvas;

        // _hudCanvas = GameObject.Find("HUDCanvas(Clone)/BottomBarCanvas").GetComponent<Canvas>();
        _bottomBarCanvas = canvas.BottomBarParent.gameObject.GetComponent<Canvas>();
        _targetInfoPanelCanvas = canvas.TargetInfoPanelParent.gameObject.GetComponent<Canvas>();

        _layer = _bottomBarCanvas.gameObject.layer;
        _barNumber = 0;
        _graphBarNumber = 0;
        _windowOffset = 0f;

        FindSprites();
        InitializeBloodButton();

        /*
        try
        {
            FindGameObjects(canvas.transform.root, string.Empty, true);
        }
        catch (Exception ex)
        {
            Core.Log.LogError($"Failed to find dump gameObject hierarchy: {ex}");
        }
        */

        try
        {
            InitializeUI();
            InitializeAbilitySlotButtons();
        }
        catch (Exception ex)
        {
            Core.Log.LogError($"Failed to initialize UI elements: {ex}");
        }
    }
    static void InitializeUI()
    {
        if (_experienceBar) ConfigureHorizontalProgressBar(ref _experienceBarGameObject, ref _experienceInformationPanel, 
            ref _experienceFill, ref _experienceText, ref _experienceHeader, UIElement.Experience, Color.green, 
            ref _experienceFirstText, ref _experienceClassText, ref _experienceSecondText);

        if (_legacyBar) ConfigureHorizontalProgressBar(ref _legacyBarGameObject, ref _legacyInformationPanel, 
            ref _legacyFill, ref _legacyText, ref _legacyHeader, UIElement.Legacy, Color.red, 
            ref _firstLegacyStat, ref _secondLegacyStat, ref _thirdLegacyStat);

        if (_expertiseBar) ConfigureHorizontalProgressBar(ref _expertiseBarGameObject, ref _expertiseInformationPanel, 
            ref _expertiseFill, ref _expertiseText, ref _expertiseHeader, UIElement.Expertise, Color.grey, 
            ref _firstExpertiseStat, ref _secondExpertiseStat, ref _thirdExpertiseStat);

        if (_familiarBar) ConfigureHorizontalProgressBar(ref _familiarBarGameObject, ref _familiarInformationPanel, 
            ref _familiarFill, ref _familiarText, ref _familiarHeader, UIElement.Familiars, Color.yellow, 
            ref _familiarMaxHealth, ref _familiarPhysicalPower, ref _familiarSpellPower);

        if (_questTracker)
        {
            ConfigureQuestWindow(ref _dailyQuestObject, UIElement.Daily, Color.green, ref _dailyQuestHeader, ref _dailyQuestSubHeader, ref _dailyQuestIcon);
            ConfigureQuestWindow(ref _weeklyQuestObject, UIElement.Weekly, Color.magenta, ref _weeklyQuestHeader, ref _weeklyQuestSubHeader, ref _weeklyQuestIcon);
        }

        if (_professionBars)
        {
            ConfigureVerticalProgressBar(ref _alchemyBarGameObject, ref _alchemyProgressFill, ref _alchemyFill, ref _alchemyLevelText, Profession.Alchemy);
            ConfigureVerticalProgressBar(ref _blacksmithingBarGameObject, ref _blacksmithingProgressFill, ref _blacksmithingFill, ref _blacksmithingLevelText, Profession.Blacksmithing);
            ConfigureVerticalProgressBar(ref _enchantingBarGameObject, ref _enchantingProgressFill, ref _enchantingFill, ref _enchantingLevelText, Profession.Enchanting);
            ConfigureVerticalProgressBar(ref _tailoringBarGameObject, ref _tailoringProgressFill, ref _tailoringFill, ref _tailoringLevelText, Profession.Tailoring);
            ConfigureVerticalProgressBar(ref _fishingBarGameObject, ref _fishingProgressFill, ref _fishingFill, ref _fishingLevelText, Profession.Fishing);
            ConfigureVerticalProgressBar(ref _harvestingGameObject, ref _harvestingProgressFill, ref _harvestingFill, ref _harvestingLevelText, Profession.Harvesting);
            ConfigureVerticalProgressBar(ref _miningBarGameObject, ref _miningProgressFill, ref _miningFill, ref _miningLevelText, Profession.Mining);
            ConfigureVerticalProgressBar(ref _woodcuttingBarGameObject, ref _woodcuttingProgressFill, ref _woodcuttingFill, ref _woodcuttingLevelText, Profession.Woodcutting);
        }

        if (_shiftSlot)
        {
            ConfigureShiftSlot(ref _abilityDummyObject, ref _abilityBarEntry, ref _uiState, ref _cooldownParentObject, ref _cooldownText,
                ref _chargesTextObject, ref _cooldownFillImage, ref _chargesText, ref _chargeCooldownFillImage, ref _chargeCooldownImageObject,
                ref _abilityEmptyIcon, ref _abilityIcon, ref _keybindObject);
        }

        _ready = true;
    }
    static void InitializeAbilitySlotButtons()
    {
        foreach (var keyValuePair in _uiElementsConfigured)
        {
            if (keyValuePair.Value && _abilitySlotNamePaths.ContainsKey(keyValuePair.Key))
            {
                GameObject abilitySlotObject = GameObject.Find(_abilitySlotNamePaths[keyValuePair.Key]);
                SimpleStunButton stunButton = abilitySlotObject.AddComponent<SimpleStunButton>();

                if (keyValuePair.Key.Equals(UIElement.Professions))
                {
                    GameObject[] capturedObjects = [.._professionObjects];
                    stunButton.onClick.AddListener((UnityAction)(() => ToggleGameObjects(capturedObjects)));
                }
                else if (_gameObjects.TryGetValue(keyValuePair.Key, out GameObject gameObject))
                {
                    GameObject[] capturedObjects = [gameObject];
                    stunButton.onClick.AddListener((UnityAction)(() => ToggleGameObjects(capturedObjects)));
                }
            }
        }
    }
    static void ToggleGameObjects(params GameObject[] gameObjects)
    {
        foreach (GameObject gameObject in gameObjects)
        {
            bool newState = !gameObject.activeSelf;
            gameObject.SetActive(newState);

            _objectStates[gameObject] = newState;
        }
    }
    static void ExperienceToggle()
    {
        bool active = !_experienceBarGameObject.activeSelf;

        _experienceBarGameObject.SetActive(active);
        _objectStates[_experienceBarGameObject] = active;
    }
    static void LegacyToggle()
    {
        bool active = !_legacyBarGameObject.activeSelf;

        _legacyBarGameObject.SetActive(active);
        _objectStates[_legacyBarGameObject] = active;
    }
    static void ExpertiseToggle()
    {
        bool active = !_expertiseBarGameObject.activeSelf;

        _expertiseBarGameObject.SetActive(active);
        _objectStates[_expertiseBarGameObject] = active;
    }
    static void FamiliarToggle()
    {
        bool active = !_familiarBarGameObject.activeSelf;

        _familiarBarGameObject.SetActive(active);
        _objectStates[_familiarBarGameObject] = active;
    }
    static void ProfessionToggle()
    {
        foreach (GameObject professionObject in _professionObjects)
        {
            Core.Log.LogWarning($"Toggling profession object: {professionObject.name} ({professionObject.activeSelf})");
            bool active = !professionObject.activeSelf;

            professionObject.SetActive(active);
            _objectStates[professionObject] = active;

            /*
            if (_objectStates.ContainsKey(professionObject))
            {
                bool active = !professionObject.activeSelf;

                professionObject.SetActive(active);
                _objectStates[professionObject] = active;
            }
            else
            {
                Core.Log.LogWarning($"Profession object not found!");
            }
            */
        }

        Core.Log.LogWarning($"Toggled profession objects ({_professionObjects.Count})");
    }
    static void DailyQuestToggle()
    {
        bool active = !_dailyQuestObject.activeSelf;

        _dailyQuestObject.SetActive(active);
        _objectStates[_dailyQuestObject] = active;
    }
    static void WeeklyQuestToggle()
    {
        bool active = !_weeklyQuestObject.activeSelf;

        _weeklyQuestObject.SetActive(active);
        _objectStates[_weeklyQuestObject] = active;
    }
    static void ShiftSlotToggle()
    {
        bool active = !_abilityDummyObject.activeSelf;

        _abilityDummyObject.SetActive(active);
        _objectStates[_abilityDummyObject] = active;
    }
    static void InitializeBloodButton()
    {
        GameObject bloodObject = GameObject.Find("HUDCanvas(Clone)/BottomBarCanvas/BottomBar(Clone)/Content/BloodOrbParent/BloodOrb/BlackBackground/Blood");

        if (bloodObject != null)
        {
            SimpleStunButton stunButton = bloodObject.AddComponent<SimpleStunButton>();
            stunButton.onClick.AddListener(new Action(ToggleAllObjects));
        }
    }
    static void ToggleAllObjects()
    {
        _active = !_active;

        foreach (GameObject gameObject in _objectStates.Keys)
        {
            gameObject.active = _active;
            _objectStates[gameObject] = _active;
        }

        // Tutorial();
    }
    public static IEnumerator CanvasUpdateLoop()
    {
        while (true)
        {
            if (_killSwitch)
            {
                yield break;
            }
            else if (!_ready || !_active)
            {
                yield return _delay;
                continue;
            }

            if (_experienceBar)
            {
                try
                {
                    UpdateBar(_experienceProgress, _experienceLevel, _experienceMaxLevel, _experiencePrestige, _experienceText, _experienceHeader, _experienceFill, UIElement.Experience);
                    UpdateClass(_classType, _experienceClassText);
                }
                catch (Exception e)
                {
                    Core.Log.LogError($"Error updating experience bar: {e}");
                }
            }

            if (_legacyBar)
            {
                try
                {
                    UpdateBar(_legacyProgress, _legacyLevel, _legacyMaxLevel, _legacyPrestige, _legacyText, _legacyHeader, _legacyFill, UIElement.Legacy, _legacyType);
                    UpdateBloodStats(_legacyBonusStats, [_firstLegacyStat, _secondLegacyStat, _thirdLegacyStat], GetBloodStatInfo);
                }
                catch (Exception e)
                {
                    Core.Log.LogError($"Error updating legacy bar: {e}");
                }
            }

            if (_expertiseBar)
            {
                try
                {
                    UpdateBar(_expertiseProgress, _expertiseLevel, _expertiseMaxLevel, _expertisePrestige, _expertiseText, _expertiseHeader, _expertiseFill, UIElement.Expertise, _expertiseType);
                    UpdateWeaponStats(_expertiseBonusStats, [_firstExpertiseStat, _secondExpertiseStat, _thirdExpertiseStat], GetWeaponStatInfo);
                    GetAndUpdateWeaponStatBuffer(LocalCharacter);
                }
                catch (Exception e)
                {
                    Core.Log.LogError($"Error updating expertise bar: {e}");
                }
            }

            if (_statsBuffActive)
            {
                try
                {
                    if (LocalCharacter.TryGetBuff(_statsBuff, out Entity buffEntity))
                    {
                        UpdateBuffStatBuffer(buffEntity);
                    }
                }
                catch (Exception e)
                {
                    Core.Log.LogError($"Error updating stats buff: {e}");
                }
            }

            if (_familiarBar)
            {
                try
                {
                    UpdateBar(_familiarProgress, _familiarLevel, _familiarMaxLevel, _familiarPrestige, _familiarText, _familiarHeader, _familiarFill, UIElement.Familiars, _familiarName);
                    UpdateFamiliarStats(_familiarStats, [_familiarMaxHealth, _familiarPhysicalPower, _familiarSpellPower]);
                }
                catch (Exception e)
                {
                    Core.Log.LogError($"Error updating familiar bar: {e}");
                }
            }

            if (_questTracker)
            {
                try
                {
                    UpdateQuests(_dailyQuestObject, _dailyQuestSubHeader, _dailyQuestIcon, _dailyTargetType, _dailyTarget, _dailyProgress, _dailyGoal, _dailyVBlood);
                    UpdateQuests(_weeklyQuestObject, _weeklyQuestSubHeader, _weeklyQuestIcon, _weeklyTargetType, _weeklyTarget, _weeklyProgress, _weeklyGoal, _weeklyVBlood);
                }
                catch (Exception e)
                {
                    Core.Log.LogError($"Error updating quest tracker: {e}");
                }
            }

            if (_professionBars)
            {
                try
                {
                    UpdateProfessions(_alchemyProgress, _alchemyLevel, _alchemyLevelText, _alchemyProgressFill, _alchemyFill, Profession.Alchemy);
                    UpdateProfessions(_blacksmithingProgress, _blacksmithingLevel, _blacksmithingLevelText, _blacksmithingProgressFill, _blacksmithingFill, Profession.Blacksmithing);
                    UpdateProfessions(_enchantingProgress, _enchantingLevel, _enchantingLevelText, _enchantingProgressFill, _enchantingFill, Profession.Enchanting);
                    UpdateProfessions(_tailoringProgress, _tailoringLevel, _tailoringLevelText, _tailoringProgressFill, _tailoringFill, Profession.Tailoring);
                }
                catch (Exception e)
                {
                    Core.Log.LogError($"Error updating professions(1): {e}");
                }

                try
                {
                    UpdateProfessions(_fishingProgress, _fishingLevel, _fishingLevelText, _fishingProgressFill, _fishingFill, Profession.Fishing);
                    UpdateProfessions(_harvestingProgress, _harvestingLevel, _harvestingLevelText, _harvestingProgressFill, _harvestingFill, Profession.Harvesting);
                    UpdateProfessions(_miningProgress, _miningLevel, _miningLevelText, _miningProgressFill, _miningFill, Profession.Mining);
                    UpdateProfessions(_woodcuttingProgress, _woodcuttingLevel, _woodcuttingLevelText, _woodcuttingProgressFill, _woodcuttingFill, Profession.Woodcutting);
                }
                catch (Exception e)
                {
                    Core.Log.LogError($"Error updating professions(2): {e}");
                }
            }

            if (_killSwitch) yield break;

            try
            {
                if (!_shiftActive && LocalCharacter.TryGetComponent(out AbilityBar_Shared abilityBar_Shared))
                {
                    Entity abilityGroupEntity = abilityBar_Shared.CastGroup.GetEntityOnServer();

                    if (abilityGroupEntity.TryGetComponent(out AbilityGroupState abilityGroupState) && abilityGroupState.SlotIndex == 3) // if ability found on slot 3, activate shift loop
                    {
                        if (_shiftRoutine == null)
                        {
                            _shiftRoutine = ShiftUpdateLoop().Start();
                            _shiftActive = true;
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Core.Log.LogError($"Error updating ability bar: {e}");
            }

            bool isSynced = _isGamepad ? _controllerType.Equals(ControllerType.Gamepad) : _controllerType.Equals(ControllerType.KeyboardAndMouse);
            if (!isSynced) SyncAdaptiveElements(_isGamepad);

            yield return _delay;
        }
    }
    static void GetAndUpdateWeaponStatBuffer(Entity playerCharacter)
    {
        if (!playerCharacter.TryGetComponent(out Equipment equipment)) return;

        Entity weaponEntity = equipment.GetEquipmentEntity(EquipmentType.Weapon).GetEntityOnServer();
        if (!weaponEntity.Exists()) return;

        Entity prefabEntity = weaponEntity.GetPrefabEntity();
        UpdateWeaponStatBuffer(prefabEntity);
    }
    static void UpdateWeaponStatBuffer(Entity weaponEntity)
    {
        if (!weaponEntity.TryGetBuffer<ModifyUnitStatBuff_DOTS>(out var buffer)) return;

        List<int> existingIds = [];

        for (int i = buffer.Length - 1; i >= 0; i--)
        {
            int id = buffer[i].Id.Id;

            if (id != 0 && !_weaponStats.ContainsKey(id))
            {
                buffer.RemoveAt(i);
            }
            else
            {
                existingIds.Add(id);
            }
        }

        foreach (var keyValuePair in _weaponStats)
        {
            if (!existingIds.Contains(keyValuePair.Key))
            {
                buffer.Add(keyValuePair.Value);
            }
        }
    }
    static void UpdateBuffStatBuffer(Entity buffEntity)
    {
        if (!buffEntity.TryGetBuffer<ModifyUnitStatBuff_DOTS>(out var buffer)) return;

        List<int> existingIds = [];

        for (int i = buffer.Length - 1; i >= 0; i--)
        {
            int id = buffer[i].Id.Id;

            if (!_weaponStats.ContainsKey(id) && !_bloodStats.ContainsKey(id))
            {
                buffer.RemoveAt(i);
            }
            else
            {
                existingIds.Add(id);
            }
        }

        foreach (var keyValuePair in _weaponStats)
        {
            if (!existingIds.Contains(keyValuePair.Key))
            {
                buffer.Add(keyValuePair.Value);
            }
        }

        foreach (var keyValuePair in _bloodStats)
        {
            if (!existingIds.Contains(keyValuePair.Key))
            {
                buffer.Add(keyValuePair.Value);
            }
        }

        _weaponStats.Clear();
        _bloodStats.Clear();
    }
    static void UpdateAbilityData(AbilityTooltipData abilityTooltipData, Entity abilityGroupEntity, Entity abilityCastEntity, PrefabGUID abilityGroupPrefabGUID)
    {
        if (!_abilityDummyObject.active)
        {
            _abilityDummyObject.SetActive(true);
            if (_uiState.CachedInputVersion != 3) _uiState.CachedInputVersion = 3;
        }

        if (!_keybindObject.active) _keybindObject.SetActive(true);

        _cooldownFillImage.fillAmount = 0f;
        _chargeCooldownFillImage.fillAmount = 0f;

        _abilityGroupPrefabGUID = abilityGroupPrefabGUID;

        _abilityBarEntry.AbilityEntity = abilityGroupEntity;
        _abilityBarEntry.AbilityId = abilityGroupPrefabGUID;
        _abilityBarEntry.AbilityIconImage.sprite = abilityTooltipData.Icon;

        _abilityBarEntry._CurrentUIState.AbilityIconImageActive = true;
        _abilityBarEntry._CurrentUIState.AbilityIconImageSprite = abilityTooltipData.Icon;

        if (abilityGroupEntity.TryGetComponent(out AbilityChargesData abilityChargesData))
        {
            _maxCharges = abilityChargesData.MaxCharges;
        }
        else
        {
            _maxCharges = 0;
            _currentCharges = 0;
            _chargesText.SetText("");
        }

        if (abilityCastEntity.TryGetComponent(out AbilityCooldownData abilityCooldownData))
        {
            _cooldownTime = _shiftSpellIndex.Equals(-1) ? abilityCooldownData.Cooldown._Value : _shiftSpellIndex * COOLDOWN_FACTOR + COOLDOWN_FACTOR;
            _cooldownEndTime = Core.ServerTime.TimeOnServer + _cooldownTime;
        }
    }
    static void UpdateAbilityState(Entity abilityGroupEntity, Entity abilityCastEntity)
    {
        PrefabGUID prefabGuid = abilityGroupEntity.GetPrefabGUID();
        if (prefabGuid.HasValue() && !prefabGuid.Equals(_abilityGroupPrefabGUID)) return;

        if (abilityCastEntity.TryGetComponent(out AbilityCooldownState abilityCooldownState))
        {
            _cooldownEndTime = _shiftSpellIndex.Equals(-1) ? abilityCooldownState.CooldownEndTime : _cooldownEndTime;
        }

        _chargeUpTimeRemaining = (float)(_chargeUpEndTime - Core.ServerTime.TimeOnServer);
        _cooldownRemaining = (float)(_cooldownEndTime - Core.ServerTime.TimeOnServer);
        
        // Core.Log.LogInfo($"UpdateAbilityState _cooldownRemaining - {_cooldownRemaining}");

        if (abilityGroupEntity.TryGetComponent(out AbilityChargesState abilityChargesState))
        {
            _currentCharges = abilityChargesState.CurrentCharges;
            _chargeUpTime = abilityChargesState.ChargeTime;
            _chargeUpEndTime = Core.ServerTime.TimeOnServer + _chargeUpTime;

            if (_currentCharges == 0)
            {
                _abilityBarEntry._CurrentUIState.ChargesTextActive = false;
                _chargeCooldownFillImage.fillAmount = 0f;
                _chargeCooldownImageObject.SetActive(false);

                _chargesText.SetText("");
                _cooldownText.SetText($"{(int)_chargeUpTime}");

                _cooldownFillImage.fillAmount = _chargeUpTime / _cooldownTime;
            }
            else
            {
                _abilityBarEntry._CurrentUIState.ChargesTextActive = true;
                _cooldownFillImage.fillAmount = 0f;

                _chargesTextObject.SetActive(true);
                _chargeCooldownImageObject.SetActive(true);

                _cooldownText.SetText("");
                _chargesText.SetText($"{_currentCharges}");

                _chargeCooldownFillImage.fillAmount = 1 - _cooldownRemaining / _cooldownTime;

                if (_currentCharges == _maxCharges) _chargeCooldownFillImage.fillAmount = 0f;
            }
        }
        else if (_maxCharges > 0)
        {
            if (_currentCharges == 0)
            {
                _abilityBarEntry._CurrentUIState.ChargesTextActive = true;
                _chargeCooldownFillImage.fillAmount = 0f;
                _chargeCooldownImageObject.SetActive(false);

                if (_chargeUpTimeRemaining < 0f)
                {
                    _cooldownText.SetText("");
                    _chargesText.SetText("1");
                }
                else
                {
                    _chargesText.SetText("");
                    _cooldownText.SetText($"{(int)_chargeUpTimeRemaining}");
                }

                _cooldownFillImage.fillAmount = _chargeUpTimeRemaining / _cooldownTime;

                if (_chargeUpTimeRemaining < 0f)
                {
                    ++_currentCharges;
                    _cooldownEndTime = Core.ServerTime.TimeOnServer + _cooldownTime;
                }
            }
            else if (_currentCharges < _maxCharges && _currentCharges > 0)
            {
                _cooldownText.SetText("");
                _abilityBarEntry._CurrentUIState.ChargesTextActive = true;
                _cooldownFillImage.fillAmount = 0f;

                _chargesTextObject.SetActive(true);
                _chargeCooldownImageObject.SetActive(true);

                _chargesText.SetText($"{_currentCharges}");

                _chargeCooldownFillImage.fillAmount = 1f - _cooldownRemaining / _cooldownTime;

                if (_cooldownRemaining < 0f)
                {
                    ++_currentCharges;
                    _cooldownEndTime = Core.ServerTime.TimeOnServer + _cooldownTime;
                }
            }
            else if (_currentCharges == _maxCharges)
            {
                _chargeCooldownImageObject.SetActive(false);

                _cooldownText.SetText("");
                _abilityBarEntry._CurrentUIState.ChargesTextActive = true;

                _cooldownFillImage.fillAmount = 0f;
                _chargeCooldownFillImage.fillAmount = 0f;

                _chargesTextObject.SetActive(true);
                _chargesText.SetText($"{_currentCharges}");
            }
        }
        else
        {
            _currentCharges = 0;
            _abilityBarEntry._CurrentUIState.ChargesTextActive = false;

            _chargeCooldownImageObject.SetActive(false);
            _chargeCooldownFillImage.fillAmount = 0f;

            if (_cooldownRemaining < 0f)
            {
                _cooldownText.SetText($"");
            }
            else
            {
                _cooldownText.SetText($"{(int)_cooldownRemaining}");
            }

            _cooldownFillImage.fillAmount = _cooldownRemaining / _cooldownTime;
        }
    }
    public static IEnumerator ShiftUpdateLoop()
    {
        while (true)
        {
            if (_killSwitch)
            {
                yield break;
            }
            else if (!_ready)
            {
                yield return _delay;
                continue;
            }
            else if (!_shiftActive)
            {
                yield return _delay;
                continue;
            }

            if (LocalCharacter.TryGetComponent(out AbilityBar_Shared abilityBar_Shared))
            {
                Entity abilityGroupEntity = abilityBar_Shared.CastGroup.GetEntityOnServer();
                Entity abilityCastEntity = abilityBar_Shared.CastAbility.GetEntityOnServer();

                if (abilityGroupEntity.TryGetComponent(out AbilityGroupState abilityGroupState) && abilityGroupState.SlotIndex == 3)
                {
                    PrefabGUID currentPrefabGUID = abilityGroupEntity.GetPrefabGUID();

                    if (TryUpdateTooltipData(abilityGroupEntity, currentPrefabGUID))
                    {
                        UpdateAbilityData(_abilityTooltipData, abilityGroupEntity, abilityCastEntity, currentPrefabGUID);
                    }
                    else if (_abilityTooltipData != null)
                    {
                        UpdateAbilityData(_abilityTooltipData, abilityGroupEntity, abilityCastEntity, currentPrefabGUID);
                    }
                }

                if (_abilityTooltipData != null)
                {
                    UpdateAbilityState(abilityGroupEntity, abilityCastEntity);
                }
            }

            yield return _shiftDelay;
        }
    }
    static bool TryUpdateTooltipData(Entity abilityGroupEntity, PrefabGUID abilityGroupPrefabGUID)
    {
        if (_abilityTooltipData == null || _abilityGroupPrefabGUID != abilityGroupPrefabGUID)
        {
            if (abilityGroupEntity.TryGetComponentObject(EntityManager, out _abilityTooltipData))
            {
                _abilityTooltipData ??= EntityManager.GetComponentObject<AbilityTooltipData>(abilityGroupEntity, _abilityTooltipDataComponent);
            }
        }

        return _abilityTooltipData != null;
    }
    static void UpdateProfessions(float progress, int level, LocalizedText levelText, 
        Image progressFill, Image fill, Profession profession)
    {
        if (_killSwitch) return;

        if (level == MAX_PROFESSION_LEVEL)
        {
            progressFill.fillAmount = 1f;
            fill.fillAmount = 1f;
        }
        else
        {
            progressFill.fillAmount = progress;
            fill.fillAmount = level / MAX_PROFESSION_LEVEL;
        }
    }
    static void UpdateBar(float progress, int level, int maxLevel, 
        int prestiges, LocalizedText levelText, LocalizedText barHeader, 
        Image fill, UIElement element, string type = "")
    {
        if (_killSwitch) return;

        string levelString = level.ToString();

        if (type == "Frailed" || type == "Familiar")
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

        if (element.Equals(UIElement.Familiars))
        {
            type = TrimToFirstWord(type);
        }

        if (barHeader.Text.fontSize != _horizontalBarHeaderFontSize)
        {
            barHeader.Text.fontSize = _horizontalBarHeaderFontSize;
        }

        if (_showPrestige && prestiges != 0)
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
            else if (element.Equals(UIElement.Familiars))
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
        if (_killSwitch) return;

        if (classType != PlayerClass.None)
        {
            if (!classText.enabled) classText.enabled = true;
            if (!classText.gameObject.active) classText.gameObject.SetActive(true);

            string formattedClassName = FormatClassName(classType);
            classText.ForceSet(formattedClassName);

            if (_classColorHexMap.TryGetValue(classType, out Color classColor))
            {
                classText.Text.color = classColor;
            }
        }
        else
        {
            classText.ForceSet("");
            classText.enabled = false;
        }
    }
    static string FormatClassName(PlayerClass classType)
    {
        return _classNameRegex.Replace(classType.ToString(), " $1");
    }
    static void UpdateWeaponStats(List<string> bonusStats, List<LocalizedText> statTexts, Func<string, string> getStatInfo)
    {
        if (_killSwitch) return;

        for (int i = 0; i < 3; i++)
        {
            if (bonusStats[i] != "None")
            {
                if (!statTexts[i].enabled) statTexts[i].enabled = true;
                if (!statTexts[i].gameObject.active) statTexts[i].gameObject.SetActive(true);

                string statInfo = getStatInfo(bonusStats[i]);
                statTexts[i].ForceSet(statInfo);
            }
            else if (bonusStats[i] == "None" && statTexts[i].enabled)
            {
                statTexts[i].ForceSet("");
                statTexts[i].enabled = false;
            }

            // Core.Log.LogWarning($"WeaponStats - {bonusStats[i]} - {statTexts[i].GetText()}");
        }
    }
    static void UpdateBloodStats(List<string> bonusStats, List<LocalizedText> statTexts, Func<string, string> getStatInfo)
    {
        if (_killSwitch) return;

        for (int i = 0; i < 3; i++)
        {
            if (bonusStats[i] != "None")
            {
                if (!statTexts[i].enabled) statTexts[i].enabled = true;
                if (!statTexts[i].gameObject.active) statTexts[i].gameObject.SetActive(true);

                string statInfo = getStatInfo(bonusStats[i]);
                statTexts[i].ForceSet(statInfo);
            }
            else if (bonusStats[i] == "None" && statTexts[i].enabled)
            {
                statTexts[i].ForceSet("");
                statTexts[i].enabled = false;
            }

            // Core.Log.LogWarning($"BloodStats - {bonusStats[i]} - {statTexts[i].GetText()}");
        }
    }
    static string GetWeaponStatInfo(string statType)
    {
        if (Enum.TryParse(statType, out WeaponStatType weaponStat))
        {
            if (_weaponStatValues.TryGetValue(weaponStat, out float statValue))
            {
                float classMultiplier = ClassSynergy(weaponStat, _classType, _classStatSynergies);
                statValue *= (1 + _prestigeStatMultiplier * _expertisePrestige) * classMultiplier * ((float)_expertiseLevel / _expertiseMaxLevel);
                float displayStatValue = statValue;
                int statModificationId = ModificationIds.GenerateId(0, (int)weaponStat, statValue);

                if (weaponStat.Equals(WeaponStatType.MovementSpeed)
                    && LocalCharacter.TryGetComponent(out Movement movement))
                {
                    float movementSpeed = movement.Speed._Value;
                    statValue /= movementSpeed;
                }

                ModifyUnitStatBuff_DOTS unitStatBuff = new()
                {
                    StatType = (UnitStatType)Enum.Parse(typeof(UnitStatType), weaponStat.ToString()),
                    ModificationType = ModificationType.Add,
                    Value = statValue,
                    Modifier = 1,
                    IncreaseByStacks = false,
                    ValueByStacks = 0,
                    Priority = 0,
                    Id = new(statModificationId)
                };

                _weaponStats.TryAdd(statModificationId, unitStatBuff);
                return FormatWeaponStat(weaponStat, displayStatValue);
            }
            else
            {
                // Core.Log.LogWarning($"GetWeaponStatInfo couldn't find - {statType}");
            }
        }
        else
        {
            // Core.Log.LogWarning($"GetWeaponStatInfo couldn't parse - {statType}");
        }

        return string.Empty;
    }
    static string GetBloodStatInfo(string statType)
    {
        if (Enum.TryParse(statType, out BloodStatType bloodStat))
        {
            if (_bloodStatValues.TryGetValue(bloodStat, out float statValue))
            {
                float classMultiplier = ClassSynergy(bloodStat, _classType, _classStatSynergies);
                statValue *= (1 + _prestigeStatMultiplier * _legacyPrestige) * classMultiplier * ((float)_legacyLevel / _legacyMaxLevel);
                string displayString = $"<color=#00FFFF>{BloodStatTypeAbbreviations[bloodStat]}</color>: <color=#90EE90>{(statValue * 100).ToString("F0") + "%"}</color>";

                int statModificationId = ModificationIds.GenerateId(1, (int)bloodStat, statValue);

                ModifyUnitStatBuff_DOTS unitStatBuff = new()
                {
                    StatType = (UnitStatType)Enum.Parse(typeof(UnitStatType), bloodStat.ToString()),
                    ModificationType = ModificationType.Add,
                    Value = statValue,
                    Modifier = 1,
                    IncreaseByStacks = false,
                    ValueByStacks = 0,
                    Priority = 0,
                    Id = new(statModificationId)
                };

                _bloodStats.TryAdd(statModificationId, unitStatBuff);

                // Core.Log.LogWarning($"GetBloodStatInfo - {statModificationId}|{_buffStats.Count}");

                return displayString;
            }
            else
            {
                // Core.Log.LogWarning($"GetBloodStatInfo couldn't find - {statType}");
            }
        }
        else
        {
            // Core.Log.LogWarning($"GetBloodStatInfo couldn't parse - {statType}");
        }

        return "";
    }
    static void UpdateFamiliarStats(List<string> familiarStats, List<LocalizedText> statTexts)
    {
        if (_killSwitch) return;

        for (int i = 0; i < 3; i++)
        {
            if (!string.IsNullOrEmpty(familiarStats[i]))
            {
                if (!statTexts[i].enabled) statTexts[i].enabled = true;
                if (!statTexts[i].gameObject.active) statTexts[i].gameObject.SetActive(true);

                string statInfo = $"<color=#00FFFF>{FamiliarStatStringAbbreviations[i]}</color>: <color=#90EE90>{familiarStats[i]}</color>";
                statTexts[i].ForceSet(statInfo);
            }
            else if (statTexts[i].enabled)
            {
                statTexts[i].ForceSet("");
                statTexts[i].enabled = false;
            }
        }
    }

    const string FISHING = "Go Fish!";
    static void UpdateQuests(GameObject questObject, LocalizedText questSubHeader, Image questIcon, 
        TargetType targetType, string target, int progress, int goal, bool isVBlood)
    {
        if (_killSwitch) return;

        if (progress != goal && _objectStates[questObject])
        {
            if (!questObject.gameObject.active) questObject.gameObject.active = true;

            if (targetType.Equals(TargetType.Kill))
            {
                // int index = target.IndexOf(TRIMMER);
                // target = index >= 0 ? target[..index] : target;
                target = TrimToFirstWord(target);
            }
            else if (targetType.Equals(TargetType.Fish)) target = FISHING;

            questSubHeader.ForceSet($"<color=white>{target}</color>: {progress}/<color=yellow>{goal}</color>");

            switch (targetType)
            {
                case TargetType.Kill:
                    if (!questIcon.gameObject.active) questIcon.gameObject.active = true;
                    if (isVBlood && questIcon.sprite != _questKillVBloodUnit)
                    {
                        questIcon.sprite = _questKillVBloodUnit;
                    }
                    else if (!isVBlood && questIcon.sprite != _questKillStandardUnit)
                    {
                        questIcon.sprite = _questKillStandardUnit;
                    }
                    break;
                case TargetType.Craft:
                    if (!questIcon.gameObject.active) questIcon.gameObject.active = true;
                    PrefabGUID targetPrefabGUID = LocalizationService.GetPrefabGuidFromName(target);
                    ManagedItemData managedItemData = ManagedDataRegistry.GetOrDefault<ManagedItemData>(targetPrefabGUID);
                    if (managedItemData != null && questIcon.sprite != managedItemData.Icon)
                    {
                        questIcon.sprite = managedItemData.Icon;
                    }
                    break;
                case TargetType.Gather:
                    if (!questIcon.gameObject.active) questIcon.gameObject.active = true;
                    targetPrefabGUID = LocalizationService.GetPrefabGuidFromName(target);
                    if (target.Equals("Stone")) targetPrefabGUID = PrefabGUIDs.Item_Ingredient_Stone; // not sure don't care hard-coding for now
                    managedItemData = ManagedDataRegistry.GetOrDefault<ManagedItemData>(targetPrefabGUID);
                    if (managedItemData != null && questIcon.sprite != managedItemData.Icon)
                    {
                        questIcon.sprite = managedItemData.Icon;
                    }
                    break;
                case TargetType.Fish:
                    if (!questIcon.gameObject.active) questIcon.gameObject.active = true;
                    // targetPrefabGUID = LocalizationService.GetPrefabGuidFromName(target);
                    managedItemData = ManagedDataRegistry.GetOrDefault<ManagedItemData>(PrefabGUIDs.FakeItem_AnyFish);
                    if (managedItemData != null && questIcon.sprite != managedItemData.Icon)
                    {
                        questIcon.sprite = managedItemData.Icon;
                    }
                    break;
                default:
                    break;
            }
        }
        else
        {
            questObject.gameObject.active = false;
            questIcon.gameObject.active = false;
        }
    }
    static void ConfigureShiftSlot(ref GameObject shiftSlotObject, ref AbilityBarEntry shiftSlotEntry, ref AbilityBarEntry.UIState uiState, ref GameObject cooldownObject,
    ref TextMeshProUGUI cooldownText, ref GameObject chargeCooldownTextObject, ref Image cooldownFill, 
    ref TextMeshProUGUI chargeCooldownText, ref Image chargeCooldownFillImage, ref GameObject chargeCooldownFillObject,
    ref GameObject abilityEmptyIcon, ref GameObject abilityIcon, ref GameObject keybindObject)
    {
        GameObject abilityDummyObject = GameObject.Find("HUDCanvas(Clone)/BottomBarCanvas/BottomBar(Clone)/Content/Background/AbilityBar/Abilities/AbilityBarEntry_Dummy/");

        if (abilityDummyObject != null)
        {
            shiftSlotObject = UnityEngine.Object.Instantiate(abilityDummyObject);
            RectTransform rectTransform = shiftSlotObject.GetComponent<RectTransform>();

            RectTransform abilitiesTransform = GameObject.Find("HUDCanvas(Clone)/BottomBarCanvas/BottomBar(Clone)/Content/Background/AbilityBar/Abilities/").GetComponent<RectTransform>();

            UnityEngine.Object.DontDestroyOnLoad(shiftSlotObject);
            SceneManager.MoveGameObjectToScene(shiftSlotObject, SceneManager.GetSceneByName("VRisingWorld"));

            shiftSlotObject.transform.SetParent(abilitiesTransform, false);
            shiftSlotObject.SetActive(false);

            shiftSlotEntry = shiftSlotObject.GetComponent<AbilityBarEntry>();
            shiftSlotEntry._CurrentUIState.CachedInputVersion = 3;
            uiState = shiftSlotEntry._CurrentUIState;

            cooldownObject = FindTargetUIObject(rectTransform, "CooldownParent").gameObject;
            cooldownText = FindTargetUIObject(rectTransform, "Cooldown").GetComponent<TextMeshProUGUI>();
            cooldownText.SetText("");
            cooldownText.alpha = 1f;
            cooldownText.color = Color.white;
            cooldownText.enabled = true;

            cooldownFill = FindTargetUIObject(rectTransform, "CooldownOverlayFill").GetComponent<Image>();
            cooldownFill.fillAmount = 0f;
            cooldownFill.enabled = true;

            chargeCooldownFillObject = FindTargetUIObject(rectTransform, "ChargeCooldownImage");
            chargeCooldownFillImage = chargeCooldownFillObject.GetComponent<Image>();
            chargeCooldownFillImage.fillOrigin = 2;
            chargeCooldownFillImage.fillAmount = 0f;
            chargeCooldownFillImage.fillMethod = Image.FillMethod.Radial360;
            chargeCooldownFillImage.fillClockwise = true;
            chargeCooldownFillImage.enabled = true;

            chargeCooldownTextObject = FindTargetUIObject(rectTransform, "ChargeCooldown");
            chargeCooldownText = chargeCooldownTextObject.GetComponent<TextMeshProUGUI>();
            chargeCooldownText.SetText("");
            chargeCooldownText.alpha = 1f;
            chargeCooldownText.color = Color.white;
            chargeCooldownText.enabled = true;

            // chargeUpFill = FindTargetUIObject(rectTransform, "ChargeUpFill").GetComponent<Image>();
            // chargeUpFill.fillAmount = 0f;
            // chargeUpFill.enabled = true;

            abilityEmptyIcon = FindTargetUIObject(rectTransform, "EmptyIcon");
            abilityEmptyIcon.SetActive(false);

            abilityIcon = FindTargetUIObject(rectTransform, "Icon");
            abilityIcon.SetActive(true);

            keybindObject = GameObject.Find("HUDCanvas(Clone)/BottomBarCanvas/BottomBar(Clone)/Content/Background/AbilityBar/Abilities/AbilityBarEntry_Dummy(Clone)/KeybindBackground/Keybind/");
            TextMeshProUGUI keybindText = keybindObject.GetComponent<TextMeshProUGUI>();
            keybindText.SetText("Shift");
            keybindText.enabled = true;

            //RectTransform layoutTransform = keybindImageLayout.GetComponent<RectTransform>();
            //keybindImageObject = GameObject.Find("HUDCanvas(Clone)/BottomBarCanvas/BottomBar(Clone)/Content/Background/AbilityBar/Abilities/AbilityBarEntry_Dummy(Clone)/KeybindBackground/KeybindImageLayout/KeybindImage/");
            //keybindImage = keybindImageObject.GetComponent<Image>();
            //keybindImageObject.SetActive(false);

            _objectStates.Add(shiftSlotObject, true);
            _gameObjects.Add(UIElement.ShiftSlot, shiftSlotObject);

            SimpleStunButton stunButton = shiftSlotObject.AddComponent<SimpleStunButton>();

            if (_actionToggles.TryGetValue(SHIFT_SLOT, out var toggleAction))
            {
                stunButton.onClick.AddListener(new Action(toggleAction));
            }
        }
        else
        {
            Core.Log.LogWarning("AbilityBarEntry_Dummy is null!");
        }
    }
    static void ConfigureQuestWindow(ref GameObject questObject, UIElement questType, Color headerColor, 
        ref LocalizedText header, ref LocalizedText subHeader, ref Image questIcon)
    {
        // Instantiate quest tooltip
        questObject = UnityEngine.Object.Instantiate(_canvasBase.BottomBarParentPrefab.FakeTooltip.gameObject);
        RectTransform questTransform = questObject.GetComponent<RectTransform>();

        // Prevent quest window from being destroyed on scene load and move to scene
        UnityEngine.Object.DontDestroyOnLoad(questObject);
        SceneManager.MoveGameObjectToScene(questObject, SceneManager.GetSceneByName("VRisingWorld"));

        // Set parent and activate quest window
        questTransform.SetParent(_bottomBarCanvas.transform, false);
        questTransform.gameObject.layer = _layer;
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
        RectTransform tooltipIconTransform = tooltipIcon.GetComponent<RectTransform>();

        // Set position relative to parent
        tooltipIconTransform.anchorMin = new Vector2(tooltipIconTransform.anchorMin.x, 0.55f);
        tooltipIconTransform.anchorMax = new Vector2(tooltipIconTransform.anchorMax.x, 0.55f);

        // Set the pivot to the vertical center
        tooltipIconTransform.pivot = new Vector2(tooltipIconTransform.pivot.x, 0.55f);

        questIcon = tooltipIcon.GetComponent<Image>();
        if (questType.Equals(UIElement.Daily))
        {
            if (Sprites.ContainsKey("BloodIcon_Small_Warrior"))
            {
                questIcon.sprite = Sprites["BloodIcon_Small_Warrior"];
            }
        }
        else if (questType.Equals(UIElement.Weekly))
        {
            if (Sprites.ContainsKey("BloodIcon_Warrior"))
            {
                questIcon.sprite = Sprites["BloodIcon_Warrior"];
            }
        }

        tooltipIconTransform.sizeDelta = new Vector2(tooltipIconTransform.sizeDelta.x * 0.35f, tooltipIconTransform.sizeDelta.y * 0.35f);

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

        // Size window
        questTransform.sizeDelta = new Vector2(questTransform.sizeDelta.x * 0.65f, questTransform.sizeDelta.y);

        // Set anchor and pivots
        questTransform.anchorMin = new Vector2(1, _windowOffset); // Anchored to bottom-right
        questTransform.anchorMax = new Vector2(1, _windowOffset);
        questTransform.pivot = new Vector2(1, _windowOffset);
        questTransform.anchoredPosition = new Vector2(0, _windowOffset);

        // Keyboard/Mouse layout
        Vector2 kmAnchorMin = new(1f, _windowOffset);
        Vector2 kmAnchorMax = new(1f, _windowOffset);
        Vector2 kmPivot = new(1f, _windowOffset);
        Vector2 kmPos = new(0f, _windowOffset);

        // Controller layout
        Vector2 ctrlAnchorMin = new(0.6f, _windowOffset);
        Vector2 ctrlAnchorMax = new(0.6f, _windowOffset);
        Vector2 ctrlPivot = new(0.6f, _windowOffset);
        Vector2 ctrlPos = new(0f, _windowOffset);

        /*
        // Apply default (Keyboard/Mouse) position for now
        questTransform.anchorMin = kmAnchorMin;
        questTransform.anchorMax = kmAnchorMax;
        questTransform.pivot = kmPivot;
        questTransform.anchoredPosition = kmPos;
        */

        // Set header text
        header.ForceSet(questType.ToString() + " Quest");
        subHeader.ForceSet("UnitName: 0/0");

        // Add to active objects
        _gameObjects.Add(questType, questObject);
        _objectStates.Add(questObject, true);
        _windowOffset += 0.075f;

        // Register positions
        RegisterAdaptiveElement(
            questObject,
            keyboardMousePos: kmPos,
            keyboardMouseAnchorMin: kmAnchorMin,
            keyboardMouseAnchorMax: kmAnchorMax,
            keyboardMousePivot: kmPivot,
            keyboardMouseScale: questTransform.localScale,

            controllerPos: ctrlPos,
            controllerAnchorMin: ctrlAnchorMin,
            controllerAnchorMax: ctrlAnchorMax,
            controllerPivot: ctrlPivot,
            controllerScale: questTransform.localScale * 0.85f
            // controllerScale: questTransform.localScale
        );
    }
    static void ConfigureHorizontalProgressBar(ref GameObject barGameObject, ref GameObject informationPanelObject, ref Image fill, 
        ref LocalizedText level, ref LocalizedText header, UIElement element, Color fillColor, 
        ref LocalizedText firstText, ref LocalizedText secondText, ref LocalizedText thirdText)
    {
        // Instantiate the bar object from the prefab
        barGameObject = UnityEngine.Object.Instantiate(_canvasBase.TargetInfoParent.gameObject);
        // barGameObject = UIHelper.InstantiateGameObjectUnderAnchor(_canvasBase.TargetInfoParent.gameObject, _targetInfoPanelCanvas.transform);
        // barGameObject = UIHelper.InstantiateGameObject(_canvasBase.TargetInfoParent.gameObject);
        // UIHelper.SetParent(barGameObject.transform, _targetInfoPanelCanvas.transform, false);

        // DontDestroyOnLoad, change scene
        UnityEngine.Object.DontDestroyOnLoad(barGameObject);
        SceneManager.MoveGameObjectToScene(barGameObject, SceneManager.GetSceneByName("VRisingWorld"));

        RectTransform barRectTransform = barGameObject.GetComponent<RectTransform>();
        // barRectTransform.SetParent(_bottomBarCanvas.transform, false);
        barRectTransform.SetParent(_targetInfoPanelCanvas.transform, false);
        barRectTransform.gameObject.layer = _layer;

        // Set anchor and pivot to middle-upper-right
        float offsetY = BAR_HEIGHT_SPACING * _barNumber;
        float offsetX = 1f - BAR_WIDTH_SPACING;
        barRectTransform.anchorMin = new Vector2(offsetX, 0.6f - offsetY);
        barRectTransform.anchorMax = new Vector2(offsetX, 0.6f - offsetY);
        barRectTransform.pivot = new Vector2(offsetX, 0.6f - offsetY);

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

        // Set header text
        header.ForceSet(element.ToString());
        header.Text.fontSize *= 1.5f;
        _horizontalBarHeaderFontSize = header.Text.fontSize;

        // Set these to 0 so don't appear, deactivating instead seemed funky
        FindTargetUIObject(barRectTransform.transform, "DamageTakenFill").GetComponent<Image>().fillAmount = 0f;
        FindTargetUIObject(barRectTransform.transform, "AbsorbFill").GetComponent<Image>().fillAmount = 0f;

        // Configure informationPanels
        informationPanelObject = FindTargetUIObject(barRectTransform.transform, "InformationPanel");
        ConfigureInformationPanel(ref informationPanelObject, ref firstText, ref secondText, ref thirdText, element);

        // Increment for spacing
        _barNumber++;
        barGameObject.SetActive(true);

        _objectStates.Add(barGameObject, true);
        _gameObjects.Add(element, barGameObject);
    }
    static void ConfigureVerticalProgressBar(ref GameObject barGameObject, ref Image progressFill, ref Image maxFill, 
        ref LocalizedText level, Profession profession)
    {
        // Instantiate the bar object from the prefab
        barGameObject = UnityEngine.Object.Instantiate(_canvasBase.TargetInfoParent.gameObject);
        // barGameObject = UIHelper.InstantiateGameObject(_canvasBase.TargetInfoParent.gameObject);
        // UIHelper.SetParent(barGameObject.transform, _targetInfoPanelCanvas.transform, false);

        UnityEngine.Object.DontDestroyOnLoad(barGameObject);
        SceneManager.MoveGameObjectToScene(barGameObject, SceneManager.GetSceneByName("VRisingWorld"));

        RectTransform barRectTransform = barGameObject.GetComponent<RectTransform>();
        barRectTransform.SetParent(_targetInfoPanelCanvas.transform, false);
        barRectTransform.gameObject.layer = _layer;

        // Define the number of professions (bars)
        int totalBars = 8;

        // Calculate the total width and height for the bars
        float totalBarAreaWidth = 0.215f; // previous 0.185f
        float barWidth = totalBarAreaWidth / totalBars; // Width of each bar

        // Calculate the starting X position to center the bar graph and position added bars appropriately
        float padding = 1f - 0.075f * 2.45f; // BAR_WIDTH_SPACING previously 0.075f
        float offsetX = padding + barWidth * _graphBarNumber / 1.4f; // previously used 1.5f

        // scale size
        Vector3 updatedScale = new(0.4f, 1f, 1f);
        barRectTransform.localScale = updatedScale;

        // positioning
        float offsetY = 0.24f; // 0.25f previous then 0.24f
        barRectTransform.anchorMin = new Vector2(offsetX, offsetY);
        barRectTransform.anchorMax = new Vector2(offsetX, offsetY);
        barRectTransform.pivot = new Vector2(offsetX, offsetY);

        // Assign fill and level text components
        progressFill = FindTargetUIObject(barRectTransform.transform, "Fill").GetComponent<Image>();
        progressFill.fillMethod = Image.FillMethod.Horizontal;
        progressFill.fillOrigin = 0;
        progressFill.fillAmount = 0f; // This will be set based on profession level
        progressFill.color = ProfessionColors[profession];

        // **Rotate the bar by 90 degrees around the Z-axis**
        barRectTransform.localRotation = Quaternion.Euler(0, 0, 90);

        // Assign and adjust the level text component
        level = FindTargetUIObject(barRectTransform.transform, "LevelText").GetComponent<LocalizedText>();
        GameObject levelBackgroundObject = FindTargetUIObject(barRectTransform.transform, "LevelBackground");

        Image levelBackgroundImage = levelBackgroundObject.GetComponent<Image>();
        Sprite professionIcon = _professionIcons.TryGetValue(profession, out string spriteName) && Sprites.TryGetValue(spriteName, out Sprite sprite) ? sprite : levelBackgroundImage.sprite;
        levelBackgroundImage.sprite = professionIcon ?? levelBackgroundImage.sprite;
        levelBackgroundImage.color = new(1f, 1f, 1f, 1f);
        levelBackgroundObject.transform.localRotation = Quaternion.Euler(0, 0, -90);
        levelBackgroundObject.transform.localScale = new(0.25f, 1f, 1f);

        // Hide unnecessary UI elements
        var headerObject = FindTargetUIObject(barRectTransform.transform, "Name");
        headerObject?.SetActive(false);

        GameObject informationPanelObject = FindTargetUIObject(barRectTransform.transform, "InformationPanel");
        informationPanelObject?.SetActive(false);

        // Set these to 0 so don't appear, deactivating instead seemed funky
        FindTargetUIObject(barRectTransform.transform, "DamageTakenFill").GetComponent<Image>().fillAmount = 0f;
        maxFill = FindTargetUIObject(barRectTransform.transform, "AbsorbFill").GetComponent<Image>();
        maxFill.fillAmount = 0f;
        maxFill.transform.localScale = new(1f, 0.25f, 1f);
        maxFill.color = _brightGold;

        // Increment GraphBarNumber for horizontal spacing within the bar graph
        _graphBarNumber++;

        barGameObject.SetActive(true);
        level.gameObject.SetActive(false);

        _objectStates.Add(barGameObject, true);
        _professionObjects.Add(barGameObject);

        // Keyboard/Mouse layout
        float offsetX_KM = padding + barWidth * _graphBarNumber / 1.4f;
        float offsetY_KM = offsetY;
        Vector2 kmAnchorMin = new(offsetX_KM, offsetY_KM);
        Vector2 kmAnchorMax = new(offsetX_KM, offsetY_KM);
        Vector2 kmPivot = new(offsetX_KM, offsetY_KM);
        Vector2 kmPos = Vector2.zero;

        // Controller layout
        float ctrlBaseX = 0.6175f; // 0.625f previous
        float ctrlSpacingX = 0.015f; // 0.02f previous
        float offsetX_CTRL = ctrlBaseX + _graphBarNumber * ctrlSpacingX;
        float offsetY_CTRL = 0.075f; // 0.1f previous
        Vector2 ctrlAnchorMin = new(offsetX_CTRL, offsetY_CTRL);
        Vector2 ctrlAnchorMax = new(offsetX_CTRL, offsetY_CTRL);
        Vector2 ctrlPivot = new(offsetX_CTRL, offsetY_CTRL);
        Vector2 ctrlPos = Vector2.zero;

        // Register positions
        RegisterAdaptiveElement(
            barGameObject,
            keyboardMousePos: kmPos,
            keyboardMouseAnchorMin: kmAnchorMin,
            keyboardMouseAnchorMax: kmAnchorMax,
            keyboardMousePivot: kmPivot,
            keyboardMouseScale: updatedScale,

            controllerPos: ctrlPos,
            controllerAnchorMin: ctrlAnchorMin,
            controllerAnchorMax: ctrlAnchorMax,
            controllerPivot: ctrlPivot,
            controllerScale: updatedScale * 0.85f
            // controllerScale: updatedScale
        );
    }
    static void ConfigureInformationPanel(ref GameObject informationPanelObject, ref LocalizedText firstText, ref LocalizedText secondText, 
        ref LocalizedText thirdText, UIElement element)
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
    static void ConfigureExperiencePanel(ref GameObject panel, ref LocalizedText firstText, ref LocalizedText secondText, 
        ref LocalizedText thirdText)
    {
        RectTransform panelTransform = panel.GetComponent<RectTransform>();
        Vector2 panelAnchoredPosition = panelTransform.anchoredPosition;
        panelAnchoredPosition.x = -18f;

        firstText = FindTargetUIObject(panel.transform, "BloodInfo").GetComponent<LocalizedText>();
        firstText.ForceSet("");
        firstText.enabled = false;

        GameObject affixesObject = FindTargetUIObject(panel.transform, "Affixes");
        LayoutElement layoutElement = affixesObject.GetComponent<LayoutElement>();
        layoutElement.ignoreLayout = false;

        secondText = affixesObject.GetComponent<LocalizedText>();
        secondText.ForceSet("");
        secondText.Text.fontSize *= 1.2f;
        secondText.enabled = false;

        thirdText = FindTargetUIObject(panel.transform, "PlatformUserName").GetComponent<LocalizedText>();
        thirdText.ForceSet("");
        thirdText.enabled = false;
    }
    static void ConfigureDefaultPanel(ref GameObject panel, ref LocalizedText firstText, ref LocalizedText secondText, 
        ref LocalizedText thirdText)
    {
        RectTransform panelTransform = panel.GetComponent<RectTransform>();
        Vector2 panelAnchoredPosition = panelTransform.anchoredPosition;
        panelAnchoredPosition.x = -18f;

        firstText = FindTargetUIObject(panel.transform, "BloodInfo").GetComponent<LocalizedText>();
        firstText.ForceSet("");
        firstText.Text.fontSize *= 1.1f;
        firstText.enabled = false;

        GameObject affixesObject = FindTargetUIObject(panel.transform, "Affixes");
        LayoutElement layoutElement = affixesObject.GetComponent<LayoutElement>();
        layoutElement.ignoreLayout = false;

        secondText = affixesObject.GetComponent<LocalizedText>();
        secondText.ForceSet("");
        secondText.Text.fontSize *= 1.1f;
        secondText.enabled = false;

        thirdText = FindTargetUIObject(panel.transform, "PlatformUserName").GetComponent<LocalizedText>();
        thirdText.ForceSet("");
        thirdText.Text.fontSize *= 1.1f;
        thirdText.enabled = false;
    }
    static float ClassSynergy<T>(T statType, PlayerClass classType, Dictionary<PlayerClass, (List<WeaponStatType> WeaponStatTypes, List<BloodStatType> BloodStatTypes)> classStatSynergy)
    {
        if (classType.Equals(PlayerClass.None))
            return 1f;

        // Check if the stat type exists in the class synergies for the current class
        if (typeof(T) == typeof(WeaponStatType) && classStatSynergy[classType].WeaponStatTypes.Contains((WeaponStatType)(object)statType))
        {
            return _classStatMultiplier;
        }
        else if (typeof(T) == typeof(BloodStatType) && classStatSynergy[classType].BloodStatTypes.Contains((BloodStatType)(object)statType))
        {
            return _classStatMultiplier;
        }

        return 1f; // Return default multiplier if stat is not present in the class synergy
    }
    static string FormatWeaponStat(WeaponStatType weaponStat, float statValue)
    {
        string statValueString = WeaponStatFormats[weaponStat] switch
        {
            "integer" => ((int)statValue).ToString(),
            "decimal" => statValue.ToString("F2"),
            "percentage" => (statValue * 100f).ToString("F1") + "%",
            _ => statValue.ToString(),
        };

        string displayString = $"<color=#00FFFF>{WeaponStatTypeAbbreviations[weaponStat]}</color>: <color=#90EE90>{statValueString}</color>";
        // Core.Log.LogWarning($"FormatWeaponStat - {weaponStat} - {displayString}");
        return displayString;
    }
    static string IntegerToRoman(int num)
    {
        string result = string.Empty;

        foreach (var item in _romanNumerals)
        {
            while (num >= item.Key)
            {
                result += item.Value;
                num -= item.Key;
            }
        }

        return result;
    }
    public static void FindSprites()
    {
        Il2CppArrayBase<Sprite> sprites = UnityEngine.Resources.FindObjectsOfTypeAll<Sprite>();

        foreach (Sprite sprite in sprites)
        {
            if (_spriteNames.Contains(sprite.name) && !Sprites.ContainsKey(sprite.name))
            {
                _sprites[sprite.name] = sprite;

                if (sprite.name.Equals("BloodIcon_Cursed") && _questKillVBloodUnit == null)
                {
                    _questKillVBloodUnit = sprite;
                }

                if (sprite.name.Equals("BloodIcon_Warrior") && _questKillStandardUnit == null)
                {
                    _questKillStandardUnit = sprite;
                }
            }
        }
    }
    static string TrimToFirstWord(string name)
    {
        int firstSpaceIndex = name.IndexOf(' ');
        int secondSpaceIndex = name.IndexOf(' ', firstSpaceIndex + 1);

        // Only trim if there are at least two spaces (i.e., three words)
        if (firstSpaceIndex > 0 && secondSpaceIndex > 0)
        {
            return name[..firstSpaceIndex];
        }

        return name;
    }
    public static void ResetState()
    {
        foreach (GameObject gameObject in _objectStates.Keys)
        {
            if (gameObject != null)
            {
                UnityEngine.Object.Destroy(gameObject);
            }
        }

        foreach (GameObject gameObject in _professionObjects)
        {
            if (gameObject != null)
            {
                UnityEngine.Object.Destroy(gameObject);
            }
        }

        _objectStates.Clear();
        _professionObjects.Clear();
        _gameObjects.Clear();
        _adaptiveElements.Clear();

        _sprites.Clear();
    }

    static ControllerType _controllerType = ControllerType.KeyboardAndMouse;
    struct InputAdaptiveElement
    {
        public GameObject AdaptiveObject;

        // Keyboard/Mouse layout
        public Vector2 KeyboardMouseAnchoredPosition;
        public Vector2 KeyboardMouseAnchorMin;
        public Vector2 KeyboardMouseAnchorMax;
        public Vector2 KeyboardMousePivot;
        public Vector3 KeyboardMouseScale;

        // Controller layout
        public Vector2 ControllerAnchoredPosition;
        public Vector2 ControllerAnchorMin;
        public Vector2 ControllerAnchorMax;
        public Vector2 ControllerPivot;
        public Vector3 ControllerScale;
    }

    static readonly List<InputAdaptiveElement> _adaptiveElements = [];
    public static void RegisterAdaptiveElement(
        GameObject adaptiveObject,
        Vector2 keyboardMousePos, Vector2 keyboardMouseAnchorMin, Vector2 keyboardMouseAnchorMax, Vector2 keyboardMousePivot, Vector3 keyboardMouseScale,
        Vector2 controllerPos, Vector2 controllerAnchorMin, Vector2 controllerAnchorMax, Vector2 controllerPivot, Vector3 controllerScale)
    {
        if (adaptiveObject == null) return;

        _adaptiveElements.Add(new InputAdaptiveElement
        {
            AdaptiveObject = adaptiveObject,

            KeyboardMouseAnchoredPosition = keyboardMousePos,
            KeyboardMouseAnchorMin = keyboardMouseAnchorMin,
            KeyboardMouseAnchorMax = keyboardMouseAnchorMax,
            KeyboardMousePivot = keyboardMousePivot,
            KeyboardMouseScale = keyboardMouseScale,

            ControllerAnchoredPosition = controllerPos,
            ControllerAnchorMin = controllerAnchorMin,
            ControllerAnchorMax = controllerAnchorMax,
            ControllerPivot = controllerPivot,
            ControllerScale = controllerScale
        });
    }
    public static void SyncAdaptiveElements(bool isGamepad)
    {
        _controllerType = isGamepad ? ControllerType.Gamepad : ControllerType.KeyboardAndMouse;
        Core.Log.LogWarning($"[OnInputDeviceChanged] - ControllerType: {_controllerType}");

        foreach (InputAdaptiveElement adaptiveElement in _adaptiveElements)
        {
            RectTransform rectTransform = adaptiveElement.AdaptiveObject.GetComponent<RectTransform>();

            if (isGamepad)
            {
                rectTransform.anchorMin = adaptiveElement.ControllerAnchorMin;
                rectTransform.anchorMax = adaptiveElement.ControllerAnchorMax;
                rectTransform.pivot = adaptiveElement.ControllerPivot;
                rectTransform.anchoredPosition = adaptiveElement.ControllerAnchoredPosition;
                rectTransform.localScale = adaptiveElement.ControllerScale;
            }
            else
            {
                rectTransform.anchorMin = adaptiveElement.KeyboardMouseAnchorMin;
                rectTransform.anchorMax = adaptiveElement.KeyboardMouseAnchorMax;
                rectTransform.pivot = adaptiveElement.KeyboardMousePivot;
                rectTransform.anchoredPosition = adaptiveElement.KeyboardMouseAnchoredPosition;
                rectTransform.localScale = adaptiveElement.KeyboardMouseScale;
            }
        }
    }
}
