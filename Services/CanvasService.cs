using Eclipse.Patches;
using Eclipse.Resources;
using Eclipse.Utilities;
using Il2CppInterop.Runtime;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using MonoMod.Utils;
using ProjectM;
using ProjectM.UI;
using Stunlock.Core;
using StunShared.UI;
using System.Collections;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using TMPro;
using Unity.Entities;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using static Eclipse.Patches.InitializationPatches;
using static Eclipse.Services.CanvasService.ConfigureHUD;
using static Eclipse.Services.CanvasService.DataHUD;
using static Eclipse.Services.CanvasService.InitializeHUD;
using static Eclipse.Services.CanvasService.InputHUD;
using static Eclipse.Services.CanvasService.UpdateHUD;
using static Eclipse.Services.CanvasService.UtilitiesHUD;
using static Eclipse.Services.DataService;
using static Eclipse.Utilities.GameObjects;
using static ProjectM.Gameplay.WarEvents.WarEventRegistrySystem;
using static System.Net.Mime.MediaTypeNames;
using Image = UnityEngine.UI.Image;

namespace Eclipse.Services;
internal class CanvasService
{
    static EntityManager EntityManager
        => Core.EntityManager;
    static SystemService SystemService
        => Core.SystemService;
    static ManagedDataRegistry ManagedDataRegistry
        => SystemService.ManagedDataSystem.ManagedDataRegistry;
    static Entity LocalCharacter
        => Core.LocalCharacter;
    static BufferLookup<ModifyUnitStatBuff_DOTS> ModifyUnitStatBuffLookup
        => ClientChatSystemPatch.ModifyUnitStatBuffLookup;
    static bool Eclipsed { get; } = Plugin.Eclipsed;
    public static WaitForSeconds WaitForSeconds { get; } = Eclipsed
        ? new WaitForSeconds(0.1f)
        : new WaitForSeconds(1f);

    public static Coroutine _canvasRoutine;
    public static Coroutine _shiftRoutine;
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
            InitializeUI();
            InitializeAbilitySlotButtons();
        }
        catch (Exception ex)
        {
            Core.Log.LogError($"Failed to initialize UI elements: {ex}");
        }
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
                yield return WaitForSeconds;
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

            var buffer = TryGetSourceBuffer();

            if (_legacyBar)
            {
                try
                {
                    UpdateBar(_legacyProgress, _legacyLevel, _legacyMaxLevel, _legacyPrestige, _legacyText, _legacyHeader, _legacyFill, UIElement.Legacy, _legacyType);
                    UpdateBloodStats(_legacyBonusStats, [_firstLegacyStat, _secondLegacyStat, _thirdLegacyStat], ref buffer, GetBloodStatInfo);
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
                    UpdateWeaponStats(_expertiseBonusStats, [_firstExpertiseStat, _secondExpertiseStat, _thirdExpertiseStat], ref buffer, GetWeaponStatInfo);
                }
                catch (Exception e)
                {
                    Core.Log.LogError($"Error updating expertise bar: {e}");
                }
            }

            if (StatBuffActive)
            {
                try
                {
                    // if (AttributesInitialized)
                    UpdateTargetBuffer(ref buffer);
                    UpdateAttributes(ref buffer);
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
            // if (_killSwitch) yield break;

            try
            {
                if (!_shiftActive && LocalCharacter.TryGetComponent(out AbilityBar_Shared abilityBar_Shared))
                {
                    Entity abilityGroupEntity = abilityBar_Shared.CastGroup.GetEntityOnServer();

                    if (abilityGroupEntity.TryGetComponent(out AbilityGroupState abilityGroupState)
                        && abilityGroupState.SlotIndex == 3 // shift "slot" index
                        && _shiftRoutine == null) // if ability found on slot 3, activate shift loop
                    {
                        _shiftRoutine = ShiftUpdateLoop().Start();
                        _shiftActive = true;
                    }
                }
            }
            catch (Exception e)
            {
                Core.Log.LogError($"Error updating ability bar: {e}");
            }

            SyncInputHUD();
            yield return WaitForSeconds;
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
                yield return WaitForSeconds;
                continue;
            }
            else if (!_shiftActive)
            {
                yield return WaitForSeconds;
                continue;
            }

            if (LocalCharacter.TryGetComponent(out AbilityBar_Shared abilityBar_Shared))
            {
                Entity abilityGroupEntity = abilityBar_Shared.CastGroup.GetEntityOnServer();
                Entity abilityCastEntity = abilityBar_Shared.CastAbility.GetEntityOnServer();

                if (abilityGroupEntity.TryGetComponent(out AbilityGroupState abilityGroupState) && abilityGroupState.SlotIndex == 3)
                {
                    PrefabGUID currentPrefabGUID = abilityGroupEntity.GetPrefabGUID();

                    if (UpdateTooltipData(abilityGroupEntity, currentPrefabGUID))
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

            yield return WaitForSeconds;
        }
    }
    public static void ResetState()
    {
        foreach (GameObject gameObject in ObjectStates.Keys)
        {
            if (gameObject != null)
            {
                UnityEngine.Object.Destroy(gameObject);
            }
        }

        foreach (GameObject gameObject in ProfessionObjects)
        {
            if (gameObject != null)
            {
                UnityEngine.Object.Destroy(gameObject);
            }
        }

        foreach (GameObject gameObject in DataHUD.GameObjects.Values)
        {
            if (gameObject != null)
            {
                UnityEngine.Object.Destroy(gameObject);
            }
        }

        foreach (GameObject gameObject in AttributeObjects)
        {
            if (gameObject != null)
            {
                UnityEngine.Object.Destroy(gameObject);
            }
        }

        foreach (InputAdaptiveElement adaptiveElement in AdaptiveElements)
        {
            if (adaptiveElement.AdaptiveObject != null)
            {
                UnityEngine.Object.Destroy(adaptiveElement.AdaptiveObject);
            }
        }
        
        ObjectStates.Clear();
        ProfessionObjects.Clear();
        DataHUD.GameObjects.Clear();
        AttributeObjects.Clear();
        BloodAttributeTexts.Clear();
        WeaponAttributeTexts.Clear();
        AdaptiveElements.Clear();

        Sprites.Clear();
    }
    public static class InitializeHUD
    {
        //[Warning: Eclipse] Registered Weapon Attribute: MaxHealth
        //[Warning:   Eclipse] Registered Weapon Attribute: PhysicalPower
        //[Warning:   Eclipse] Registered Weapon Attribute: SpellPower
        //[Warning:   Eclipse] Registered Weapon Attribute: MovementSpeed
        //[Warning:   Eclipse] Registered Weapon Attribute: PrimaryAttackSpeed
        //[Warning:   Eclipse] Registered Weapon Attribute: PhysicalCriticalStrikeChance
        //[Warning:   Eclipse] Registered Weapon Attribute: PhysicalCriticalStrikeDamage
        //[Warning:   Eclipse] Registered Weapon Attribute: SpellCriticalStrikeChance
        //[Warning:   Eclipse] Registered Weapon Attribute: SpellCriticalStrikeDamage
        //[Warning:   Eclipse] Registered Blood Attribute: MinionDamage
        //[Warning:   Eclipse] Registered Blood Attribute: DamageReduction
        //[Warning:   Eclipse] Registered Blood Attribute: HealingReceived
        //[Warning:   Eclipse] Registered Weapon Attribute: PrimaryLifeLeech
        //[Warning:   Eclipse] Registered Weapon Attribute: PhysicalLifeLeech
        //[Warning:   Eclipse] Registered Weapon Attribute: SpellLifeLeech
        //[Warning:   Eclipse] Registered Blood Attribute: ReducedBloodDrain
        //[Warning:   Eclipse] Registered Blood Attribute: ResourceYield
        //[Warning:   Eclipse] Registered Blood Attribute: WeaponCooldownRecoveryRate
        //[Warning:   Eclipse] Registered Blood Attribute: SpellCooldownRecoveryRate
        //[Warning:   Eclipse] Registered Blood Attribute: UltimateCooldownRecoveryRate
        public static void InitializeUI()
        {
            if (_experienceBar)
            {
                ConfigureHorizontalProgressBar(ref _experienceBarGameObject, ref _experienceInformationPanel,
                ref _experienceFill, ref _experienceText, ref _experienceHeader, UIElement.Experience, Color.green,
                ref _experienceFirstText, ref _experienceClassText, ref _experienceSecondText);
            }

            if (_legacyBar)
            {
                ConfigureHorizontalProgressBar(ref _legacyBarGameObject, ref _legacyInformationPanel,
                ref _legacyFill, ref _legacyText, ref _legacyHeader, UIElement.Legacy, Color.red,
                ref _firstLegacyStat, ref _secondLegacyStat, ref _thirdLegacyStat);
            }

            if (_expertiseBar)
            {
                ConfigureHorizontalProgressBar(ref _expertiseBarGameObject, ref _expertiseInformationPanel,
                ref _expertiseFill, ref _expertiseText, ref _expertiseHeader, UIElement.Expertise, Color.grey,
                ref _firstExpertiseStat, ref _secondExpertiseStat, ref _thirdExpertiseStat);
            }

            if (_familiarBar)
            {
                ConfigureHorizontalProgressBar(ref _familiarBarGameObject, ref _familiarInformationPanel,
                ref _familiarFill, ref _familiarText, ref _familiarHeader, UIElement.Familiars, Color.yellow,
                ref _familiarMaxHealth, ref _familiarPhysicalPower, ref _familiarSpellPower);
            }

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
        public static void InitializeAbilitySlotButtons()
        {
            foreach (var keyValuePair in UiElementsConfigured)
            {
                if (keyValuePair.Value && AbilitySlotNamePaths.ContainsKey(keyValuePair.Key))
                {
                    GameObject abilitySlotObject = GameObject.Find(AbilitySlotNamePaths[keyValuePair.Key]);
                    SimpleStunButton stunButton = abilitySlotObject.AddComponent<SimpleStunButton>();

                    if (keyValuePair.Key.Equals(UIElement.Professions))
                    {
                        GameObject[] capturedObjects = [.. ProfessionObjects];
                        stunButton.onClick.AddListener((UnityAction)(() => ToggleHUD.ToggleGameObjects(capturedObjects)));
                    }
                    else if (DataHUD.GameObjects.TryGetValue(keyValuePair.Key, out GameObject gameObject))
                    {
                        GameObject[] capturedObjects = [gameObject];
                        stunButton.onClick.AddListener((UnityAction)(() => ToggleHUD.ToggleGameObjects(capturedObjects)));
                    }
                }
            }
        }
        public static void InitializeBloodButton()
        {
            GameObject bloodObject = GameObject.Find("HUDCanvas(Clone)/BottomBarCanvas/BottomBar(Clone)/Content/BloodOrbParent/BloodOrb/BlackBackground/Blood");

            if (bloodObject != null)
            {
                SimpleStunButton stunButton = bloodObject.AddComponent<SimpleStunButton>();
                stunButton.onClick.AddListener(new Action(ToggleHUD.ToggleAllObjects));
            }
        }
        public static bool InitializeAttributeValues(InventorySubMenu inventorySubMenu)
        {
            bool isInitialized = false;

            if (inventorySubMenu == null)
            {
                Core.Log.LogError("InventorySubMenu is null!");
            }

            Transform attributeSectionsParent = inventorySubMenu.AttributesParentConsole.transform.parent.parent.GetChild(4).GetChild(0).GetChild(2).GetChild(0);
            // var attributeSections = attributeSectionsParent?.GetComponentsInChildren<CharacterAttributeSection>(false).Skip(2);
            var attributeSections = attributeSectionsParent?
                .GetComponentsInChildren<CharacterAttributeSection>(false)
                .Take(1)
                .Concat(attributeSectionsParent.GetComponentsInChildren<CharacterAttributeSection>(false).Skip(2)
                );

            foreach (CharacterAttributeSection section in attributeSections)
            {
                GameObject attributesContainer = section.transform.FindChild("AttributesContainer").gameObject;
                var characterAttributeEntries = attributesContainer.transform.GetComponentsInChildren<CharacterAttributeEntry>(false);
                int index = 0;

                try
                {
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
                        // GameObject attributeTypeClone = UIHelper.InstantiateGameObjectUnderAnchor(_attributeObjectPrefab, gameObject.transform);
                        // GameObject attibuteSynergyClone = UIHelper.InstantiateGameObjectUnderAnchor(_attributeObjectPrefab, gameObject.transform);

                        ConfigureAttributeObjects(simpleStunButton, attributeObject, gameObject,
                            attributeValue, attributeValueClone,
                            // attributeTypeClone, attibuteSynergyClone,
                            unitStatType);

                        isInitialized = true;
                    }
                }
                catch (Exception ex)
                {
                    Core.Log.LogError($"Failed to initialize attribute values for section {section.name}: {ex}");
                }
            }

            return isInitialized;
        }
    }
    public static class DataHUD
    {
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

        public static readonly Dictionary<int, string> RomanNumerals = new()
        {
            {100, "C"}, {90, "XC"}, {50, "L"}, {40, "XL"},
            {10, "X"}, {9, "IX"}, {5, "V"}, {4, "IV"},
            {1, "I"}
        };

        public static readonly List<string> SpriteNames =
        [
            "Attribute_TierIndicator_Fixed", // class stat synergy?
            "BloodTypeFrame",                // bl
            "BloodTypeIcon_Tiny_Warrior",    // wep
            // sprites for attribute page ^
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

        public const string ABILITY_ICON = "Stunlock_Icon_Ability_Spell_";
        public const string NPC_ABILITY = "Ashka_M1_64";

        public static readonly Dictionary<Profession, string> ProfessionIcons = new()
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

        public static readonly Dictionary<string, Sprite> Sprites = [];

        public static Sprite _questKillStandardUnit;
        public static Sprite _questKillVBloodUnit;

        public static readonly Regex ClassNameRegex = new("(?<!^)([A-Z])");
        public static readonly Regex AbilitySpellRegex = new("(?<=AB_).*(?=_Group)");

        public static readonly Dictionary<PlayerClass, Color> ClassColorHexMap = new()
        {
            { PlayerClass.ShadowBlade, new Color(0.6f, 0.1f, 0.9f) },  // ignite purple
            { PlayerClass.DemonHunter, new Color(1f, 0.8f, 0f) },      // static yellow
            { PlayerClass.BloodKnight, new Color(1f, 0f, 0f) },        // leech red
            { PlayerClass.ArcaneSorcerer, new Color(0f, 0.5f, 0.5f) }, // weaken teal
            { PlayerClass.VampireLord, new Color(0f, 1f, 1f) },        // chill cyan
            { PlayerClass.DeathMage, new Color(0f, 1f, 0f) }           // condemn green
        };

        public const string V1_3 = "1.3";

        public static UICanvasBase _canvasBase;
        public static Canvas _bottomBarCanvas;
        public static Canvas _targetInfoPanelCanvas;
        public static string _version = string.Empty;

        public static GameObject _experienceBarGameObject;
        public static GameObject _experienceInformationPanel;
        public static LocalizedText _experienceHeader;
        public static LocalizedText _experienceText;
        public static LocalizedText _experienceFirstText;
        public static LocalizedText _experienceClassText;
        public static LocalizedText _experienceSecondText;
        public static Image _experienceFill;
        public static float _experienceProgress = 0f;
        public static int _experienceLevel = 0;
        public static int _experiencePrestige = 0;
        public static int _experienceMaxLevel = 90;
        public static PlayerClass _classType = PlayerClass.None;

        public static GameObject _legacyBarGameObject;
        public static GameObject _legacyInformationPanel;
        public static LocalizedText _firstLegacyStat;
        public static LocalizedText _secondLegacyStat;
        public static LocalizedText _thirdLegacyStat;
        public static LocalizedText _legacyHeader;
        public static LocalizedText _legacyText;
        public static Image _legacyFill;
        public static string _legacyType;
        public static float _legacyProgress = 0f;
        public static int _legacyLevel = 0;
        public static int _legacyPrestige = 0;
        public static int _legacyMaxLevel = 100;
        public static List<string> _legacyBonusStats = ["", "", ""];

        public static GameObject _expertiseBarGameObject;
        public static GameObject _expertiseInformationPanel;
        public static LocalizedText _firstExpertiseStat;
        public static LocalizedText _secondExpertiseStat;
        public static LocalizedText _thirdExpertiseStat;
        public static LocalizedText _expertiseHeader;
        public static LocalizedText _expertiseText;
        public static Image _expertiseFill;
        public static string _expertiseType;
        public static float _expertiseProgress = 0f;
        public static int _expertiseLevel = 0;
        public static int _expertisePrestige = 0;
        public static int _expertiseMaxLevel = 100;
        public static List<string> _expertiseBonusStats = ["", "", ""];

        public static GameObject _familiarBarGameObject;
        public static GameObject _familiarInformationPanel;
        public static LocalizedText _familiarMaxHealth;
        public static LocalizedText _familiarPhysicalPower;
        public static LocalizedText _familiarSpellPower;
        public static LocalizedText _familiarHeader;
        public static LocalizedText _familiarText;
        public static Image _familiarFill;
        public static float _familiarProgress = 0f;
        public static int _familiarLevel = 1;
        public static int _familiarPrestige = 0;
        public static int _familiarMaxLevel = 90;
        public static string _familiarName = "";
        public static List<string> _familiarStats = ["", "", ""];

        public static bool _equipmentBonus = false;
        public const float MAX_PROFESSION_LEVEL = 100f;
        public const float EQUIPMENT_BONUS = 0.1f;

        public static GameObject _enchantingBarGameObject;
        public static LocalizedText _enchantingLevelText;
        public static Image _enchantingProgressFill;
        public static Image _enchantingFill;
        public static float _enchantingProgress = 0f;
        public static int _enchantingLevel = 0;

        public static GameObject _alchemyBarGameObject;
        public static LocalizedText _alchemyLevelText;
        public static Image _alchemyProgressFill;
        public static Image _alchemyFill;
        public static float _alchemyProgress = 0f;
        public static int _alchemyLevel = 0;

        public static GameObject _harvestingGameObject;
        public static LocalizedText _harvestingLevelText;
        public static Image _harvestingProgressFill;
        public static Image _harvestingFill;
        public static float _harvestingProgress = 0f;
        public static int _harvestingLevel = 0;

        public static GameObject _blacksmithingBarGameObject;
        public static LocalizedText _blacksmithingLevelText;
        public static Image _blacksmithingProgressFill;
        public static Image _blacksmithingFill;
        public static float _blacksmithingProgress = 0f;
        public static int _blacksmithingLevel = 0;

        public static GameObject _tailoringBarGameObject;
        public static LocalizedText _tailoringLevelText;
        public static Image _tailoringProgressFill;
        public static Image _tailoringFill;
        public static float _tailoringProgress = 0f;
        public static int _tailoringLevel = 0;

        public static GameObject _woodcuttingBarGameObject;
        public static LocalizedText _woodcuttingLevelText;
        public static Image _woodcuttingProgressFill;
        public static Image _woodcuttingFill;
        public static float _woodcuttingProgress = 0f;
        public static int _woodcuttingLevel = 0;

        public static GameObject _miningBarGameObject;
        public static LocalizedText _miningLevelText;
        public static Image _miningProgressFill;
        public static Image _miningFill;
        public static float _miningProgress = 0f;
        public static int _miningLevel = 0;

        public static GameObject _fishingBarGameObject;
        public static LocalizedText _fishingLevelText;
        public static Image _fishingProgressFill;
        public static Image _fishingFill;
        public static float _fishingProgress = 0f;
        public static int _fishingLevel = 0;

        public static GameObject _dailyQuestObject;
        public static LocalizedText _dailyQuestHeader;
        public static LocalizedText _dailyQuestSubHeader;
        public static Image _dailyQuestIcon;
        public static TargetType _dailyTargetType = TargetType.Kill;
        public static int _dailyProgress = 0;
        public static int _dailyGoal = 0;
        public static string _dailyTarget = "";
        public static bool _dailyVBlood = false;

        public static GameObject _weeklyQuestObject;
        public static LocalizedText _weeklyQuestHeader;
        public static LocalizedText _weeklyQuestSubHeader;
        public static Image _weeklyQuestIcon;
        public static TargetType _weeklyTargetType = TargetType.Kill;
        public static int _weeklyProgress = 0;
        public static int _weeklyGoal = 0;
        public static string _weeklyTarget = "";
        public static bool _weeklyVBlood = false;

        public static PrefabGUID _abilityGroupPrefabGUID;

        public static AbilityTooltipData _abilityTooltipData;
        public static readonly ComponentType AbilityTooltipDataComponent = ComponentType.ReadOnly(Il2CppType.Of<AbilityTooltipData>());

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
        public const float COOLDOWN_FACTOR = 8f;

        public static double _cooldownEndTime = 0;
        public static float _cooldownRemaining = 0f;
        public static float _cooldownTime = 0f;
        public static int _currentCharges = 0;
        public static int _maxCharges = 0;
        public static double _chargeUpEndTime = 0;
        public static float _chargeUpTime = 0f;
        public static float _chargeUpTimeRemaining = 0f;
        public static float _chargeCooldownTime = 0f;

        public static int _layer;
        public static int _barNumber;
        public static int _graphBarNumber;
        public static float _horizontalBarHeaderFontSize;
        public static float _windowOffset;
        public static readonly Color BrightGold = new(1f, 0.8f, 0f, 1f);

        public const float BAR_HEIGHT_SPACING = 0.075f;
        public const float BAR_WIDTH_SPACING = 0.065f;

        public static readonly Dictionary<UIElement, GameObject> GameObjects = [];
        public static readonly Dictionary<GameObject, bool> ObjectStates = [];
        public static readonly List<GameObject> ProfessionObjects = [];

        public static readonly Dictionary<UIElement, string> AbilitySlotNamePaths = new()
        {
            { UIElement.Experience, "HUDCanvas(Clone)/BottomBarCanvas/BottomBar(Clone)/Content/Background/AbilityBar/AbilityBarEntry_Primary/" },
            { UIElement.Legacy, "HUDCanvas(Clone)/BottomBarCanvas/BottomBar(Clone)/Content/Background/AbilityBar/AbilityBarEntry_WeaponSkill1/" },
            { UIElement.Expertise, "HUDCanvas(Clone)/BottomBarCanvas/BottomBar(Clone)/Content/Background/AbilityBar/AbilityBarEntry_WeaponSkill2/" },
            { UIElement.Familiars, "HUDCanvas(Clone)/BottomBarCanvas/BottomBar(Clone)/Content/Background/AbilityBar/AbilityBarEntry_Travel/" },
            { UIElement.Professions, "HUDCanvas(Clone)/BottomBarCanvas/BottomBar(Clone)/Content/Background/AbilityBar/AbilityBarEntry_Spell1/" },
            { UIElement.Weekly, "HUDCanvas(Clone)/BottomBarCanvas/BottomBar(Clone)/Content/Background/AbilityBar/AbilityBarEntry_Spell2/" },
            { UIElement.Daily, "HUDCanvas(Clone)/BottomBarCanvas/BottomBar(Clone)/Content/Background/AbilityBar/AbilityBarEntry_Ultimate/" },
        };

        public static readonly Dictionary<UIElement, bool> UiElementsConfigured = new()
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

        public const string FISHING = "Go Fish!";

        public static bool _ready = false;
        public static bool _active = false;
        public static bool _shiftActive = false;
        public static bool _killSwitch = false;
    }
    public static class UpdateHUD
    {
        public static readonly PrefabGUID StatBuff = PrefabGUIDs.SetBonus_AllLeech_T09;
        public static readonly bool StatBuffActive = _legacyBar || _expertiseBar;

        public static readonly HashSet<GameObject> AttributeObjects = [];
        public static GameObject _attributeObjectPrefab;

        /*
        public static Dictionary<UnitStatType, LocalizedText> CombinedAttributeTexts => BloodAttributeTexts
            .Concat(WeaponAttributeTexts)
            .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
        */

        public static readonly Dictionary<UnitStatType, LocalizedText> BloodAttributeTexts = [];
        public static readonly Dictionary<UnitStatType, LocalizedText> WeaponAttributeTexts = [];

        public static readonly List<ModifyUnitStatBuff_DOTS> BloodStatBuffs = [default, default, default];
        public static readonly List<ModifyUnitStatBuff_DOTS> WeaponStatBuffs = [default, default, default];
        public static void UpdateAttributeType(UnitStatType unitStatType, Sprite sprite)
        {
            if (BloodAttributeTexts.TryGetValue(unitStatType, out LocalizedText localizedText))
            {
                ConfigureAttributeType(localizedText.gameObject, sprite);
            }
            else if (WeaponAttributeTexts.TryGetValue(unitStatType, out localizedText))
            {
                ConfigureAttributeType(localizedText.gameObject, sprite);
            }
        }
        public static DynamicBuffer<ModifyUnitStatBuff_DOTS> TryGetSourceBuffer()
        {
            /*
            if (!LocalCharacter.TryGetBuffer<ModifyUnitStatBuff_DOTS>(out var buffer))
            {
                if (!LocalCharacter.Exists())
                {
                    return default;
                }

                return EntityManager.AddBuffer<ModifyUnitStatBuff_DOTS>(LocalCharacter);
            }
            */

            if (!ModifyUnitStatBuffLookup.TryGetBuffer(LocalCharacter, out var buffer))
                return default;

            return buffer;
        }
        public static void UpdateTargetBuffer(ref DynamicBuffer<ModifyUnitStatBuff_DOTS> sourceBuffer)
        {
            if (!sourceBuffer.IsCreated)
                return;

            if (!LocalCharacter.TryGetBuff(StatBuff, out Entity buff))
                return;

            if (!ModifyUnitStatBuffLookup.TryGetBuffer(buff, out var targetBuffer))
                return;

            targetBuffer.CopyFrom(sourceBuffer);

            /*
            if (!Core.LocalCharacter.TryGetBuff(_statBuff, out Entity buffEntity)
                || !buffEntity.TryGetBuffer<ModifyUnitStatBuff_DOTS>(out var buffer)) return;

            Dictionary<UnitStatType, ModifyUnitStatBuff_DOTS> unitStatBuffs = [];
            HashSet<UnitStatType> activeUnitStats = [];
            HashSet<int> presentIdentifiers = [];

            foreach (var bloodStatBuff in _bloodStatBuffs)
            {
                if (!bloodStatBuff.Id.Id.Equals(0))
                {
                    unitStatBuffs[bloodStatBuff.StatType] = bloodStatBuff;
                }
            }

            foreach (var weaponStatBuff in _weaponStatBuffs)
            {
                if (!weaponStatBuff.Id.Id.Equals(0))
                {
                    unitStatBuffs[weaponStatBuff.StatType] = weaponStatBuff;
                }
            }

            for (int i = buffer.Length - 1; i >= 0; i--)
            {
                ModifyUnitStatBuff_DOTS unitStatBuff = buffer[i];
                UnitStatType unitStatType = unitStatBuff.StatType;
                int identifier = unitStatBuff.Id.Id;
                bool isActive = unitStatBuffs.ContainsKey(unitStatType);
                bool isPresent = presentIdentifiers.Contains(identifier);

                if (!isActive)
                {
                    buffer.RemoveAt(i);
                    TryClearAttribute(unitStatType);

                    Core.Log.LogWarning($"Clearing Attribute: {unitStatBuff.Id.Id}, {unitStatBuff.StatType}, {unitStatBuff.Value}");
                }
                else if (!isPresent)
                {
                    activeUnitStats.Add(unitStatType);
                }
                else
                {
                    presentIdentifiers.Add(identifier);
                }
            }

            foreach (var unitStat in activeUnitStats)
            {
                if (unitStatBuffs.TryGetValue(unitStat, out ModifyUnitStatBuff_DOTS statBuff))
                {
                    buffer.Add(statBuff);
                    // TrySetAttribute(unitStat, statBuff.Value);

                    Core.Log.LogWarning($"Setting Attribute: {statBuff.Id.Id}, {statBuff.StatType}, {statBuff.Value}");
                }
            }

            foreach (var unitStatBuff in unitStatBuffs)
            {
                ModifyUnitStatBuff_DOTS statBuff = unitStatBuff.Value;
                UnitStatType unitStatType = statBuff.StatType;
                float value = statBuff.Value;

                if (!pendingUnitStats.Contains(unitStatType))
                {
                    buffer.Add(statBuff);
                    TrySetAttribute(unitStatType, value);

                    Core.Log.LogWarning($"Setting Attribute: {statBuff.Id.Id}, {statBuff.StatType}, {statBuff.Value}");
                }
            }
            */
        }
        public static void UpdateAttributes(ref DynamicBuffer<ModifyUnitStatBuff_DOTS> sourceBuffer)
        {
            if (!AttributesInitialized)
            {
                TryInitializeAttributeValues();

                if (!ModifyUnitStatBuffLookup.TryGetBuffer(LocalCharacter, out var buffer))
                {
                    buffer = EntityManager.AddBuffer<ModifyUnitStatBuff_DOTS>(LocalCharacter);

                    buffer.EnsureCapacity(6);
                    buffer.ResizeUninitialized(6);
                }

                return;
            }

            HashSet<UnitStatType> activeUnitStats = [];

            for (int i = 0; i < sourceBuffer.Length; i++)
            {
                ModifyUnitStatBuff_DOTS unitStatBuff = sourceBuffer[i];
                UnitStatType unitStatType = unitStatBuff.StatType;

                int identifier = unitStatBuff.Id.Id;
                float value = unitStatBuff.Value;

                if (identifier == 0)
                {
                    Core.Log.LogWarning($"Skipping Attribute: {unitStatType}");
                    continue; // might be due to not having full blood stats instead?
                }

                if(BloodAttributeTexts.TryGetValue(unitStatType, out LocalizedText localizedText))
                {
                    string text = FormatAttributeValue(unitStatType, value);
                    // if (text != localizedText.GetText())
                    Core.Log.LogWarning($"Setting Blood Attribute: {unitStatType} to {text}");
                    localizedText.ForceSet(text);
                    activeUnitStats.Add(unitStatType);
                }
                else if (WeaponAttributeTexts.TryGetValue(unitStatType, out localizedText))
                {
                    string text = FormatAttributeValue(unitStatType, value);
                    // if (text != localizedText.GetText())
                    Core.Log.LogWarning($"Setting Weapon Attribute: {unitStatType} to {text}");
                    localizedText.ForceSet(text);
                    activeUnitStats.Add(unitStatType);
                }
            }

            foreach (var attributePair in BloodAttributeTexts)
            {
                if (!activeUnitStats.Contains(attributePair.Key))
                    // && !string.IsNullOrEmpty(attributePair.Value.GetText()))
                {
                    Core.Log.LogWarning($"Clearing Blood Attribute: {attributePair.Key}");
                    attributePair.Value.ForceSet(string.Empty);
                }
            }

            foreach (var attributePair in WeaponAttributeTexts)
            {
                if (!activeUnitStats.Contains(attributePair.Key))
                    // && !string.IsNullOrEmpty(attributePair.Value.GetText()))
                {
                    Core.Log.LogWarning($"Clearing Weapon Attribute: {attributePair.Key}");
                    attributePair.Value.ForceSet(string.Empty);
                }
            }

            /*
            foreach (var attributePair in CombinedAttributeTexts)
            {
                if (!activeUnitStats.Contains(attributePair.Key))
                    // && !string.IsNullOrEmpty(attributePair.Value.GetText()))
                {
                    attributePair.Value.ForceSet(string.Empty);
                }
            }
            */
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string GetWeaponStatInfo(int i, string statType)
        {
            if (Enum.TryParse(statType, out WeaponStatType weaponStatType))
            {
                if (_weaponStatValues.TryGetValue(weaponStatType, out float statValue))
                {
                    float classMultiplier = ClassSynergy(weaponStatType, _classType, _classStatSynergies);
                    statValue *= (1 + (_prestigeStatMultiplier * _expertisePrestige)) * classMultiplier * ((float)_expertiseLevel / _expertiseMaxLevel);

                    int statModificationId = ModificationIds.GenerateId(0, (int)weaponStatType, statValue);
                    UnitStatType unitStatType = (UnitStatType)Enum.Parse(typeof(UnitStatType), weaponStatType.ToString());

                    WeaponStatBuffs[i] = new()
                    {
                        StatType = unitStatType,
                        ModificationType = ModificationType.Add,
                        Value = statValue,
                        Modifier = 1,
                        IncreaseByStacks = false,
                        ValueByStacks = 0,
                        Priority = 0,
                        Id = new(statModificationId)
                    };

                    return FormatWeaponStatBar(weaponStatType, statValue);
                }
            }

            return string.Empty;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string GetBloodStatInfo(int i, string statType)
        {
            if (Enum.TryParse(statType, out BloodStatType bloodStat))
            {
                if (_bloodStatValues.TryGetValue(bloodStat, out float statValue))
                {
                    float classMultiplier = ClassSynergy(bloodStat, _classType, _classStatSynergies);
                    statValue *= (1 + (_prestigeStatMultiplier * _legacyPrestige)) * classMultiplier * ((float)_legacyLevel / _legacyMaxLevel);

                    string displayString = $"<color=#00FFFF>{BloodStatTypeAbbreviations[bloodStat]}</color>: <color=#90EE90>{(statValue * 100).ToString("F0") + "%"}</color>";
                    int statModificationId = ModificationIds.GenerateId(1, (int)bloodStat, statValue);
                    UnitStatType unitStatType = (UnitStatType)Enum.Parse(typeof(UnitStatType), bloodStat.ToString());

                    BloodStatBuffs[i] = new()
                    {
                        StatType = unitStatType,
                        ModificationType = ModificationType.Add,
                        Value = statValue,
                        Modifier = 1,
                        IncreaseByStacks = false,
                        ValueByStacks = 0,
                        Priority = 0,
                        Id = new(statModificationId)
                    };

                    return displayString;
                }
            }

            return string.Empty;
        }
        public static void UpdateAbilityData(AbilityTooltipData abilityTooltipData, Entity abilityGroupEntity,
            Entity abilityCastEntity, PrefabGUID abilityGroupPrefabGUID)
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
        public static bool UpdateTooltipData(Entity abilityGroupEntity, PrefabGUID abilityGroupPrefabGUID)
        {
            if (_abilityTooltipData == null || _abilityGroupPrefabGUID != abilityGroupPrefabGUID)
            {
                if (abilityGroupEntity.TryGetComponentObject(EntityManager, out _abilityTooltipData))
                {
                    _abilityTooltipData ??= EntityManager.GetComponentObject<AbilityTooltipData>(abilityGroupEntity, AbilityTooltipDataComponent);
                }
            }

            return _abilityTooltipData != null;
        }
        public static void UpdateProfessions(float progress, int level, LocalizedText levelText,
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
        public static void UpdateBar(float progress, int level, int maxLevel,
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

            if (element.Equals(UIElement.Expertise))
            {
                type = SplitPascalCase(type);
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
                string header = string.Empty;

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
        public static void UpdateClass(PlayerClass classType, LocalizedText classText)
        {
            if (_killSwitch) return;

            if (classType != PlayerClass.None)
            {
                if (!classText.enabled) classText.enabled = true;
                if (!classText.gameObject.active) classText.gameObject.SetActive(true);

                string formattedClassName = FormatClassName(classType);
                classText.ForceSet(formattedClassName);

                if (ClassColorHexMap.TryGetValue(classType, out Color classColor))
                {
                    classText.Text.color = classColor;
                }
            }
            else
            {
                classText.ForceSet(string.Empty);
                classText.enabled = false;
            }
        }
        public static void UpdateBloodStats(List<string> bonusStats, List<LocalizedText> statTexts,
            ref DynamicBuffer<ModifyUnitStatBuff_DOTS> buffer, Func<int, string, string> getStatInfo)
        {
            for (int i = 0; i < 3; i++)
            {
                if (bonusStats[i] != "None")
                {
                    if (!statTexts[i].enabled)
                        statTexts[i].enabled = true;

                    if (!statTexts[i].gameObject.active)
                        statTexts[i].gameObject.SetActive(true);

                    statTexts[i].ForceSet(getStatInfo(i, bonusStats[i]));

                    if (buffer.IsCreated
                        && BloodStatBuffs[i].Id.Id != 0)
                    {
                        buffer[i] = BloodStatBuffs[i];
                    }
                }
                else if (bonusStats[i] == "None" && statTexts[i].enabled)
                {
                    statTexts[i].ForceSet(string.Empty);
                    statTexts[i].enabled = false;

                    BloodStatBuffs[i] = default;
                    if (buffer.IsCreated)
                    {
                        buffer[i] = default;
                    }
                }
            }
        }
        public static void UpdateWeaponStats(List<string> bonusStats, List<LocalizedText> statTexts,
            ref DynamicBuffer<ModifyUnitStatBuff_DOTS> buffer, Func<int, string, string> getStatInfo)
        {
            for (int i = 0; i < 3; i++)
            {
                int j = i + 3; // Weapon stats -> second half of buffer

                if (bonusStats[i] != "None")
                {
                    if (!statTexts[i].enabled)
                        statTexts[i].enabled = true;

                    if (!statTexts[i].gameObject.active)
                        statTexts[i].gameObject.SetActive(true);

                    statTexts[i].ForceSet(getStatInfo(i, bonusStats[i]));

                    if (buffer.IsCreated
                        && WeaponStatBuffs[i].Id.Id != 0)
                    {
                        buffer[j] = WeaponStatBuffs[i];
                    }
                }
                else if (bonusStats[i] == "None" && statTexts[i].enabled)
                {
                    statTexts[i].ForceSet(string.Empty);
                    statTexts[i].enabled = false;

                    WeaponStatBuffs[i] = default;
                    if (buffer.IsCreated)
                    {
                        buffer[i] = default;
                    }
                }
            }
        }
        public static void UpdateFamiliarStats(List<string> familiarStats, List<LocalizedText> statTexts)
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
        public static void UpdateQuests(GameObject questObject, LocalizedText questSubHeader, Image questIcon,
            TargetType targetType, string target, int progress, int goal, bool isVBlood)
        {
            if (_killSwitch) return;

            if (progress != goal && ObjectStates[questObject])
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
    }
    public static class ToggleHUD
    {
        public static readonly Dictionary<int, Action> ActionToggles = new()
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
        static void DailyQuestToggle()
        {
            bool active = !_dailyQuestObject.activeSelf;

            _dailyQuestObject.SetActive(active);
            ObjectStates[_dailyQuestObject] = active;
        }
        static void ExperienceToggle()
        {
            bool active = !_experienceBarGameObject.activeSelf;

            _experienceBarGameObject.SetActive(active);
            ObjectStates[_experienceBarGameObject] = active;
        }
        static void ExpertiseToggle()
        {
            bool active = !_expertiseBarGameObject.activeSelf;

            _expertiseBarGameObject.SetActive(active);
            ObjectStates[_expertiseBarGameObject] = active;
        }
        static void FamiliarToggle()
        {
            bool active = !_familiarBarGameObject.activeSelf;

            _familiarBarGameObject.SetActive(active);
            ObjectStates[_familiarBarGameObject] = active;
        }
        static void LegacyToggle()
        {
            bool active = !_legacyBarGameObject.activeSelf;

            _legacyBarGameObject.SetActive(active);
            ObjectStates[_legacyBarGameObject] = active;
        }
        static void ProfessionToggle()
        {
            foreach (GameObject professionObject in ProfessionObjects)
            {
                Core.Log.LogWarning($"Toggling profession object: {professionObject.name} ({professionObject.activeSelf})");
                bool active = !professionObject.activeSelf;

                professionObject.SetActive(active);
                ObjectStates[professionObject] = active;
            }

            Core.Log.LogWarning($"Toggled profession objects ({ProfessionObjects.Count})");
        }
        static void ShiftSlotToggle()
        {
            bool active = !_abilityDummyObject.activeSelf;

            _abilityDummyObject.SetActive(active);
            ObjectStates[_abilityDummyObject] = active;
        }
        public static void ToggleAllObjects()
        {
            _active = !_active;

            foreach (GameObject gameObject in ObjectStates.Keys)
            {
                gameObject.active = _active;
                ObjectStates[gameObject] = _active;
            }
        }
        public static void ToggleGameObjects(params GameObject[] gameObjects)
        {
            foreach (GameObject gameObject in gameObjects)
            {
                bool newState = !gameObject.activeSelf;
                gameObject.SetActive(newState);

                ObjectStates[gameObject] = newState;
            }
        }
        static void WeeklyQuestToggle()
        {
            bool active = !_weeklyQuestObject.activeSelf;

            _weeklyQuestObject.SetActive(active);
            ObjectStates[_weeklyQuestObject] = active;
        }
    }
    public static class InputHUD
    {
        public static bool IsGamepad => InputActionSystemPatch.IsGamepad;
        static ControllerType _inputDevice = ControllerType.KeyboardAndMouse;
        public readonly struct InputAdaptiveElement(GameObject adaptiveObject, Vector2 keyboardMousePos, Vector2 keyboardMouseAnchorMin,
            Vector2 keyboardMouseAnchorMax, Vector2 keyboardMousePivot, Vector3 keyboardMouseScale, Vector2 controllerPos,
            Vector2 controllerAnchorMin, Vector2 controllerAnchorMax, Vector2 controllerPivot, Vector3 controllerScale)
        {
            public readonly GameObject AdaptiveObject = adaptiveObject;

            public readonly Vector2 KeyboardMouseAnchoredPosition = keyboardMousePos;
            public readonly Vector2 KeyboardMouseAnchorMin = keyboardMouseAnchorMin;
            public readonly Vector2 KeyboardMouseAnchorMax = keyboardMouseAnchorMax;
            public readonly Vector2 KeyboardMousePivot = keyboardMousePivot;
            public readonly Vector3 KeyboardMouseScale = keyboardMouseScale;

            public readonly Vector2 ControllerAnchoredPosition = controllerPos;
            public readonly Vector2 ControllerAnchorMin = controllerAnchorMin;
            public readonly Vector2 ControllerAnchorMax = controllerAnchorMax;
            public readonly Vector2 ControllerPivot = controllerPivot;
            public readonly Vector3 ControllerScale = controllerScale;
        }
        public static readonly List<InputAdaptiveElement> AdaptiveElements = [];
        public static void SyncInputHUD()
        {
            bool isSynced = IsGamepad
                ? _inputDevice.Equals(ControllerType.Gamepad)
                : _inputDevice.Equals(ControllerType.KeyboardAndMouse);

            if (!isSynced)
                SyncAdaptiveElements(IsGamepad);
        }
        public static void RegisterAdaptiveElement(GameObject adaptiveObject, Vector2 keyboardMousePos, Vector2 keyboardMouseAnchorMin,
            Vector2 keyboardMouseAnchorMax, Vector2 keyboardMousePivot, Vector3 keyboardMouseScale, Vector2 controllerPos,
            Vector2 controllerAnchorMin, Vector2 controllerAnchorMax, Vector2 controllerPivot, Vector3 controllerScale)
        {
            if (adaptiveObject == null) return;

            AdaptiveElements.Add(new InputAdaptiveElement(adaptiveObject, keyboardMousePos, keyboardMouseAnchorMin,
                keyboardMouseAnchorMax, keyboardMousePivot, keyboardMouseScale, controllerPos,
                controllerAnchorMin, controllerAnchorMax, controllerPivot, controllerScale));
        }
        public static void SyncAdaptiveElements(bool isGamepad)
        {
            _inputDevice = isGamepad ? ControllerType.Gamepad : ControllerType.KeyboardAndMouse;
            Core.Log.LogWarning($"[OnInputDeviceChanged] - ControllerType: {_inputDevice}");

            foreach (InputAdaptiveElement adaptiveElement in AdaptiveElements)
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
    public static class ConfigureHUD
    {
        public static readonly bool _experienceBar = Plugin.Leveling;
        public static readonly bool _showPrestige = Plugin.Prestige;
        public static readonly bool _legacyBar = Plugin.Legacies;
        public static readonly bool _expertiseBar = Plugin.Expertise;
        public static readonly bool _familiarBar = Plugin.Familiars;
        public static readonly bool _professionBars = Plugin.Professions;
        public static readonly bool _questTracker = Plugin.Quests;
        public static readonly bool _shiftSlot = Plugin.ShiftSlot;
        public static void ConfigureShiftSlot(ref GameObject shiftSlotObject, ref AbilityBarEntry shiftSlotEntry, ref AbilityBarEntry.UIState uiState, ref GameObject cooldownObject,
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

                        ObjectStates.Add(shiftSlotObject, true);
                DataHUD.GameObjects.Add(UIElement.ShiftSlot, shiftSlotObject);

                        SimpleStunButton stunButton = shiftSlotObject.AddComponent<SimpleStunButton>();
                        if (ToggleHUD.ActionToggles.TryGetValue((int)UIElement.ShiftSlot, out var toggleAction))
                        {
                            stunButton.onClick.AddListener(new Action(toggleAction));
                        }
                    }
                    else
                    {
                        Core.Log.LogWarning("AbilityBarEntry_Dummy is null!");
                    }
                }
        public static void ConfigureQuestWindow(ref GameObject questObject, UIElement questType, Color headerColor,
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
            DataHUD.GameObjects.Add(questType, questObject);
            ObjectStates.Add(questObject, true);
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

            ObjectStates.Add(barGameObject, true);
            DataHUD.GameObjects.Add(element, barGameObject);
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
            Sprite professionIcon = ProfessionIcons.TryGetValue(profession, out string spriteName) && Sprites.TryGetValue(spriteName, out Sprite sprite) ? sprite : levelBackgroundImage.sprite;
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
            maxFill.color = BrightGold;

            // Increment GraphBarNumber for horizontal spacing within the bar graph
            _graphBarNumber++;

            barGameObject.SetActive(true);
            level.gameObject.SetActive(false);

            ObjectStates.Add(barGameObject, true);
            ProfessionObjects.Add(barGameObject);

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
        public static void ConfigureInformationPanel(ref GameObject informationPanelObject, ref LocalizedText firstText, ref LocalizedText secondText,
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
        public static void ConfigureExperiencePanel(ref GameObject panel, ref LocalizedText firstText, ref LocalizedText secondText,
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
        public static void ConfigureDefaultPanel(ref GameObject panel, ref LocalizedText firstText, ref LocalizedText secondText,
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
        public static void ConfigureAttributeType(GameObject attributeSpriteClone, Sprite sprite)
        {
            TextMeshProUGUI textMeshPro = attributeSpriteClone.GetComponent<TextMeshProUGUI>();
            textMeshPro.spriteAsset = Utilities.GameObjects.CreateSpriteAsset(sprite);
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
        public static void ConfigureAttributeSynergy(GameObject attributeSynergyClone, Sprite sprite)
        {
            TextMeshProUGUI textMeshPro = attributeSynergyClone.GetComponent<TextMeshProUGUI>();
            textMeshPro.spriteAsset = Utilities.GameObjects.CreateSpriteAsset(sprite);
            textMeshPro.m_spriteColor = Color.white;

            LayoutElement layoutElement = attributeSynergyClone.GetComponent<LayoutElement>();
            LocalizedText localizedText = attributeSynergyClone.GetComponent<LocalizedText>();

            layoutElement.flexibleWidth = 1f;
            localizedText.ForceSet(string.Empty);

            attributeSynergyClone.transform.SetSiblingIndex(1);
            textMeshPro.autoSizeTextContainer = true;
            textMeshPro.enableWordWrapping = false;

            attributeSynergyClone.SetActive(true);
        }
        public static void ConfigureAttributeButton(SimpleStunButton button, string command)
            => button.onClick.AddListener((UnityAction)(() => Quips.SendCommand(command)));
        public static void ConfigureAttributeObjects(SimpleStunButton simpleStunButton, GameObject attributeEntryObject,
            GameObject gameObject, GameObject attributeValue, GameObject attributeValueClone,
            // GameObject attributeTypeClone, GameObject attibuteSynergyClone,
            UnitStatType unitStatType)
        {
            HorizontalLayoutGroup horizontalLayoutGroup = gameObject.GetComponent<HorizontalLayoutGroup>();
            TextMeshProUGUI textMeshPro = attributeValue.GetComponent<TextMeshProUGUI>();

            LayoutElement layoutElement = attributeValueClone.GetComponent<LayoutElement>();
            LocalizedText localizedText = attributeValueClone.GetComponent<LocalizedText>();

            bool isValidStat = false;

            if (Enum.TryParse(unitStatType.ToString(), true, out BloodStatType bloodStatType)
                && _bloodStatValues.ContainsKey(bloodStatType))
            {
                ConfigureAttributeButton(simpleStunButton, $".bl cst {(int)bloodStatType}");
                // attributeEntryObject Image color thing?
                // ConfigureAttributeType(attributeTypeClone, _sprites["BloodTypeFrame"]);
                // ConfigureAttributeSynergy(attibuteSynergyClone, _sprites["Attribute_TierIndicator_Fixed"]);

                BloodAttributeTexts[unitStatType] = localizedText;
                // CombinedAttributeTexts[unitStatType] = localizedText;
                AttributeObjects.Add(attributeValueClone);
                // _attributeObjects.Add(attributeTypeClone);

                Core.Log.LogWarning($"Registered Blood Attribute: {unitStatType}");
                isValidStat = true;
            }

            if (Enum.TryParse(unitStatType.ToString(), true, out WeaponStatType weaponStatType)
                && _weaponStatValues.ContainsKey(weaponStatType))
            {
                ConfigureAttributeButton(simpleStunButton, $".wep cst {(int)weaponStatType}");
                // ConfigureAttributeType(attributeTypeClone, _sprites["BloodTypeIcon_Tiny_Warrior"]);
                // ConfigureAttributeSynergy(attibuteSynergyClone, _sprites["Attribute_TierIndicator_Fixed"]);

                WeaponAttributeTexts[unitStatType] = localizedText;
                // CombinedAttributeTexts[unitStatType] = localizedText;
                AttributeObjects.Add(attributeValueClone);
                // _attributeObjects.Add(attributeTypeClone);

                // if (_lastSeen.TryGetValue(unitStatType, out float statValue) && statValue != 0f) TrySetAttribute(unitStatType, statValue);
                Core.Log.LogWarning($"Registered Weapon Attribute: {unitStatType}");
                isValidStat = true;
            }

            if (!isValidStat)
                AttributeObjects.Add(attributeValueClone); // need to refactor so not making extra objects per stat geez

            horizontalLayoutGroup.childForceExpandWidth = false;
            layoutElement.flexibleWidth = 1f;
            attributeValueClone.transform.SetSiblingIndex(1);
            textMeshPro.autoSizeTextContainer = true;
            textMeshPro.enableWordWrapping = false;

            localizedText.ForceSet(string.Empty);
            attributeValueClone.SetActive(true);
        }
    }
    public static class UtilitiesHUD
    {
        public static float ClassSynergy<T>(T statType, PlayerClass classType, Dictionary<PlayerClass, (List<WeaponStatType> WeaponStatTypes, List<BloodStatType> BloodStatTypes)> classStatSynergy)
        {
            if (classType.Equals(PlayerClass.None))
                return 1f;

            if (typeof(T) == typeof(WeaponStatType) && classStatSynergy[classType].WeaponStatTypes.Contains((WeaponStatType)(object)statType))
            {
                return _classStatMultiplier;
            }

            if (typeof(T) == typeof(BloodStatType) && classStatSynergy[classType].BloodStatTypes.Contains((BloodStatType)(object)statType))
            {
                return _classStatMultiplier;
            }

            return 1f;
        }
        public static string FormatWeaponStatBar(WeaponStatType weaponStat, float statValue)
        {
            string statValueString = WeaponStatFormats[weaponStat] switch
            {
                "integer" => ((int)statValue).ToString(),
                "decimal" => statValue.ToString("0.#"),
                "percentage" => (statValue * 100f).ToString("0.#") + "%",
                _ => statValue.ToString(),
            };

            return $"<color=#00FFFF>{WeaponStatTypeAbbreviations[weaponStat]}</color>: <color=#90EE90>{statValueString}</color>";
        }
        public static string FormatAttributeValue(UnitStatType unitStatType, float statValue)
        {
            string statString = $"<color=#90EE90>+{statValue * 100f:F0}%</color>";

            if (Enum.TryParse(unitStatType.ToString(), out WeaponStatType weaponStatType))
                statString = FormatWeaponAttribute(weaponStatType, statValue);

            return statString;
        }
        public static string FormatWeaponAttribute(WeaponStatType weaponStat, float statValue)
        {
            string statValueString = WeaponStatFormats[weaponStat] switch
            {
                "integer" => ((int)statValue).ToString(),
                "decimal" => statValue.ToString("0.#"),
                "percentage" => (statValue * 100f).ToString("0.#") + "%",
                _ => statValue.ToString(),
            };

            return $"<color=#90EE90>+{statValueString}</color>";
        }
        public static string IntegerToRoman(int num)
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
        public static string FormatClassName(PlayerClass classType)
        {
            return ClassNameRegex.Replace(classType.ToString(), " $1");
        }
        public static string TrimToFirstWord(string input)
        {
            int firstSpaceIndex = input.IndexOf(' ');
            int secondSpaceIndex = input.IndexOf(' ', firstSpaceIndex + 1);
            // bool isProperTitle = input.StartsWith("Sir") || input.StartsWith("Lord") || input.StartsWith("General") || input.StartsWith("Baron");

            if (firstSpaceIndex > 0 && secondSpaceIndex > 0)
            {
                // if (isProperTitle)
                    // return input[..++firstSpaceIndex];

                return input[..firstSpaceIndex];
            }

            return input;
        }
        public static string SplitPascalCase(string input)
        {
            return input.SpacedPascalCase();
        }
        public static void FindSprites()
        {
            Il2CppArrayBase<Sprite> sprites = UnityEngine.Resources.FindObjectsOfTypeAll<Sprite>();

            foreach (Sprite sprite in sprites)
            {
                if (SpriteNames.Contains(sprite.name) && !Sprites.ContainsKey(sprite.name))
                {
                    Sprites[sprite.name] = sprite;

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
}
