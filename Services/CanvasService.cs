using Bloodcraft.Resources;
using Eclipse.Patches;
using Eclipse.Utilities;
using Il2CppInterop.Runtime;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using ProjectM;
using ProjectM.Shared;
using ProjectM.UI;
using Stunlock.Core;
using StunShared.UI;
using System.Collections;
using System.Text.RegularExpressions;
using TMPro;
using Unity.Entities;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using static Eclipse.Services.CanvasService.GameObjectUtilities;
using static Eclipse.Services.DataService;
using Image = UnityEngine.UI.Image;
using StringComparison = System.StringComparison;

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

    /*
    public static readonly Dictionary<PrefabGUID, Entity> PrefabEntityCache = [];
    public static readonly Dictionary<Entity, Dictionary<UnitStatType, float>> WeaponEntityStatCache = [];
    public static readonly Dictionary<PrefabGUID, Dictionary<UnitStatType, float>> WeaponStatCache = [];
    // static readonly Dictionary<PrefabGUID, Dictionary<UnitStatType, float>> _blacksmithingStatCache = []; throwing in towel on this for now x_x
    public static readonly Dictionary<PrefabGUID, Dictionary<UnitStatType, float>> OriginalWeaponStatsCache = [];

    public static readonly Dictionary<PrefabGUID, Dictionary<UnitStatType, float>> GrimoireStatCache = [];
    public static readonly Dictionary<PrefabGUID, Dictionary<UnitStatType, float>> OriginalGrimoireStatsCache = [];

    public static readonly Dictionary<PrefabGUID, Dictionary<UnitStatType, float>> ArmorStatCache = [];
    public static readonly Dictionary<PrefabGUID, Dictionary<UnitStatType, float>> OriginalArmorStatsCache = [];
    */

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
                int index = _uiElementIndices[keyValuePair.Key];
                GameObject abilitySlotObject = GameObject.Find(_abilitySlotNamePaths[keyValuePair.Key]);

                if (abilitySlotObject != null && _gameObjects.TryGetValue(keyValuePair.Key, out GameObject gameObject))
                {
                    SimpleStunButton stunButton = abilitySlotObject.AddComponent<SimpleStunButton>();
                    GameObject capturedObject = gameObject;

                    if (!keyValuePair.Key.Equals(UIElement.Professions)) stunButton.onClick.AddListener((UnityAction)(() => ToggleGameObject(capturedObject)));
                    else if (_actionToggles.TryGetValue(index, out var toggleAction))
                    {
                        stunButton.onClick.AddListener(new Action(toggleAction));
                    }
                }
            }
        }
    }

    /*
    static readonly Dictionary<UIElement, (Vector2 mouseAndKeyboard, Vector2 controllerPosition)> _elementPositions = [];
    public static void HandleInputDeviceChange(bool useController)
    {
        // For each UIElement that is Daily or Weekly, set its RectTransform
        foreach (var kvp in _questWindowPositionMap)
        {
            UIElement elementType = kvp.Key;
            (Vector2 keyboardPos, Vector2 controllerPos) = kvp.Value;

            // We expect _gameObjects[elementType] to be the quest GameObject
            if (_gameObjects.TryGetValue(elementType, out GameObject questGo))
            {
                RectTransform rt = questGo.GetComponent<RectTransform>();
                if (rt != null)
                {
                    Vector2 target = useController ? controllerPos : keyboardPos;
                    rt.anchoredPosition = target;
                }
            }
        }
    }
    */
    static void ToggleGameObject(GameObject gameObject)
    {
        bool active = !gameObject.activeSelf;
        gameObject.SetActive(active);
        _objectStates[gameObject] = active;
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
        foreach (GameObject professionObject in _objectStates.Keys)
        {
            if (_professionObjects.Contains(professionObject))
            {
                bool active = !professionObject.activeSelf;

                professionObject.SetActive(active);
                _objectStates[professionObject] = active;
            }
            else
            {
                Core.Log.LogWarning($"Profession object not found!");
            }
        }
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

            /*
            if (!abilityGroupEntity.Has<VBloodAbilityData>() && abilityCastEntity.TryGetComponent(out AbilityCooldownState abilityCooldownState))
            {
                _cooldownTime = abilityCooldownState.CurrentCooldown;
            }
            */
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

                _chargeCooldownFillImage.fillAmount = 1 - (_cooldownRemaining / _cooldownTime);

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

                _chargeCooldownFillImage.fillAmount = 1f - (_cooldownRemaining / _cooldownTime);

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
                        // Core.Log.LogWarning($"AbilityTooltipData refreshed from AbilityGroupState, updating ability data! {currentPrefabGUID}");
                        UpdateAbilityData(_abilityTooltipData, abilityGroupEntity, abilityCastEntity, currentPrefabGUID);
                    }
                    else if (_abilityTooltipData != null)
                    {
                        // Core.Log.LogWarning($"AbilityTooltipData unchanged from AbilityGroupState, updating ability data! {currentPrefabGUID}");
                        UpdateAbilityData(_abilityTooltipData, abilityGroupEntity, abilityCastEntity, currentPrefabGUID);
                    }
                }

                if (_abilityTooltipData != null)
                {
                    // Core.Log.LogWarning($"AbilityTooltipData exists, updating ability state!");
                    UpdateAbilityState(abilityGroupEntity, abilityCastEntity);
                }
            }
            else
            {
                // Core.Log.LogWarning($"AbilityBar_Shared not found!");
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
                statValue *= (1 + (_prestigeStatMultiplier * _expertisePrestige)) * classMultiplier * ((float)_expertiseLevel / _expertiseMaxLevel);
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
                statValue *= ((1 + (_prestigeStatMultiplier * _legacyPrestige)) * classMultiplier * ((float)_legacyLevel / _legacyMaxLevel));
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

            /*
            if (targetType.Equals(TargetType.Kill))
            {
                if (!questIcon.gameObject.active) questIcon.gameObject.active = true;

                if (isVBlood && questIcon.sprite.name != "BloodIcon_Cursed" && Sprites.TryGetValue("BloodIcon_Cursed", out Sprite vBloodSprite))
                {
                    questIcon.sprite = vBloodSprite;
                }
                else if (!isVBlood && questIcon.sprite.name != "BloodIcon_Warrior" && Sprites.TryGetValue("BloodIcon_Warrior", out Sprite unitSprite))
                {
                    questIcon.sprite = unitSprite;
                }
            }
            else if (targetType.Equals(TargetType.Craft))
            {
                if (!questIcon.gameObject.active) questIcon.gameObject.active = true;

                PrefabGUID targetPrefabGUID = LocalizationService.GetPrefabGuidFromName(target);
                ManagedItemData managedItemData = ManagedDataRegistry.GetOrDefault<ManagedItemData>(targetPrefabGUID);

                if (managedItemData != null && questIcon.sprite.name != managedItemData.Icon.name)
                {
                    questIcon.sprite = managedItemData.Icon;
                }
            }
            else if (targetType.Equals(TargetType.Gather))
            {
                if (!questIcon.gameObject.active) questIcon.gameObject.active = true;

                PrefabGUID targetPrefabGUID = LocalizationService.GetPrefabGuidFromName(target);
                ManagedItemData managedItemData = ManagedDataRegistry.GetOrDefault<ManagedItemData>(targetPrefabGUID);

                if (managedItemData != null && questIcon.sprite.name != managedItemData.Icon.name)
                {
                    questIcon.sprite = managedItemData.Icon;
                }
            }
            */

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
            shiftSlotObject = GameObject.Instantiate(abilityDummyObject);
            RectTransform rectTransform = shiftSlotObject.GetComponent<RectTransform>();

            RectTransform abilitiesTransform = GameObject.Find("HUDCanvas(Clone)/BottomBarCanvas/BottomBar(Clone)/Content/Background/AbilityBar/Abilities/").GetComponent<RectTransform>();

            GameObject.DontDestroyOnLoad(shiftSlotObject);
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
        questObject = GameObject.Instantiate(_canvasBase.BottomBarParentPrefab.FakeTooltip.gameObject);
        RectTransform questTransform = questObject.GetComponent<RectTransform>();

        // Prevent quest window from being destroyed on scene load and move to scene
        GameObject.DontDestroyOnLoad(questObject);
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

        // Size window and set anchors
        questTransform.sizeDelta = new Vector2(questTransform.sizeDelta.x * 0.65f, questTransform.sizeDelta.y);
        questTransform.anchorMin = new Vector2(1, _windowOffset); // Anchored to bottom-right
        questTransform.anchorMax = new Vector2(1, _windowOffset);
        questTransform.pivot = new Vector2(1, _windowOffset);
        questTransform.anchoredPosition = new Vector2(0, _windowOffset);

        // Set header text
        header.ForceSet(questType.ToString() + " Quest");
        subHeader.ForceSet("UnitName: 0/0");

        // Add to active objects
        _gameObjects.Add(questType, questObject);
        _objectStates.Add(questObject, true);
        _windowOffset += 0.075f;
    }
    static void ConfigureHorizontalProgressBar(ref GameObject barGameObject, ref GameObject informationPanelObject, ref Image fill, 
        ref LocalizedText level, ref LocalizedText header, UIElement element, Color fillColor, 
        ref LocalizedText firstText, ref LocalizedText secondText, ref LocalizedText thirdText)
    {
        // Instantiate the bar object from the prefab
        barGameObject = GameObject.Instantiate(_canvasBase.TargetInfoParent.gameObject);
        // barGameObject = UIHelper.InstantiateGameObjectUnderAnchor(_canvasBase.TargetInfoParent.gameObject, _targetInfoPanelCanvas.transform);
        // barGameObject = UIHelper.InstantiateGameObject(_canvasBase.TargetInfoParent.gameObject);
        // UIHelper.SetParent(barGameObject.transform, _targetInfoPanelCanvas.transform, false);

        // DontDestroyOnLoad, change scene
        GameObject.DontDestroyOnLoad(barGameObject);
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
        barGameObject = GameObject.Instantiate(_canvasBase.TargetInfoParent.gameObject);
        // barGameObject = UIHelper.InstantiateGameObject(_canvasBase.TargetInfoParent.gameObject);
        // UIHelper.SetParent(barGameObject.transform, _targetInfoPanelCanvas.transform, false);

        GameObject.DontDestroyOnLoad(barGameObject);
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
        float padding = 1f - (0.075f * 2.45f); // BAR_WIDTH_SPACING previously 0.075f
        float offsetX = padding + (barWidth * _graphBarNumber / 1.4f); // previously used 1.5f

        // scale size
        Vector3 updatedScale = new(0.4f, 1f, 1f);
        barRectTransform.localScale = updatedScale;

        // positioning
        float offsetY = 0.24f; // try 0.24f if needs adjusting? 0.235f previous
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

        // font size for later reference
        // _fontSize = level.Text.fontSize;
        // level.Text.fontSize *= 1.2f;

        // Get text container and rotate back
        // GameObject levelTextContainer = FindTargetUIObject(barRectTransform.transform, "LevelDiff");
        // RectTransform levelTextContainerRectTransform = levelTextContainer.GetComponent<RectTransform>();
        // levelTextContainerRectTransform.rotation = Quaternion.identity;
        // levelTextContainerRectTransform.localRotation = Quaternion.Euler(0, 0, -90);

        // LevelBackground scale set back
        GameObject levelBackgroundObject = FindTargetUIObject(barRectTransform.transform, "LevelBackground");
        // GameObject highlightObject = GameObject.Instantiate(levelBackgroundObject);

        Image levelBackgroundImage = levelBackgroundObject.GetComponent<Image>();
        Sprite professionIcon = _professionIcons.TryGetValue(profession, out string spriteName) && Sprites.TryGetValue(spriteName, out Sprite sprite) ? sprite : levelBackgroundImage.sprite;
        levelBackgroundImage.sprite = professionIcon ?? levelBackgroundImage.sprite;
        levelBackgroundImage.color = new(1f, 1f, 1f, 1f);
        levelBackgroundObject.transform.localRotation = Quaternion.Euler(0, 0, -90);
        levelBackgroundObject.transform.localScale = new(0.25f, 1f, 1f);

        /*
        // add gameObject with image to use as coloring fill effect for icon
        highlightObject.transform.SetParent(levelBackgroundObject.transform.parent, false);
        highlightObject.transform.SetSiblingIndex(levelBackgroundObject.transform.GetSiblingIndex() - 1);

        iconFill = highlightObject.GetComponent<Image>();
        iconFill.sprite = professionIcon ?? levelBackgroundImage.sprite;
        // highlightObject.transform.rotation = Quaternion.identity;
        // iconFill.type = Image.Type.Filled;
        // iconFill.fillMethod = Image.FillMethod.Vertical;
        // iconFill.fillOrigin = 2;
        // iconFill.fillAmount = 0.5f;
        highlightObject?.SetActive(true);
        */

        // Hide unnecessary UI elements
        var headerObject = FindTargetUIObject(barRectTransform.transform, "Name");
        headerObject?.SetActive(false);

        GameObject informationPanelObject = FindTargetUIObject(barRectTransform.transform, "InformationPanel");
        informationPanelObject?.SetActive(false);

        // Set these to 0 so don't appear, deactivating instead seemed funky
        FindTargetUIObject(barRectTransform.transform, "DamageTakenFill").GetComponent<Image>().fillAmount = 0f;
        //FindTargetUIObject(barRectTransform.transform, "AbsorbFill").GetComponent<Image>().fillAmount = 0f;
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

            Core.Log.LogWarning($"GameObject with name '{targetName}' not found!");
            return null;
        }
        public static void FindLoadedObjects<T>() where T : UnityEngine.Object
        {
            Il2CppReferenceArray<UnityEngine.Object> resources = UnityEngine.Resources.FindObjectsOfTypeAll(Il2CppType.Of<T>());
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
            List<Transform> transforms = [..children];

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

            Core.Log.LogWarning($"Found {transforms.Count} GameObjects!");

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
                    Core.Log.LogInfo($"{indent}{current.gameObject.name} | {string.Join(",", objectComponents)} | [{current.gameObject.scene.name}]");

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
        foreach (GameObject gameObject in CanvasService._objectStates.Keys)
        {
            if (gameObject != null)
            {
                GameObject.Destroy(gameObject);
            }
        }

        foreach (GameObject gameObject in CanvasService._professionObjects)
        {
            if (gameObject != null)
            {
                GameObject.Destroy(gameObject);
            }
        }

        _objectStates.Clear();
        _professionObjects.Clear();
        _gameObjects.Clear();

        _sprites.Clear();
    }
}

/*
    static GameObject DropdownMenu;
    static TMP_Dropdown DropdownSelection;
    static List<string> Selections = ["1","2","3"];
    static LocalizedText DropdownText;
    static void ConfigureFamiliarObject()
    {
        
        OptionsPanel_Interface optionsPanelInterface = GameObject.FindObjectOfType<OptionsPanel_Interface>();

        if (optionsPanelInterface != null)
        {
            Core.Log.LogInfo("OptionsPanel_Interface found!");

            familiarObject = new GameObject("FamiliarObject");

            GameObject.DontDestroyOnLoad(familiarObject);
            SceneManager.MoveGameObjectToScene(familiarObject, SceneManager.GetSceneByName("VRisingWorld"));

            RectTransform familiarTransform = familiarObject.AddComponent<RectTransform>();
            familiarTransform.SetParent(Canvas.transform, false);
            familiarObject.layer = Layer;

            familiarTransform.anchorMin = new Vector2(0.5f, 0.5f);
            familiarTransform.anchorMax = new Vector2(0.5f, 0.5f);
            familiarTransform.pivot = new Vector2(0.5f, 0.5f);

            List<string> stringOptions = ["Box1", "Box2", "Box3"];
            Il2CppSystem.Collections.Generic.List<string> testOptions = new(stringOptions.Count);

            foreach (string testOption in stringOptions)
            {
                testOptions.Add(testOption);
            }

            OptionsHelper.AddDropdown(familiarTransform, optionsPanelInterface.DropdownPrefab, false, LocalizationKey.Empty, LocalizationKey.Empty, testOptions, 1, 2, new Action<int>(OnDropDownChanged));
            familiarObject.SetActive(true);
        }
        else
        {
            Core.Log.LogInfo("OptionsPanel_Interface not found...");
        }
        

        try
        {
            Core.Log.LogInfo("Creating dropdown menu...");
            GameObject dropdownMenuObject = new("DropdownMenu");
DropdownMenu = dropdownMenuObject;
            RectTransform dropdownRectTransform = dropdownMenuObject.AddComponent<RectTransform>();

Core.Log.LogInfo("Making persistent and moving to scene before setting parent...");
            GameObject.DontDestroyOnLoad(dropdownMenuObject);
            SceneManager.MoveGameObjectToScene(dropdownMenuObject, SceneManager.GetSceneByName("VRisingWorld"));
            dropdownRectTransform.SetParent(Canvas.transform, false);

            Core.Log.LogInfo("Adding Dropdown component...");
            TMP_Dropdown dropdownMenu = dropdownMenuObject.AddComponent<TMP_Dropdown>();
DropdownSelection = dropdownMenu;

            Core.Log.LogInfo("Setting dropdown position/anchors...");
            dropdownRectTransform.anchorMin = new Vector2(0.5f, 0.5f);
dropdownRectTransform.anchorMax = new Vector2(0.5f, 0.5f);
dropdownRectTransform.pivot = new Vector2(0.5f, 0.5f);
dropdownRectTransform.anchoredPosition = Vector2.zero;
            dropdownRectTransform.sizeDelta = new Vector2(160f, 30f);

// Caption Text
Core.Log.LogInfo("Creating caption text...");
            GameObject captionObject = new("CaptionText");
TextMeshProUGUI dropdownText = captionObject.AddComponent<TextMeshProUGUI>();
dropdownText.text = "Familiar Boxes";
            RectTransform captionRectTransform = captionObject.GetComponent<RectTransform>();

GameObject.DontDestroyOnLoad(captionObject);
            SceneManager.MoveGameObjectToScene(captionObject, SceneManager.GetSceneByName("VRisingWorld"));
            captionObject.transform.SetParent(dropdownMenu.transform, false);

            Core.Log.LogInfo("Setting caption anchors and size...");
            captionRectTransform.anchorMin = new Vector2(0, 0.5f);
captionRectTransform.anchorMax = new Vector2(1, 0.5f);
captionRectTransform.pivot = new Vector2(0.5f, 0.5f);
captionRectTransform.sizeDelta = new Vector2(0, 30f);
captionRectTransform.anchoredPosition = Vector2.zero;

            Core.Log.LogInfo("Setting caption text properties...");
            dropdownText.font = ExperienceClassText.Text.font;
            dropdownText.fontSize = (int) ExperienceClassText.Text.fontSize;
dropdownText.color = ExperienceClassText.Text.color;
            dropdownText.alignment = TextAlignmentOptions.Center;

            // Create Dropdown Template
            Core.Log.LogInfo("Creating dropdown template...");
            GameObject templateObject = new("Template");
RectTransform templateRectTransform = templateObject.AddComponent<RectTransform>();
templateObject.transform.SetParent(dropdownMenuObject.transform, false);

            templateRectTransform.anchorMin = new Vector2(0, 0);
templateRectTransform.anchorMax = new Vector2(1, 0);
templateRectTransform.pivot = new Vector2(0.5f, 1f);
templateRectTransform.sizeDelta = new Vector2(0, 90f);

// Add background to the template
Core.Log.LogInfo("Adding background to template...");
            Image templateBackground = templateObject.AddComponent<Image>();
templateBackground.color = new Color(0, 0, 0, 0.5f);

// Create Viewport
GameObject viewportObject = new("Viewport");
RectTransform viewportRectTransform = viewportObject.AddComponent<RectTransform>();
viewportObject.transform.SetParent(templateObject.transform, false);

            viewportRectTransform.anchorMin = Vector2.zero;
            viewportRectTransform.anchorMax = Vector2.one;
            viewportRectTransform.sizeDelta = Vector2.zero;

            Mask viewportMask = viewportObject.AddComponent<Mask>();
viewportMask.showMaskGraphic = false;

            // Create Content
            Core.Log.LogInfo("Creating content object...");
            GameObject contentObject = new("Content");
RectTransform contentRectTransform = contentObject.AddComponent<RectTransform>();
contentObject.transform.SetParent(viewportObject.transform, false);

            contentRectTransform.anchorMin = new Vector2(0, 1);
contentRectTransform.anchorMax = new Vector2(1, 1);
contentRectTransform.pivot = new Vector2(0.5f, 1f);
contentRectTransform.sizeDelta = new Vector2(0, 90f);

// Create Item Template
GameObject itemObject = new("Item");
itemObject.transform.SetParent(contentObject.transform, false);

            RectTransform itemRectTransform = itemObject.AddComponent<RectTransform>();
itemRectTransform.anchorMin = new Vector2(0, 0.5f);
itemRectTransform.anchorMax = new Vector2(1, 0.5f);
itemRectTransform.sizeDelta = new Vector2(0, 25f);

// Add Toggle to the item
Core.Log.LogInfo("Adding toggle to item...");
            Toggle itemToggle = itemObject.AddComponent<Toggle>();
itemToggle.isOn = false;

            // Create 'Item Background'
            GameObject itemBackgroundObject = new("ItemBackground");
itemBackgroundObject.transform.SetParent(itemObject.transform, false);

            RectTransform itemBackgroundRect = itemBackgroundObject.AddComponent<RectTransform>();
itemBackgroundRect.anchorMin = Vector2.zero;
            itemBackgroundRect.anchorMax = Vector2.one;
            itemBackgroundRect.sizeDelta = Vector2.zero;

            Image itemBackgroundImage = itemBackgroundObject.AddComponent<Image>();
itemBackgroundImage.color = new Color(1, 1, 1, 1);

// Create 'Item Checkmark'
GameObject itemCheckmarkObject = new("ItemCheckmark");
itemCheckmarkObject.transform.SetParent(itemBackgroundObject.transform, false);

            RectTransform itemCheckmarkRect = itemCheckmarkObject.AddComponent<RectTransform>();
itemCheckmarkRect.anchorMin = new Vector2(0, 0.5f);
itemCheckmarkRect.anchorMax = new Vector2(0, 0.5f);
itemCheckmarkRect.pivot = new Vector2(0.5f, 0.5f);
itemCheckmarkRect.sizeDelta = new Vector2(20, 20);
itemCheckmarkRect.anchoredPosition = new Vector2(10, 0);

Image itemCheckmarkImage = itemCheckmarkObject.AddComponent<Image>();
// Assign a sprite to the checkmark image if available
// itemCheckmarkImage.sprite = yourCheckmarkSprite;

// Create 'Item Label'
GameObject itemLabelObject = new("ItemLabel");
itemLabelObject.transform.SetParent(itemBackgroundObject.transform, false);

            RectTransform itemLabelRect = itemLabelObject.AddComponent<RectTransform>();
itemLabelRect.anchorMin = new Vector2(0, 0);
itemLabelRect.anchorMax = new Vector2(1, 1);
itemLabelRect.offsetMin = new Vector2(20, 0);
itemLabelRect.offsetMax = new Vector2(0, 0);

TextMeshProUGUI itemLabelText = itemLabelObject.AddComponent<TextMeshProUGUI>();
itemLabelText.font = ExperienceClassText.Text.font;
            itemLabelText.fontSize = (int) ExperienceClassText.Text.fontSize;
itemLabelText.color = ExperienceClassText.Text.color;
            itemLabelText.alignment = TextAlignmentOptions.Left;
            itemLabelText.text = "Option";

            // Configure the Toggle component
            itemToggle.targetGraphic = itemBackgroundImage;
            itemToggle.graphic = itemCheckmarkImage;
            itemToggle.isOn = false;

            // Assign the itemText property of the dropdown
            dropdownMenu.itemText = itemLabelText;

            // Add ScrollRect to the template
            ScrollRect scrollRect = templateObject.AddComponent<ScrollRect>();
scrollRect.content = contentRectTransform;
            scrollRect.viewport = viewportRectTransform;
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            scrollRect.movementType = ScrollRect.MovementType.Clamped;
            scrollRect.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHideAndExpandViewport;

            // Disable the item template and template by default
            itemObject.SetActive(false);
            templateObject.SetActive(false);

            // Set layers
            itemObject.layer = Canvas.gameObject.layer;
            itemBackgroundObject.layer = Canvas.gameObject.layer;
            itemCheckmarkObject.layer = Canvas.gameObject.layer;
            itemLabelObject.layer = Canvas.gameObject.layer;

            // Remove redundant addition of TextMeshProUGUI to itemObject

            Core.Log.LogInfo("Adding background to dropdown...");
            Image dropdownImage = dropdownMenuObject.AddComponent<Image>();
dropdownImage.color = new Color(0, 0, 0, 0.5f);
dropdownImage.type = Image.Type.Sliced;

            // Assign properties to the dropdown
            dropdownMenu.template = templateRectTransform;
            dropdownMenu.targetGraphic = dropdownImage;
            dropdownMenu.captionText = dropdownText;
            // dropdownMenu.itemText is already assigned above

            Core.Log.LogInfo("Setting initial dropdown options...");
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
dropdownMenuObject.layer = Canvas.gameObject.layer;
dropdownMenuObject.SetActive(true);

            
            Core.Log.LogInfo("Creating dropdown menu...");
            // might need to use own canvas for this if BottomBarParent throws a fit which it probably will
            GameObject dropdownMenuObject = new("DropdownMenu");
            DropdownMenu = dropdownMenuObject;
            RectTransform dropdownRectTransform = dropdownMenuObject.AddComponent<RectTransform>();

            Core.Log.LogInfo("Making persistent and moving to scene before setting parent...");
            // DontDestroyOnLoad, move to proper scene, set canvas as parent
            GameObject.DontDestroyOnLoad(dropdownMenuObject);
            SceneManager.MoveGameObjectToScene(dropdownMenuObject, SceneManager.GetSceneByName("VRisingWorld"));
            dropdownRectTransform.SetParent(Canvas.transform, false);

            Core.Log.LogInfo("Adding Dropdown component...");
            // Add dropdown components and configure position, starting state, etc
            TMP_Dropdown dropdownMenu = dropdownMenuObject.AddComponent<TMP_Dropdown>();
            DropdownSelection = dropdownMenu;

            Core.Log.LogInfo("Setting dropdown position/anchors...");     
            dropdownRectTransform.anchorMin = new Vector2(0.5f, 0.5f);  // Centered
            dropdownRectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            dropdownRectTransform.pivot = new Vector2(0.5f, 0.5f);
            dropdownRectTransform.anchoredPosition = Vector2.zero;  // Centered on screen
            dropdownRectTransform.sizeDelta = new Vector2(160f, 30f);  // Test with a larger size

            // captionText
            Core.Log.LogInfo("Creating caption text...");
            GameObject captionObject = new("CaptionText");
            TextMeshProUGUI dropdownText = captionObject.AddComponent<TextMeshProUGUI>();
            dropdownText.text = "Familiar Boxes";
            RectTransform captionRectTransform = captionObject.AddComponent<RectTransform>();

            // DontDestroyOnLoad, move to proper scene, set canvas as parent
            GameObject.DontDestroyOnLoad(captionObject);
            SceneManager.MoveGameObjectToScene(captionObject, SceneManager.GetSceneByName("VRisingWorld"));
            captionObject.transform.SetParent(dropdownMenu.transform, false);

            // Anchor the text to stretch across the width of the dropdown
            Core.Log.LogInfo("Setting caption anchors and size...");
            captionRectTransform.anchorMin = new Vector2(0, 0.5f);  // Anchored to the middle of the dropdown
            captionRectTransform.anchorMax = new Vector2(1, 0.5f);  // Stretched horizontally
            captionRectTransform.pivot = new Vector2(0.5f, 0.5f);   // Center pivot

            // Set size and position relative to dropdown
            captionRectTransform.sizeDelta = new Vector2(0, 30f);   // Matches dropdown height (30 units)
            captionRectTransform.anchoredPosition = new Vector2(0, 0);  // Centered within dropdown

            // Configure the font, font size, and other properties
            Core.Log.LogInfo("Setting caption text properties...");
            dropdownText.font = ExperienceClassText.Text.font;
            dropdownText.fontSize = (int)ExperienceClassText.Text.fontSize;
            dropdownText.color = ExperienceClassText.Text.color;
            dropdownText.alignment = TextAlignmentOptions.Center;

            // Create Dropdown Template (needed for displaying options)
            Core.Log.LogInfo("Creating dropdown template...");
            GameObject templateObject = new("Template");
            RectTransform templateRectTransform = templateObject.AddComponent<RectTransform>();
            templateObject.transform.SetParent(dropdownMenuObject.transform, false);

            // Set up the template’s size and positioning
            templateRectTransform.anchorMin = new Vector2(0, 0);
            templateRectTransform.anchorMax = new Vector2(1, 0);
            templateRectTransform.pivot = new Vector2(0.5f, 1f);
            templateRectTransform.sizeDelta = new Vector2(0, 90f);  // Size for showing options

            // Add a background to the template (optional for styling)
            Core.Log.LogInfo("Adding background to template...");
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
            Core.Log.LogInfo("Creating content object...");
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
            Core.Log.LogInfo("Adding toggle to item...");
            Toggle itemToggle = itemObject.AddComponent<Toggle>();
            itemToggle.isOn = false;  // Default to off

            // Create 'Item Background' GameObject
            GameObject itemBackgroundObject = new("ItemBackground");
            itemBackgroundObject.transform.SetParent(itemObject.transform, false);

            RectTransform itemBackgroundRect = itemBackgroundObject.AddComponent<RectTransform>();
            itemBackgroundRect.anchorMin = Vector2.zero;
            itemBackgroundRect.anchorMax = Vector2.one;
            itemBackgroundRect.sizeDelta = Vector2.zero;

            Image itemBackgroundImage = itemBackgroundObject.AddComponent<Image>();
            itemBackgroundImage.color = new Color(1, 1, 1, 1); // White background

            // Create 'Item Checkmark' GameObject
            GameObject itemCheckmarkObject = new("ItemCheckmark");
            itemCheckmarkObject.transform.SetParent(itemBackgroundObject.transform, false);

            RectTransform itemCheckmarkRect = itemCheckmarkObject.AddComponent<RectTransform>();
            itemCheckmarkRect.anchorMin = new Vector2(0, 0.5f);
            itemCheckmarkRect.anchorMax = new Vector2(0, 0.5f);
            itemCheckmarkRect.pivot = new Vector2(0.5f, 0.5f);
            itemCheckmarkRect.sizeDelta = new Vector2(20, 20);
            itemCheckmarkRect.anchoredPosition = new Vector2(10, 0);

            Image itemCheckmarkImage = itemCheckmarkObject.AddComponent<Image>();
            // Assign a sprite to the checkmark image if available
            // itemCheckmarkImage.sprite = yourCheckmarkSprite;

            // Create 'Item Label' GameObject
            GameObject itemLabelObject = new("ItemLabel");
            itemLabelObject.transform.SetParent(itemBackgroundObject.transform, false);

            RectTransform itemLabelRect = itemLabelObject.AddComponent<RectTransform>();
            itemLabelRect.anchorMin = new Vector2(0, 0);
            itemLabelRect.anchorMax = new Vector2(1, 1);
            itemLabelRect.offsetMin = new Vector2(20, 0); // Left padding
            itemLabelRect.offsetMax = new Vector2(0, 0);

            TextMeshProUGUI itemLabelText = itemLabelObject.AddComponent<TextMeshProUGUI>();
            itemLabelText.font = ExperienceClassText.Text.font;
            itemLabelText.fontSize = (int)ExperienceClassText.Text.fontSize;
            itemLabelText.color = ExperienceClassText.Text.color;
            itemLabelText.alignment = TextAlignmentOptions.Left;
            itemLabelText.text = "Option"; // Placeholder text

            // Configure the Toggle component
            itemToggle.targetGraphic = itemBackgroundImage;
            itemToggle.graphic = itemCheckmarkImage;
            itemToggle.isOn = false;

            // Assign the itemText property of the dropdown
            dropdownMenu.itemText = itemLabelText;

            // Add ScrollRect to the template
            ScrollRect scrollRect = templateObject.AddComponent<ScrollRect>();
            scrollRect.content = contentRectTransform;
            scrollRect.viewport = viewportRectTransform;
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            scrollRect.movementType = ScrollRect.MovementType.Clamped;
            scrollRect.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHideAndExpandViewport;

            // Disable the template by default
            templateObject.SetActive(false);

            // set layers
            itemObject.layer = Canvas.gameObject.layer;
            itemBackgroundObject.layer = Canvas.gameObject.layer;
            itemCheckmarkObject.layer = Canvas.gameObject.layer;
            itemLabelObject.layer = Canvas.gameObject.layer;

            // Add TextMeshProUGUI for the option text
            Core.Log.LogInfo("Adding text to item...");
            TextMeshProUGUI itemText = itemObject.AddComponent<TextMeshProUGUI>();
            //itemText.text = "Option";  // Placeholder text for the option
            itemText.font = ExperienceClassText.Text.font;
            itemText.fontSize = (int)ExperienceClassText.Text.fontSize;
            itemText.color = ExperienceClassText.Text.color;
            itemText.alignment = TextAlignmentOptions.Center;

            Core.Log.LogInfo("Adding background to dropdown...");
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
            dropdownMenuObject.layer = Canvas.gameObject.layer;
            dropdownMenuObject.SetActive(true);
            
        }
        catch (Exception e)
        {
            Core.Log.LogError(e);
        }
    }
public static GameObject CreateUIObject(string name, GameObject parent, Vector2 sizeDelta = default)
{
    GameObject obj = new(name)
    {
        layer = 5,
        hideFlags = HideFlags.HideAndDontSave,
    };

    if (parent)
    {
        obj.transform.SetParent(parent.transform, false);
    }

    RectTransform rect = obj.AddComponent<RectTransform>();
    rect.sizeDelta = sizeDelta;
    return obj;
}
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
/*
if (_localCharacter.TryGetComponent(out Equipment equipment))
{
    Entity weaponEntity = equipment.WeaponSlot.SlotEntity.GetEntityOnServer();

    if (!weaponEntity.Exists()) return;
    DataService.WeaponType weaponType = GetWeaponTypeFromWeaponEntity(weaponEntity);

    if (weaponType.ToString() != _expertiseType) return;
    else if (weaponEntity.TryGetComponent(out PrefabGUID prefabGuid) && _localCharacter.TryGetComponent(out Movement movement))
    {
        // if (PrefabCollectionSystem._PrefabGuidToEntityMap.TryGetValue(prefabGuid, out Entity prefabEntity) && prefabEntity.TryGetBuffer<ModifyUnitStatBuff_DOTS>(out var buffer)) ClientGameManager y u do dis :(

        if (weaponEntity.Has<ModifyUnitStatBuff_DOTS>())
        {
            if (!PrefabEntityCache.TryGetValue(prefabGuid, out Entity prefabEntity))
            {
                PrefabEntityCache[prefabGuid] = PrefabCollectionSystem._PrefabGuidToEntityMap[prefabGuid];
            }

            if (!prefabEntity.Has<ModifyUnitStatBuff_DOTS>()) return;

            var buffer = prefabEntity.ReadBuffer<ModifyUnitStatBuff_DOTS>(); // try GameManager_Shared?

            if (!WeaponStatCache.TryGetValue(prefabGuid, out var previousWeaponStats))
            {
                WeaponStatCache[prefabGuid] = [];
                previousWeaponStats = WeaponStatCache[prefabGuid];
            }

            if (!OriginalWeaponStatsCache.TryGetValue(prefabGuid, out var originalWeaponStats))
            {
                OriginalWeaponStatsCache[prefabGuid] = [];

                foreach (var entry in buffer)
                {
                    OriginalWeaponStatsCache[prefabGuid][entry.StatType] = entry.Value;
                }
            }

            float movementSpeed = movement.Speed._Value;

            if (previousWeaponStats.Any() && !weaponStats.Any())
            {
                buffer.Clear();

                foreach (var keyValuePair in originalWeaponStats)
                {
                    ModifyUnitStatBuff_DOTS newEntry = new()
                    {
                        StatType = keyValuePair.Key,
                        ModificationType = !keyValuePair.Key.Equals(UnitStatType.MovementSpeed) ? ModificationType.AddToBase : ModificationType.MultiplyBaseAdd,
                        Value = keyValuePair.Key.Equals(UnitStatType.MovementSpeed) ? keyValuePair.Value / movementSpeed : keyValuePair.Value,
                        Modifier = 1,
                        IncreaseByStacks = false,
                        ValueByStacks = 0,
                        Priority = 0,
                        Id = ModificationIDs.Create().NewModificationId()
                    };

                    buffer.Add(newEntry);
                }

                previousWeaponStats.Clear();
            }
            else if (weaponStats.Any())
            {
                for (int i = 0; i < buffer.Length; i++)
                {
                    ModifyUnitStatBuff_DOTS entry = buffer[i];

                    if (previousWeaponStats.TryGetValue(entry.StatType, out var previousValue) &&
                        weaponStats.TryGetValue(entry.StatType, out var updatedValue))
                    {
                        float delta = updatedValue - previousValue;

                        if (entry.StatType == UnitStatType.MovementSpeed)
                        {
                            entry.Value += delta / movementSpeed;
                        }
                        else
                        {
                            entry.Value += delta;
                        }

                        buffer[i] = entry;

                        previousWeaponStats[entry.StatType] = updatedValue;
                        weaponStats.Remove(entry.StatType);
                    }
                }

                if (weaponStats.Any())
                {
                    foreach (var keyValuePair in weaponStats)
                    {
                        float previousValue = previousWeaponStats.TryGetValue(keyValuePair.Key, out var value) ? value : 0f;
                        float valueDelta = keyValuePair.Value - previousValue;

                        ModifyUnitStatBuff_DOTS newEntry = new()
                        {
                            StatType = keyValuePair.Key,
                            ModificationType = !keyValuePair.Key.Equals(UnitStatType.MovementSpeed) ? ModificationType.AddToBase : ModificationType.MultiplyBaseAdd,
                            Value = keyValuePair.Key.Equals(UnitStatType.MovementSpeed) ? valueDelta / movementSpeed : valueDelta,
                            Modifier = 1,
                            IncreaseByStacks = false,
                            ValueByStacks = 0,
                            Priority = 0,
                            Id = ModificationIDs.Create().NewModificationId()
                        };

                        buffer.Insert(1, newEntry);
                        previousWeaponStats[keyValuePair.Key] = keyValuePair.Value; // Track the new value
                    }
                }
            }
        }
    }
}
*/
