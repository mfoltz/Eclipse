using Bloodcraft.Resources;
using Eclipse.Elements;
using Eclipse.Elements.States;
using Eclipse.Patches;
using Eclipse.Utilities;
using Il2CppInterop.Runtime;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using ProjectM;
using ProjectM.UI;
using Stunlock.Core;
using StunShared.UI;
using System.Collections;
using System;
using System.Linq;
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

    public static bool LevelingEnabled { get; } = Plugin.Leveling;
    public static bool PrestigeEnabled { get; } = Plugin.Prestige;
    public static bool LegaciesEnabled { get; } = Plugin.Legacies;
    public static bool ExpertiseEnabled { get; } = Plugin.Expertise;
    public static bool FamiliarEnabled { get; } = Plugin.Familiars;
    public static bool ProfessionsEnabled { get; } = Plugin.Professions;
    public static bool QuestsEnabled { get; } = Plugin.Quests;
    public static bool ShiftSlotEnabled { get; } = Plugin.ShiftSlot;
    public enum Element
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
        // sprites for attribute page
        "Attribute_TierIndicator_Fixed", // class stat synergy?
        "BloodTypeFrame",                // bl
        "BloodTypeIcon_Tiny_Warrior",    // wep
        // older
        "BloodIcon_Cursed",
        "BloodIcon_Small_Cursed",
        "BloodIcon_Small_Holy",
        "BloodIcon_Warrior",
        "BloodIcon_Small_Warrior",
        "Poneti_Icon_Hammer_30",
        "Poneti_Icon_Bag",
        "Poneti_Icon_Res_93",
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

    const string BLOOD_ORB_PATH = "HUDCanvas(Clone)/BottomBarCanvas/BottomBar(Clone)/Content/BloodOrbParent/BloodOrb/BlackBackground/Blood";

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
    public static WaitForSeconds Delay { get; } = new(0.25f);
    static readonly WaitForSeconds _shiftDelay = new(0.1f);

    static UICanvasBase _canvasBase;
    static Canvas _bottomBarCanvas;
    static Canvas _targetInfoPanelCanvas;
    public static string _version = string.Empty;

    internal static LevelingState LevelingState => DataService.Leveling;
    internal static LegacyState LegacyState => DataService.Legacy;

    public static string _expertiseType = string.Empty;
    public static float _expertiseProgress = 0f;
    public static int _expertiseLevel = 0;
    public static int _expertisePrestige = 0;
    public static int _expertiseMaxLevel = 100;
    public static List<string> _expertiseBonusStats = ["", "", ""];

    public static float _familiarProgress = 0f;
    public static int _familiarLevel = 1;
    public static int _familiarPrestige = 0;
    public static int _familiarMaxLevel = 90;
    public static string _familiarName = "";
    public static List<string> _familiarStats = ["", "", ""];

    // moved to Professions component
    public static float _enchantingProgress = 0f;
    public static int _enchantingLevel = 0;

    public static float _alchemyProgress = 0f;
    public static int _alchemyLevel = 0;

    public static float _harvestingProgress = 0f;
    public static int _harvestingLevel = 0;

    public static float _blacksmithingProgress = 0f;
    public static int _blacksmithingLevel = 0;

    public static float _tailoringProgress = 0f;
    public static int _tailoringLevel = 0;

    public static float _woodcuttingProgress = 0f;
    public static int _woodcuttingLevel = 0;

    public static float _miningProgress = 0f;
    public static int _miningLevel = 0;

    public static float _fishingProgress = 0f;
    public static int _fishingLevel = 0;

    public static Image _dailyQuestIcon;
    public static TargetType _dailyTargetType = TargetType.Kill;
    public static int _dailyProgress = 0;
    public static int _dailyGoal = 0;
    public static string _dailyTarget = "";
    public static bool _dailyVBlood = false;

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

    public static GameObject _abilityEmptyIcon;
    public static GameObject _abilityIcon;

    public static GameObject _keybindObject;

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

    static readonly Dictionary<Element, GameObject> _elementObjects = [];
    static readonly Dictionary<GameObject, bool> _elementStates = [];
    static readonly List<GameObject> _professionElements = [];

    internal static Leveling Leveling { get; set; }
    internal static Legacies Legacies { get; set; }
    internal static Professions Professions { get; set; }
    internal static Expertise Expertise { get; set; }
    internal static Familiar Familiar { get; set; }
    internal static Quests Quests { get; set; }
    internal static ShiftSlot ShiftSlot { get; set; }
    internal static SyncAdaptives SyncAdaptives { get; set; }

    internal static void SetElementState(GameObject obj, bool state)
    {
        if (_elementStates.ContainsKey(obj))
        {
            _elementStates[obj] = state;
        }
    }

    static readonly Dictionary<Element, Action> _abilitySlotToggles = new()
    {
        {Element.Experience, () => Leveling?.Toggle()},
        {Element.Legacy, () => Legacies?.Toggle()},
        {Element.Expertise, () => Expertise?.Toggle()},
        {Element.Familiars, () => Familiar?.Toggle()},
        {Element.Professions, () => Professions?.Toggle()},
        {Element.Daily, () => Quests?.Toggle()},
        {Element.Weekly, () => Quests?.Toggle()},
        {Element.ShiftSlot, () => ShiftSlot?.Toggle()}
    };

    static readonly Dictionary<Element, string> _abilitySlotPaths = new()
    {
        { Element.Experience, "HUDCanvas(Clone)/BottomBarCanvas/BottomBar(Clone)/Content/Background/AbilityBar/AbilityBarEntry_Primary/" },
        { Element.Legacy, "HUDCanvas(Clone)/BottomBarCanvas/BottomBar(Clone)/Content/Background/AbilityBar/AbilityBarEntry_WeaponSkill1/" },
        { Element.Expertise, "HUDCanvas(Clone)/BottomBarCanvas/BottomBar(Clone)/Content/Background/AbilityBar/AbilityBarEntry_WeaponSkill2/" },
        { Element.Familiars, "HUDCanvas(Clone)/BottomBarCanvas/BottomBar(Clone)/Content/Background/AbilityBar/AbilityBarEntry_Travel/" },
        { Element.Professions, "HUDCanvas(Clone)/BottomBarCanvas/BottomBar(Clone)/Content/Background/AbilityBar/AbilityBarEntry_Spell1/" },
        { Element.Weekly, "HUDCanvas(Clone)/BottomBarCanvas/BottomBar(Clone)/Content/Background/AbilityBar/AbilityBarEntry_Spell2/" },
        { Element.Daily, "HUDCanvas(Clone)/BottomBarCanvas/BottomBar(Clone)/Content/Background/AbilityBar/AbilityBarEntry_Ultimate/" },
    };

    static readonly Dictionary<Element, bool> _activeElements = new()
    {
        { Element.Experience, LevelingEnabled },
        { Element.Legacy, LegaciesEnabled },
        { Element.Expertise, ExpertiseEnabled },
        { Element.Familiars, FamiliarEnabled },
        { Element.Professions, ProfessionsEnabled },
        { Element.Daily, QuestsEnabled },
        { Element.Weekly, QuestsEnabled },
        { Element.ShiftSlot, ShiftSlotEnabled }
    };

    const string FISHING = "Go Fish!";

    public static bool _ready = false;
    public static bool _active = false;
    public static bool _shiftActive = false;
    public static bool _killSwitch = false;

    public static Coroutine _shiftRoutine;

    static readonly PrefabGUID _statsBuff = PrefabGUIDs.SetBonus_AllLeech_T09;
    static readonly bool _statsBuffActive = LegaciesEnabled || ExpertiseEnabled;

    static readonly List<IReactiveElement> _managers = [];
    static readonly List<Coroutine> _managerCoroutines = [];

    static readonly Dictionary<UnitStatType, float> _lastSeen = [];
    static readonly Dictionary<ulong, ModifyUnitStatBuff_DOTS> _weaponStats = [];
    static readonly Dictionary<ulong, ModifyUnitStatBuff_DOTS> _bloodStats = [];

    static bool ContainsMetadata(Dictionary<ulong, ModifyUnitStatBuff_DOTS> source, uint metadata) =>
        source.Keys.Any(id => ModificationIds.ExtractMetadata(id) == metadata);
    public CanvasService(UICanvasBase canvas)
    {
        _canvasBase = canvas;

        _bottomBarCanvas = canvas.BottomBarParent.gameObject.GetComponent<Canvas>();
        _targetInfoPanelCanvas = canvas.TargetInfoPanelParent.gameObject.GetComponent<Canvas>();

        _layer = _bottomBarCanvas.gameObject.layer;
        _barNumber = 0;
        _graphBarNumber = 0;
        _windowOffset = 0f;

        FindSprites();
        InitializeBloodButton();

        try
        {
            Leveling = new Leveling(DataService.Leveling);
            Legacies = new Legacies(DataService.Legacy);
            Professions = new Professions();
            Expertise = new Expertise();
            Familiar = new Familiar();
            Quests = new Quests();
            ShiftSlot = new ShiftSlot();
            SyncAdaptives = new SyncAdaptives();

            _managers.AddRange(
            [
                new Managers.LevelingManager(),
                new Managers.LegacyManager(),
                new Managers.ExpertiseManager(),
                new Managers.FamiliarManager(),
                new Managers.ProfessionManager(),
                new Managers.QuestManager(),
                new Managers.ShiftManager(),
                new Managers.SyncManager()
            ]);

            foreach (var manager in _managers)
            {
                manager.Awake();
                _managerCoroutines.Add(manager.OnUpdate().Start());
            }

            InitializeAbilitySlotButtons();
            _ready = true;
        }
        catch (Exception ex)
        {
            Core.Log.LogError($"Failed to initialize UI elements: {ex}");
        }
    }

    static GameObject _attributeObjectPrefab;
    static readonly List<GameObject> _attributeObjects = [];

    static readonly Dictionary<UnitStatType, LocalizedText> _bloodAttributeTexts = [];
    static readonly Dictionary<UnitStatType, LocalizedText> _weaponAttributeTexts = [];
    public static bool InitializeAttributeValues(InventorySubMenu inventorySubMenu)
    {
        bool isInitialized = false;

        if (inventorySubMenu == null)
        {
            Core.Log.LogError("InventorySubMenu is null!");
        }

        Transform attributeSectionsParent = inventorySubMenu.AttributesParentConsole.transform.parent.parent.GetChild(4).GetChild(0).GetChild(2).GetChild(0);
        var attributeSections = attributeSectionsParent?.GetComponentsInChildren<CharacterAttributeSection>(false).Skip(2);

        foreach (CharacterAttributeSection section in attributeSections)
        {
            GameObject attributesContainer = section.transform.FindChild("AttributesContainer").gameObject;
            var characterAttributeEntries = attributesContainer.transform.GetComponentsInChildren<CharacterAttributeEntry>(false);
            int index = 0;

            foreach (CharacterAttributeEntry characterAttributeEntry in characterAttributeEntries)
            {
                string name = characterAttributeEntry.gameObject.name;

                if (!name.EndsWith("(Clone)"))
                {
                    continue;
                }

                GameObject attributeObject = characterAttributeEntry.transform.GetChild(0).gameObject;
                SimpleStunButton simpleStunButton = attributeObject.GetComponent<SimpleStunButton>();

                if (simpleStunButton == null)
                    continue;

                GameObject gameObject = attributeObject.gameObject.transform.GetChild(0).gameObject;
                GameObject attributeValue = gameObject.transform.GetChild(1).gameObject;

                if (_attributeObjectPrefab == null)
                    _attributeObjectPrefab = attributeValue;

                UnitStatType unitStatType = section.Attributes[index++].Type;
                GameObject attributeValueClone = UIHelper.InstantiateGameObjectUnderAnchor(_attributeObjectPrefab, gameObject.transform);
                GameObject attributeTypeClone = UIHelper.InstantiateGameObjectUnderAnchor(_attributeObjectPrefab, gameObject.transform);
                GameObject attibuteSynergyClone = UIHelper.InstantiateGameObjectUnderAnchor(_attributeObjectPrefab, gameObject.transform);

                ConfigureAttributeObjects(simpleStunButton, attributeObject, gameObject,
                    attributeValue, attributeValueClone, attributeTypeClone,
                    attibuteSynergyClone, unitStatType);

                isInitialized = true;
            }
        }

        return isInitialized;
    }
    static void ConfigureAttributeType(GameObject attributeSpriteClone, Sprite sprite)
    {
        TextMeshProUGUI textMeshPro = attributeSpriteClone.GetComponent<TextMeshProUGUI>();
        textMeshPro.spriteAsset = GameObjects.CreateSpriteAsset(sprite);
        textMeshPro.m_spriteColor = Color.white;

        LayoutElement layoutElement = attributeSpriteClone.GetComponent<LayoutElement>();
        LocalizedText localizedText = attributeSpriteClone.GetComponent<LocalizedText>();

        layoutElement.flexibleWidth = 1f;
        attributeSpriteClone.transform.SetSiblingIndex(1);

        textMeshPro.autoSizeTextContainer = true;
        textMeshPro.enableWordWrapping = false;

        localizedText.ForceSet(string.Empty);
        attributeSpriteClone.SetActive(true);
    }
    static void ConfigureAttributeButton(SimpleStunButton button, string command)
        => button.onClick.AddListener((UnityAction)(() => Quips.SendCommand(command)));
    static void ConfigureAttributeObjects(SimpleStunButton simpleStunButton, GameObject attributeEntryObject,
        GameObject gameObject, GameObject attributeValue, GameObject attributeValueClone,
        GameObject attributeTypeClone, GameObject attibuteSynergyClone, UnitStatType unitStatType)
    {
        HorizontalLayoutGroup horizontalLayoutGroup = gameObject.GetComponent<HorizontalLayoutGroup>();
        TextMeshProUGUI textMeshPro = attributeValue.GetComponent<TextMeshProUGUI>();
        // Image image = attributeEntryObject.GetComponent<Image>();

        LayoutElement layoutElement = attributeValueClone.GetComponent<LayoutElement>();
        LocalizedText localizedText = attributeValueClone.GetComponent<LocalizedText>();

        if (Enum.TryParse(unitStatType.ToString(), true, out BloodStatType bloodStatType)
            && _bloodStatValues.ContainsKey(bloodStatType))
        {
            // image.color = new(1f, 0f, 0f, 0.75f);
            ConfigureAttributeButton(simpleStunButton, $".bl cst {(int)bloodStatType}");
            ConfigureAttributeType(attributeTypeClone, _sprites["BloodTypeFrame"]);

            _bloodAttributeTexts[unitStatType] = localizedText;
            _attributeObjects.Add(attributeValueClone);

            if (_lastSeen.TryGetValue(unitStatType, out float statValue) && statValue != 0f) TrySetAttribute(unitStatType, statValue);
        }
        else if (Enum.TryParse(unitStatType.ToString(), true, out WeaponStatType weaponStatType)
            && _weaponStatValues.ContainsKey(weaponStatType))
        {
            ConfigureAttributeButton(simpleStunButton, $".wep cst {(int)weaponStatType}");
            ConfigureAttributeType(attributeTypeClone, _sprites["BloodTypeIcon_Tiny_Warrior"]);

            _weaponAttributeTexts[unitStatType] = localizedText;
            _attributeObjects.Add(attributeValueClone);

            if (_lastSeen.TryGetValue(unitStatType, out float statValue) && statValue != 0f) TrySetAttribute(unitStatType, statValue);
        }
        else
            _attributeObjects.Add(attributeValueClone);

        horizontalLayoutGroup.childForceExpandWidth = false;
        layoutElement.flexibleWidth = 1f;
        attributeValueClone.transform.SetSiblingIndex(1);
        textMeshPro.autoSizeTextContainer = true;
        textMeshPro.enableWordWrapping = false;

        localizedText.ForceSet(string.Empty);
        attributeValueClone.SetActive(true);
    }
    static void InitializeAbilitySlotButtons()
    {
        foreach (var keyValuePair in _activeElements)
        {
            if (keyValuePair.Value && _abilitySlotPaths.ContainsKey(keyValuePair.Key))
            {
                GameObject abilitySlotObject = GameObject.Find(_abilitySlotPaths[keyValuePair.Key]);
                SimpleStunButton stunButton = abilitySlotObject.AddComponent<SimpleStunButton>();

                if (keyValuePair.Key.Equals(Element.Professions))
                {
                    GameObject[] capturedObjects = [.. _professionElements];
                    stunButton.onClick.AddListener((UnityAction)(() => ToggleGameObjects(capturedObjects)));
                }
                else if (_elementObjects.TryGetValue(keyValuePair.Key, out GameObject gameObject))
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

            _elementStates[gameObject] = newState;
        }
    }
    static void InitializeBloodButton()
    {
        GameObject bloodObject = GameObject.Find(BLOOD_ORB_PATH);

        if (bloodObject != null)
        {
            SimpleStunButton stunButton = bloodObject.AddComponent<SimpleStunButton>();
            stunButton.onClick.AddListener(new Action(ToggleAllObjects));
        }
    }
    static void ToggleAllObjects()
    {
        _active = !_active;

        foreach (GameObject gameObject in _elementStates.Keys)
        {
            gameObject.active = _active;
            _elementStates[gameObject] = _active;
        }
    }
    public static void GetAndUpdateWeaponStatBuffer(Entity playerCharacter)
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

        List<uint> existingIds = [];

        for (int i = buffer.Length - 1; i >= 0; i--)
        {
            uint id = (uint)buffer[i].Id.Id;

            if (id != 0 && !ContainsMetadata(_weaponStats, id))
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
            if (!existingIds.Contains(ModificationIds.ExtractMetadata(keyValuePair.Key)))
            {
                buffer.Add(keyValuePair.Value);
            }
        }
    }
    static void TryClearAttribute(UnitStatType unitStatType)
    {
        if (_bloodAttributeTexts.TryGetValue(unitStatType, out LocalizedText localizedText))
        {
            localizedText.ForceSet(string.Empty);
        }
        else if (_weaponAttributeTexts.TryGetValue(unitStatType, out localizedText))
        {
            localizedText.ForceSet(string.Empty);
        }
    }
    static void TrySetAttribute(UnitStatType unitStatType, float statValue = 0f)
    {
        string statString = $"<color=#90EE90>+{statValue * 100f:F0}%</color>";

        if (_bloodAttributeTexts.TryGetValue(unitStatType, out LocalizedText localizedText))
        {
            localizedText.ForceSet(statString);
        }
        else if (_weaponAttributeTexts.TryGetValue(unitStatType, out localizedText))
        {
            localizedText.ForceSet(statString);
        }
    }
    public static void UpdateBuffStatBuffer()
    {
        if (!Core.LocalCharacter.TryGetBuff(_statsBuff, out Entity buffEntity)
            || !buffEntity.TryGetBuffer<ModifyUnitStatBuff_DOTS>(out var buffer)) return;

        var existingIds = new HashSet<uint>();
        for (int i = 0; i < buffer.Length; i++)
            existingIds.Add((uint)buffer[i].Id.Id);

        List<ModifyUnitStatBuff_DOTS> existingEtries = [];
        existingEtries.AddRange(_weaponStats.Values);
        existingEtries.AddRange(_bloodStats.Values);

        for (int i = buffer.Length - 1; i >= 0; i--)
        {
            uint id = (uint)buffer[i].Id.Id;
            UnitStatType unitStatType = buffer[i].StatType;
            if (!ContainsMetadata(_weaponStats, id)
                && !ContainsMetadata(_bloodStats, id))
                buffer.RemoveAt(i);
            TryClearAttribute(unitStatType);
        }

        foreach (var entry in existingEtries)
        {
            if (!existingIds.Contains((uint)entry.Id.Id))
            {
                buffer.Add(entry);
            }
        }

        foreach (var entry in existingEtries)
        {
            TrySetAttribute(entry.StatType, entry.Value);
        }

        _weaponStats.Clear();
        _bloodStats.Clear();
    }
    public static void UpdateAbilityData(AbilityTooltipData abilityTooltipData, Entity abilityGroupEntity, Entity abilityCastEntity, PrefabGUID abilityGroupPrefabGuid)
    {
        if (!_abilityDummyObject.active)
        {
            _abilityDummyObject.SetActive(true);
            // if (_uiState.CachedInputVersion != 3) _uiState.CachedInputVersion = 3;
        }

        if (!_keybindObject.active) _keybindObject.SetActive(true);

        _cooldownFillImage.fillAmount = 0f;
        _chargeCooldownFillImage.fillAmount = 0f;

        _abilityGroupPrefabGUID = abilityGroupPrefabGuid;

        _abilityBarEntry.AbilityEntity = abilityGroupEntity;
        _abilityBarEntry.AbilityId = abilityGroupPrefabGuid;
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
            _cooldownTime = _shiftSpellIndex.Equals(-1) ? abilityCooldownData.Cooldown._Value : (_shiftSpellIndex * COOLDOWN_FACTOR) + COOLDOWN_FACTOR;
            _cooldownEndTime = Core.ServerTime.TimeOnServer + _cooldownTime;
        }

        AbilityBarEntry.Data data = new();
        _abilityBarEntry.SetData(ref data, SystemService.InputActionSystem);
        // SystemService.AbilityBarParentBinderSystem ?
    }
    public static void UpdateAbilityState(Entity abilityGroupEntity, Entity abilityCastEntity)
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
                yield return Delay;
                continue;
            }
            else if (!_shiftActive)
            {
                yield return Delay;
                continue;
            }

            if (Core.LocalCharacter.TryGetComponent(out AbilityBar_Shared abilityBar_Shared))
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
    public static bool TryUpdateTooltipData(Entity abilityGroupEntity, PrefabGUID abilityGroupPrefabGUID)
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
    public static void UpdateBar(float progress, int level, int maxLevel,
        int prestiges, LocalizedText levelText, LocalizedText barHeader,
        Image fill, Element element, string type = "")
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

        if (element.Equals(Element.Familiars))
        {
            type = TrimToFirstWord(type);
        }

        if (barHeader.Text.fontSize != _horizontalBarHeaderFontSize)
        {
            barHeader.Text.fontSize = _horizontalBarHeaderFontSize;
        }

        if (PrestigeEnabled && prestiges != 0)
        {
            string header = "";

            if (element.Equals(Element.Experience))
            {
                header = $"{element} {IntegerToRoman(prestiges)}";
            }
            else if (element.Equals(Element.Legacy))
            {
                header = $"{type} {IntegerToRoman(prestiges)}";
            }
            else if (element.Equals(Element.Expertise))
            {
                header = $"{type} {IntegerToRoman(prestiges)}";
            }
            else if (element.Equals(Element.Familiars))
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
    public static void UpdateClass(PlayerClass classType, LocalizedText classText)
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
    public static void UpdateWeaponStats(List<string> bonusStats, List<LocalizedText> statTexts, Func<string, string> getStatInfo)
    {
        if (_killSwitch) return;

        for (int i = 0; i < 3 && i < statTexts.Count; i++)
        {
            string stat = i < bonusStats.Count ? bonusStats[i] : "None";

            if (stat != "None")
            {
                if (!statTexts[i].enabled) statTexts[i].enabled = true;
                if (!statTexts[i].gameObject.active) statTexts[i].gameObject.SetActive(true);

                string statInfo = getStatInfo(stat);
                statTexts[i].ForceSet(statInfo);
            }
            else if (statTexts[i].enabled)
            {
                statTexts[i].ForceSet("");
                statTexts[i].enabled = false;
            }

            // Core.Log.LogWarning($"WeaponStats - {bonusStats[i]} - {statTexts[i].GetText()}");
        }
    }
    public static void UpdateBloodStats(List<string> bonusStats, List<LocalizedText> statTexts, Func<string, string> getStatInfo)
    {
        if (_killSwitch) return;

        for (int i = 0; i < 3 && i < statTexts.Count; i++)
        {
            string stat = i < bonusStats.Count ? bonusStats[i] : "None";

            if (stat != "None")
            {
                if (!statTexts[i].enabled) statTexts[i].enabled = true;
                if (!statTexts[i].gameObject.active) statTexts[i].gameObject.SetActive(true);

                string statInfo = getStatInfo(stat);
                statTexts[i].ForceSet(statInfo);
            }
            else if (statTexts[i].enabled)
            {
                statTexts[i].ForceSet("");
                statTexts[i].enabled = false;
            }

            // Core.Log.LogWarning($"BloodStats - {bonusStats[i]} - {statTexts[i].GetText()}");
        }
    }
    public static string GetWeaponStatInfo(string statType)
    {
        if (Enum.TryParse(statType, out WeaponStatType weaponStat))
        {
            if (_weaponStatValues.TryGetValue(weaponStat, out float statValue))
            {
                float classMultiplier = ClassSynergy(weaponStat, LevelingState.Class, _classStatSynergies);
                statValue *= (1 + _prestigeStatMultiplier * _expertisePrestige) * classMultiplier * ((float)_expertiseLevel / _expertiseMaxLevel);
                float displayStatValue = statValue;
                ulong statModificationId = ModificationIds.GenerateId(0, (int)weaponStat, statValue);

                if (weaponStat.Equals(WeaponStatType.BonusMovementSpeed)
                    && Core.LocalCharacter.TryGetComponent(out Movement movement))
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
                    Id = new((int)ModificationIds.ExtractMetadata(statModificationId))
                };

                _weaponStats[statModificationId] = unitStatBuff;
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
    public static string GetBloodStatInfo(string statType)
    {
        if (Enum.TryParse(statType, out BloodStatType bloodStat))
        {
            if (_bloodStatValues.TryGetValue(bloodStat, out float statValue))
            {
                float classMultiplier = ClassSynergy(bloodStat, LevelingState.Class, _classStatSynergies);
                statValue *= (1 + _prestigeStatMultiplier * LegacyState.Prestige) * classMultiplier * ((float)LegacyState.Level / LegacyState.MaxLevel);
                string displayString = $"<color=#00FFFF>{BloodStatTypeAbbreviations[bloodStat]}</color>: <color=#90EE90>{(statValue * 100).ToString("F0") + "%"}</color>";

                ulong statModificationId = ModificationIds.GenerateId(1, (int)bloodStat, statValue);

                ModifyUnitStatBuff_DOTS unitStatBuff = new()
                {
                    StatType = (UnitStatType)Enum.Parse(typeof(UnitStatType), bloodStat.ToString()),
                    ModificationType = ModificationType.Add,
                    Value = statValue,
                    Modifier = 1,
                    IncreaseByStacks = false,
                    ValueByStacks = 0,
                    Priority = 0,
                    Id = new((int)ModificationIds.ExtractMetadata(statModificationId))
                };

                _bloodStats[statModificationId] = unitStatBuff;

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

        return string.Empty;
    }
    public static void UpdateFamiliarStats(List<string> familiarStats, List<LocalizedText> statTexts)
    {
        if (_killSwitch) return;

        // Use the smallest list count to avoid out-of-range errors when either list doesn't have three entries.
        int count = Math.Min(3, Math.Min(familiarStats.Count, statTexts.Count));

        for (int i = 0; i < count; i++)
        {
            string stat = i < familiarStats.Count ? familiarStats[i] : "0";

            if (!string.IsNullOrEmpty(stat) && stat != "0")
            {
                if (!statTexts[i].enabled) statTexts[i].enabled = true;
                if (!statTexts[i].gameObject.active) statTexts[i].gameObject.SetActive(true);

                string statInfo = $"<color=#00FFFF>{FamiliarStatStringAbbreviations[i]}</color>: <color=#90EE90>{stat}</color>";
                statTexts[i].ForceSet(statInfo);
            }
            else if (statTexts[i].enabled)
            {
                statTexts[i].ForceSet(string.Empty);
                statTexts[i].enabled = false;
            }
        }

        // Clear remaining text objects if there are more UI slots than stats.
        for (int i = count; i < Math.Min(3, statTexts.Count); i++)
        {
            if (statTexts[i].enabled)
            {
                statTexts[i].ForceSet(string.Empty);
                statTexts[i].enabled = false;
            }
        }
    }
    public static void UpdateQuests(GameObject questObject, LocalizedText questSubHeader, Image questIcon,
        TargetType targetType, string target, int progress, int goal, bool isVBlood)
    {
        if (_killSwitch) return;

        if (progress != goal && _elementStates[questObject])
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
    public static void ConfigureShiftSlot(ref GameObject shiftSlotObject,
        ref AbilityBarEntry shiftSlotEntry, ref AbilityBarEntry.UIState uiState, ref GameObject cooldownObject,
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

            _elementStates.Add(shiftSlotObject, true);
            _elementObjects.Add(Element.ShiftSlot, shiftSlotObject);

            SimpleStunButton stunButton = shiftSlotObject.AddComponent<SimpleStunButton>();

            if (_abilitySlotToggles.TryGetValue(Element.ShiftSlot, out var toggleAction))
            {
                stunButton.onClick.AddListener(new Action(toggleAction));
            }
        }
        else
        {
            Core.Log.LogWarning("AbilityBarEntry_Dummy is null!");
        }
    }
    public static void ConfigureQuestWindow(ref GameObject questObject, Element questType, Color headerColor,
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
        if (questType.Equals(Element.Daily))
        {
            if (Sprites.ContainsKey("BloodIcon_Small_Warrior"))
            {
                questIcon.sprite = Sprites["BloodIcon_Small_Warrior"];
            }
        }
        else if (questType.Equals(Element.Weekly))
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
        _elementObjects.Add(questType, questObject);
        _elementStates.Add(questObject, true);
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
    public static void ConfigureHorizontalProgressBar(ref GameObject barGameObject, ref GameObject informationPanelObject, ref Image fill,
        ref LocalizedText level, ref LocalizedText header, Element element, Color fillColor,
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

        _elementStates.Add(barGameObject, true);
        _elementObjects.Add(element, barGameObject);
    }
    public static void ConfigureVerticalProgressBar(ref GameObject barGameObject, ref Image progressFill, ref Image maxFill,
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
        float padding = 1f - (0.075f * 2.45f); // BAR_WIDTH_SPACING previously 0.075f
        float offsetX = padding + (barWidth * _graphBarNumber / 1.4f); // previously used 1.5f

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

        _elementStates.Add(barGameObject, true);
        _professionElements.Add(barGameObject);

        // spacing test
        const float referenceAspect = 16f / 9f; // ≈ 1.777...
        float aspectRatio = (float)Screen.width / Screen.height;

        float aspectCompensation = referenceAspect / aspectRatio; // >1 on ultrawide, <1 on narrow
        aspectCompensation = Mathf.Clamp(aspectCompensation, 0.75f, 1.0f);

        // Keyboard/Mouse layout
        float offsetX_KM = padding + (barWidth * _graphBarNumber / (1.4f * aspectCompensation));
        // float offsetX_KM = padding + barWidth * _graphBarNumber / 1.4f;
        float offsetY_KM = offsetY;
        Vector2 kmAnchorMin = new(offsetX_KM, offsetY_KM);
        Vector2 kmAnchorMax = new(offsetX_KM, offsetY_KM);
        Vector2 kmPivot = new(offsetX_KM, offsetY_KM);
        Vector2 kmPos = Vector2.zero;

        // Controller layout
        float ctrlBaseX = 0.6175f; // 0.625f previous
        float ctrlSpacingX = 0.015f; // 0.02f previous
        float offsetX_CTRL = ctrlBaseX + (_graphBarNumber * ctrlSpacingX);
        float offsetY_CTRL = 0.08f; // 0.1f previous, 0.075f previous
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
        ref LocalizedText thirdText, Element element)
    {
        switch (element)
        {
            case Element.Experience:
                ConfigureLevelingPanel(ref informationPanelObject, ref firstText, ref secondText, ref thirdText);
                break;
            default:
                ConfigureDefaultPanel(ref informationPanelObject, ref firstText, ref secondText, ref thirdText);
                break;
        }
    }
    static void ConfigureLevelingPanel(ref GameObject panel, ref LocalizedText firstText, ref LocalizedText secondText, 
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
    public static void ResetStates()
    {
        foreach (var coroutine in _managerCoroutines)
        {
            if (coroutine != null)
            {
                Core.StopCoroutine(coroutine);
            }
        }
        _managerCoroutines.Clear();
        _managers.Clear();

        foreach (GameObject gameObject in _elementStates.Keys)
        {
            if (gameObject != null)
            {
                UnityEngine.Object.Destroy(gameObject);
            }
        }

        foreach (GameObject gameObject in _professionElements)
        {
            if (gameObject != null)
            {
                UnityEngine.Object.Destroy(gameObject);
            }
        }

        foreach (GameObject gameObject in _attributeObjects)
        {
            if (gameObject != null)
            {
                UnityEngine.Object.Destroy(gameObject);
            }
        }

        foreach (GameObject gameObject in _elementObjects.Values)
        {
            if (gameObject != null)
            {
                UnityEngine.Object.Destroy(gameObject);
            }
        }

        _elementStates.Clear();
        _professionElements.Clear();
        _elementObjects.Clear();

        _lastSeen.Clear();
        _adaptiveElements.Clear();
        _attributeObjects.Clear();
        _sprites.Clear();
    }

    public static ControllerType ControllerType => _controllerType;
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
    public static void SyncAdaptiveElements()
    {
        bool isGamepad = InputActionSystemPatch.IsGamepad;
        _controllerType = isGamepad ? ControllerType.Gamepad : ControllerType.KeyboardAndMouse;
        // Core.Log.LogWarning($"[OnInputDeviceChanged] - ControllerType: {_controllerType}");

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