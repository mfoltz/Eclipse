using Eclipse.Resources;
using ProjectM;
using ProjectM.Hybrid;
using ProjectM.UI;
using Stunlock.Core;
using Stunlock.Localization;
using System.Collections;
using System.Collections.Concurrent;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;
using static Eclipse.Core;
using static Eclipse.Utilities.ShadowMatter.PrimalData.PrimalSettings;
using static Eclipse.Utilities.ShadowMatter.PrimalItem;
using static Eclipse.Utilities.ShadowMatter.PrimalItem.PrimalClient;
using static Eclipse.Utilities.ShadowMatter.PrimalItem.PrimalShared;

namespace Eclipse.Utilities;
internal static class ShadowMatter
{
    const string WEAPON_1H_FEMALE = "HybridModels/HYB_VampireFemale_Prefab(Clone)/VampireFemale_Standard_LOD00_rig/Root_JNT/Pelvis_JNT/Spine_JNT/Gut_JNT/Chest_JNT/Ribcage_JNT/Left_Collar_JNT/Left_Shoulder_JNT/Left_Elbow_JNT/Weapon02_JNT";
    const string WEAPON_2H_FEMALE = "HybridModels/HYB_VampireFemale_Prefab(Clone)/VampireFemale_Standard_LOD00_rig/Root_JNT/Pelvis_JNT/Spine_JNT/Gut_JNT/Chest_JNT/Ribcage_JNT/Right_Collar_JNT/Right_Shoulder_JNT/Right_Elbow_JNT/Weapon01_JNT";

    const string WEAPON_1H_MALE = "HybridModels/HYB_VampireMale_Prefab(Clone)/VampireMale_Standard_LOD00_rig/Root_JNT/Pelvis_JNT/Spine_JNT/Gut_JNT/Chest_JNT/Ribcage_JNT/Left_Collar_JNT/Left_Shoulder_JNT/Left_Elbow_JNT/Weapon02_JNT";
    const string WEAPON_2H_MALE = "HybridModels/HYB_VampireMale_Prefab(Clone)/VampireMale_Standard_LOD00_rig/Root_JNT/Pelvis_JNT/Spine_JNT/Gut_JNT/Chest_JNT/Ribcage_JNT/Right_Collar_JNT/Right_Shoulder_JNT/Right_Elbow_JNT/Weapon01_JNT";

    static WaitForSeconds WaitForSeconds { get; } = new(WAIT_FOR_SECONDS);
    const float WAIT_FOR_SECONDS = 0.25f; // 0.5f -> 0.25*f;

    static PrimalData PrimalItems { get; } = new(PrimalData.PrimalItems);
    static HashSet<PrefabGUID> EquipBuffs { get; } = [..PrimalData.PrimalItems.Select(data => data.EquipBuff)];
    static Dictionary<PrefabGUID, PrefabGUID> BuffWeaponLookup { get; } = PrimalData.PrimalItems.ToDictionary(data => data.EquipBuff, data => data.ItemWeapon);

    static readonly ConcurrentDictionary<PrefabGUID, (Material Material, Mesh Mesh)> _managedAssets = []; // EquipBuff -> (Material, Mesh)

    public readonly record struct PrimalData(ReadOnlyMemory<PrimalItem> Items)
    {
        internal static PrimalItem[] PrimalItems { get; } =
        [
            new(new(PrefabGUIDs.Item_Weapon_Spear_T09_ShadowMatter, PrefabGUIDs.EquipBuff_Weapon_Pollaxe_Ability01),
                PowerInfo.Default,
                AbilityInfo.Attack(PrefabGUIDs.AB_Vampire_Spear_Primary_Attack_Group),
                AbilityInfo.Offensive(PrefabGUIDs.AB_Vampire_BloodKnight_SkeweringLeap_AbilityGroup),
                AbilityInfo.Defensive(PrefabGUIDs.AB_Vampire_BloodKnight_SpearTwirl_AbilityGroup),
                AbilityInfo.Ultimate(PrefabGUIDs.AB_Vampire_BloodKnight_ThousandSpears_AbilityGroup),
                AbilityInfo.Dash(PrefabGUIDs.AB_Vampire_BloodKnight_Dash_AbilityGroup),
                new("Part_Dracula_SwordFloor_PLACEHOLDER/DraculaSword_1H_Static"),
                new(title:"<color=#C52443>Velvet Thorn</color>")),
            new(new(PrefabGUIDs.Item_Weapon_GreatSword_T09_ShadowMatter, PrefabGUIDs.EquipBuff_Weapon_Pollaxe_Ability02),
                PowerInfo.Default,
                AbilityInfo.Attack(PrefabGUIDs.AB_Vampire_GreatSword_Primary_Moving_AbilityGroup),
                AbilityInfo.Offensive(PrefabGUIDs.AB_HighLord_SwordDashCleave_AbilityGroup),
                AbilityInfo.Projectile(PrefabGUIDs.AB_HighLord_UnholySkill_AbilityGroup),
                AbilityInfo.Ultimate(PrefabGUIDs.AB_HighLord_CorpseStorm_AbilityGroup),
                AbilityInfo.Dash(PrefabGUIDs.AB_HighLord_UnholyWarp_AbilityGroup),
                new("Model_Weapon_SanguineGreatsword_Legendary_Unholy_Prefab/SanguineGreatsword_2H_Standard/LOD00_GRP/SteelGreatsword_GEO", true),
                new(title:"<color=#C52443>Nightfall Edge</color>")),
        ];

        public static PrimalSettings Settings { get; } = Default;

        public readonly record struct PrimalSettings(float WeaponLevel, float PhysicalPower, float SpellPower,
            float OffensiveCd, float DefensiveCd, float ProjectileCd, float UltimateCd, float DashCd)
        {
            public const float WEAPON_LVL = 100f;
            public const float PHYSICAL_PWR = 35f;
            public const float SPELL_PWR = 10f;

            public const float OFFENSIVE_CD = 8f;
            public const float DEFENSIVE_CD = 10f;
            public const float PROJECTILE_CD = 5f;

            public const float ULTIMATE_CD = 60f;
            public const float DASH_CD = 6f;

            public static PrimalSettings Default { get; } = new(WEAPON_LVL, PHYSICAL_PWR, SPELL_PWR, OFFENSIVE_CD, DEFENSIVE_CD, PROJECTILE_CD, ULTIMATE_CD, DASH_CD);
        }

        public ReadOnlySpan<PrimalItem>.Enumerator GetEnumerator()
            => Items.Span.GetEnumerator();
    }

    public readonly struct PrimalItem(PrimalBase weaponBase, PowerInfo weaponPower = default,
        AbilityInfo attackAbility = default, AbilityInfo primaryAbility = default, AbilityInfo secondaryAbility = default,
        AbilityInfo ultimateAbility = default, AbilityInfo dashAbility = default, ModelInfo itemModel = default,
        TooltipInfo itemInfo = default, TooltipInfo attackInfo = default, TooltipInfo primaryInfo = default,
        TooltipInfo secondaryInfo = default, TooltipInfo ultimateInfo = default, TooltipInfo dashInfo = default)
    {
        public PrefabGUID ItemWeapon
            => new(WeaponBase.WeaponGuid);
        public PrefabGUID EquipBuff
            => new(WeaponBase.BuffGuid);

        public readonly struct PrimalBase(PrefabGUID itemWeapon, PrefabGUID equipBuff)
        {
            public readonly int WeaponGuid = itemWeapon.GuidHash;
            public readonly int BuffGuid = equipBuff.GuidHash;
        }

        public PrimalBase WeaponBase { get; } = weaponBase;

        public PrefabGUID AttackGroup
            => WeaponShared.AttackSlot.AbilityGroup;
        public PrefabGUID PrimaryGroup
            => WeaponShared.PrimarySlot.AbilityGroup;
        public PrefabGUID SecondaryGroup
            => WeaponShared.SecondarySlot.AbilityGroup;
        public PrefabGUID UltimateGroup
            => WeaponShared.UltimateSlot.AbilityGroup;
        public PrefabGUID DashGroup
            => WeaponShared.DashSlot.AbilityGroup;

        public PrimalShared WeaponShared { get; } = new(weaponPower, attackAbility, primaryAbility, secondaryAbility, ultimateAbility, dashAbility);

        public readonly struct PrimalShared(PowerInfo weaponPower,
                AbilityInfo attackAbility, AbilityInfo primaryAbility, AbilityInfo secondaryAbility,
                AbilityInfo ultimateAbility, AbilityInfo dashAbility)
        {
            public PowerInfo WeaponPower { get; } = weaponPower;

            public readonly struct PowerInfo(float weaponLevel, float physicalPower, float spellPower)
            {
                public readonly float WeaponLevel = weaponLevel;
                public readonly float PhysicalPower = physicalPower;
                public readonly float SpellPower = spellPower;

                public static readonly PowerInfo Default = new(WEAPON_LVL, PHYSICAL_PWR, SPELL_PWR);
                public static readonly PowerInfo Scholar = new(WEAPON_LVL, SPELL_PWR, PHYSICAL_PWR);
            }

            public AbilityInfo AttackSlot
                => AbilityInfos[0];
            public AbilityInfo PrimarySlot
                => AbilityInfos[1];
            public AbilityInfo SecondarySlot
                => AbilityInfos[2];
            public AbilityInfo UltimateSlot
                => AbilityInfos[3];
            public AbilityInfo DashSlot
                => AbilityInfos[4];

            public readonly struct AbilityInfo(PrefabGUID abilityGroup, float cooldown = default)
            {
                public readonly PrefabGUID AbilityGroup = abilityGroup;
                public readonly float Cooldown = cooldown;
                public static AbilityInfo Attack(PrefabGUID abilityGroup)
                    => new(abilityGroup);
                public static AbilityInfo Offensive(PrefabGUID abilityGroup)
                    => new(abilityGroup, OFFENSIVE_CD);
                public static AbilityInfo Defensive(PrefabGUID abilityGroup)
                    => new(abilityGroup, DEFENSIVE_CD);
                public static AbilityInfo Projectile(PrefabGUID abilityGroup)
                    => new(abilityGroup, PROJECTILE_CD);
                public static AbilityInfo Ultimate(PrefabGUID abilityGroup)
                    => new(abilityGroup, ULTIMATE_CD);
                public static AbilityInfo Dash(PrefabGUID abilityGroup)
                    => new(abilityGroup, DASH_CD);
            }

            internal AbilityData AbilityInfos { get; } = new(attackAbility, primaryAbility, secondaryAbility, ultimateAbility, dashAbility);

            public readonly record struct AbilityData(ReadOnlyMemory<AbilityInfo> AbilityInfos)
            {
                public AbilityInfo this[int index]
                    => AbilityInfos.Span[index];

                public ReadOnlySpan<AbilityInfo>.Enumerator GetEnumerator()
                    => AbilityInfos.Span.GetEnumerator();

                public AbilityData(AbilityInfo attack, AbilityInfo primary, AbilityInfo secondary, AbilityInfo ultimate,
                    AbilityInfo dash) : this(new AbilityInfo[] { attack, primary, secondary, ultimate, dash }) { }
            }
        }

        public PrimalClient WeaponClient { get; } = new(itemModel, itemInfo, attackInfo, primaryInfo, secondaryInfo, ultimateInfo, dashInfo);

        public readonly struct PrimalClient(ModelInfo weaponModel, TooltipInfo itemInfo, TooltipInfo attackInfo,
                TooltipInfo primaryInfo, TooltipInfo secondaryInfo,
                TooltipInfo ultimateInfo, TooltipInfo dashInfo)
        {
            public bool HasMesh
                => WeaponModel.HasMesh;

            public ModelInfo WeaponModel { get; } = weaponModel;

            public readonly struct ModelInfo(string ScenePath, bool HasMesh = false)
            {
                public readonly string ScenePath = ScenePath;
                public readonly bool HasMesh = HasMesh;
            }

            public bool HasItemIcon
                => TooltipInfos.HasItemIcon;
            public bool HasItemText
                => TooltipInfos.HasItemText;
            public bool HasAbilityIcon
                => TooltipInfos.HasAbilityIcon;
            public bool HasAbilityText
                => TooltipInfos.HasAbilityText;

            public TooltipInfo ItemInfo
                => TooltipInfos[0];
            public TooltipInfo AttackInfo
                => TooltipInfos[1];
            public TooltipInfo PrimaryInfo
                => TooltipInfos[2];
            public TooltipInfo SecondaryInfo
                => TooltipInfos[3];
            public TooltipInfo UltimateInfo
                => TooltipInfos[4];
            public TooltipInfo DashInfo
                => TooltipInfos[5];

            public readonly struct TooltipInfo
            {
                public bool HasText
                    => !Title.IsEmpty() || !Description.IsEmpty();
                public bool HasSprite
                    => !Icon.IsEmpty();

                public readonly string Title;
                public readonly string Rarity;
                public readonly string Description;
                public readonly string Icon;

                public TooltipInfo(string title = default, string color = default, string description = default, string icon = default)
                {
                    Title = title;
                    Rarity = color;
                    Description = description;
                    Icon = icon;

                    if (!icon.IsEmpty())
                        icon.PreloadSprite();
                }
            }

            internal TooltipData TooltipInfos { get; } = new(itemInfo, attackInfo, primaryInfo, secondaryInfo, ultimateInfo, dashInfo);

            public readonly record struct TooltipData(ReadOnlyMemory<TooltipInfo> TooltipInfos)
            {
                public TooltipInfo this[int index]
                    => TooltipInfos.Span[index];

                public bool HasItemIcon
                    => TooltipInfos.Length > 0 && TooltipInfos.Span[0].HasSprite;
                public bool HasItemText
                    => TooltipInfos.Length > 0 && TooltipInfos.Span[0].HasText;
                public bool HasAbilityIcon
                {
                    get
                    {
                        var span = TooltipInfos.Span;

                        for (int i = 1; i < span.Length; i++)
                        {
                            if (span[i].HasSprite)
                                return true;
                        }

                        return false;
                    }
                }
                public bool HasAbilityText
                {
                    get
                    {
                        var span = TooltipInfos.Span;

                        for (int i = 1; i < span.Length; i++)
                        {
                            if (!span[i].Title.IsEmpty() || !span[i].Description.IsEmpty())
                                return true;
                        }

                        return false;
                    }
                }

                public ReadOnlySpan<TooltipInfo>.Enumerator GetEnumerator()
                    => TooltipInfos.Span.GetEnumerator();

                public TooltipData(TooltipInfo item, TooltipInfo attack, TooltipInfo primary, TooltipInfo secondary, TooltipInfo ultimate,
                    TooltipInfo dash) : this(new TooltipInfo[] { item, attack, primary, secondary, ultimate, dash }) { }
            }
        }
    }

    internal static IEnumerator GatherShadows()
    {
        while (!_managedAssets.Any())
        {
            yield return null;

            foreach (var item in PrimalItems)
            {
                PrimalItem primalItem = item;
                RefineMateria(ref primalItem);
                TravelPath(ref primalItem);
                ManageAssets(ref primalItem);
            }
        }
    }

    static void RefineMateria(ref PrimalItem primalItem)
    {
        Entity itemWeapon = primalItem.ItemWeapon.GetPrefabEntity();
        PowerInfo weaponPower = primalItem.WeaponShared.WeaponPower;

        itemWeapon.WithEdit(0, (ref ModifyUnitStatBuff_DOTS buff)
            => buff.Value = weaponPower.PhysicalPower);
        itemWeapon.WithInsert(1, new ModifyUnitStatBuff_DOTS()
        {
            StatType = UnitStatType.SpellPower,
            ModificationType = ModificationType.AddToBase,
            AttributeCapType = AttributeCapType.SoftCapped,
            Value = weaponPower.SpellPower,
            Modifier = 1
        });
    }

    static void TravelPath(ref PrimalItem primalItem)
    {
        PrimalClient weaponClient = primalItem.WeaponClient;
        if (weaponClient.WeaponModel.ScenePath.IsEmpty())
            return;

        GameObject gameObject = GameObjects.FindByTransformPath(weaponClient.WeaponModel.ScenePath);
        if (weaponClient.HasMesh)
        {
            Material material = gameObject?.GetComponent<MeshRenderer>()?.material;
            Mesh mesh = gameObject?.GetComponent<MeshFilter>()?.mesh;

            if (material != null && mesh != null)
                _managedAssets[primalItem.EquipBuff] = (material, mesh);
        }
        else
        {
            Material material = gameObject?.GetComponent<MeshRenderer>()?.material;

            if (material != null)
                _managedAssets[primalItem.EquipBuff] = (material, null);
        }
    }

    static void ManageAssets(ref PrimalItem primalItem)
    {
        PrimalClient weaponClient = primalItem.WeaponClient;
        ManagedItemData itemData = primalItem.ItemWeapon.GetExistingDataManaged<ManagedItemData>();

        bool hasText = weaponClient.HasItemText;
        bool hasIcon = weaponClient.HasItemIcon;

        if (hasText)
        {
            string name = weaponClient.ItemInfo.Title;
            string description = weaponClient.ItemInfo.Description;

            LocalizationKey nameKey = name?.LocalizeText() ?? default;
            LocalizationKey descKey = description?.LocalizeText() ?? default;

            if (!nameKey.IsEmpty)
                itemData?.Name = nameKey;

            if (!descKey.IsEmpty)
            {
                LocalizedStringBuilderBase localizedString = itemData.Description;
                localizedString.Key = descKey;
            }
        }
        if (hasIcon)
        {
            Sprite sprite = weaponClient.ItemInfo.Icon.GetExistingSprite();
            if (sprite.HasValue())
                itemData.Icon = sprite;
        }
    }

    internal static void OnLoad()
    {
        foreach (PrefabGUID buff in EquipBuffs)
        {
            if (Core.LocalCharacter.HasBuff(buff)
                && ShouldOverride(buff))
            {
                YieldChange(buff).Run();
            }
        }
    }

    internal static bool ShouldOverride(PrefabGUID buff)
        => IsPrimalBuff(buff) && HasPrimalItem(buff);

    internal static IEnumerator YieldChange(PrefabGUID buff)
    {
        yield return WaitForSeconds;

        if (!LocalCharacter.HasBuff(buff))
            yield break;

        Material material = _managedAssets[buff].Material;
        Mesh mesh = _managedAssets[buff].Mesh;

        if (mesh != null)
        {
            GameObject modelGeo = ResolveJoint01();
            MeshRenderer renderer = modelGeo?.GetComponent<MeshRenderer>();
            MeshFilter filter = modelGeo?.GetComponent<MeshFilter>();

            renderer?.material = material;
            renderer?.sharedMaterial = material;

            filter?.mesh = mesh;
            filter?.sharedMesh = mesh;
        }
        else
        {
            GameObject simpleGeo = ResolveJoint02();
            SkinnedMeshRenderer renderer = simpleGeo?.GetComponent<SkinnedMeshRenderer>();

            renderer?.material = material;
            renderer?.sharedMaterial = material;
        }
    }

    static GameObject ResolveJoint01()
    {
        GameObject modelGeo = GameObject.Find(WEAPON_2H_MALE)?.transform?.GetChild(0)?.GetChild(0)?.GetChild(0)?.GetChild(0)?.gameObject;
        modelGeo ??= GameObject.Find(WEAPON_2H_FEMALE)?.transform?.GetChild(0)?.GetChild(0)?.GetChild(0)?.GetChild(0)?.gameObject;

        return modelGeo;
    }

    static GameObject ResolveJoint02()
    {
        GameObject simpleGeo = GameObject.Find(WEAPON_1H_MALE)?.transform?.GetChild(0)?.GetChild(1)?.gameObject;
        simpleGeo ??= GameObject.Find(WEAPON_1H_FEMALE)?.transform?.GetChild(0)?.GetChild(1)?.gameObject;

        return simpleGeo;
    }

    static bool HasPrimalItem(PrefabGUID buff)
    {
        PrefabGUID itemWeapon = Core.LocalCharacter.GetEquipment().WeaponSlot.SlotId;
        return BuffWeaponLookup[buff].Equals(itemWeapon);
    }

    static bool IsPrimalBuff(PrefabGUID buff)
    {
        return EquipBuffs.Contains(buff);
    }

    public static readonly ConcurrentDictionary<AssetGuid, GameObject> ModelPrefabCache = [];

    internal static void LoadAssets()
    {
        try
        {
            var query = Core.EntityManager.BuildEntityQuery([new(Il2CppTypeOf<HybridModelUser>())], options: EntityQueryOptions.IncludeAll);
            var entities = query.ToEntityArray(Allocator.Temp);
            //var hybridModelUsers = query.ToComponentDataArray<HybridModelUser>(Allocator.Temp);

            try
            {
                for (int i = 0; i < entities.Length; i++)
                {
                    Entity entity = entities[i];
                    //HybridModelUser hybridModelUser = hybridModelUsers[i];

                    entity.DumpEntity();
                }
            }
            catch (Exception ex)
            {
                Log.LogError($"{ex}");
            }
            finally
            {
                query.Dispose();
                entities.Dispose();
            }
        }
        catch (Exception ex)
        {
            Log.LogError($"{ex}");
        }

        /*
        try
        {
            var mats = UnityEngine.Object.FindObjectsOfTypeIncludingAssets(Il2CppTypeOf<Material>());
            foreach (var mat in mats)
            {
                Log.LogWarning($"Material: {mat.name}");
            }

            var meshes = UnityEngine.Object.FindObjectsOfTypeIncludingAssets(Il2CppTypeOf<Mesh>());
            foreach (var mesh in meshes)
            {
                Log.LogWarning($"Mesh: {mesh.name}");
            }
        }
        catch (Exception ex)
        {
            Log.LogError($"{ex}");
        }
        */
    }

    internal static void UnloadAssets()
    {
        _managedAssets.Clear();
    }
}
