using Eclipse.Resources;
using Stunlock.Core;
using System.Collections;
using System.Collections.Concurrent;
using UnityEngine;
using static Eclipse.Utilities.ShadowMatter.PrimalItem;
using static Eclipse.Utilities.ShadowMatter.PrimalItems;

namespace Eclipse.Utilities;
internal static class ShadowMatter
{
    const string WEAPON_1H_FEMALE = "HybridModels/HYB_VampireFemale_Prefab(Clone)/VampireFemale_Standard_LOD00_rig/Root_JNT/Pelvis_JNT/Spine_JNT/Gut_JNT/Chest_JNT/Ribcage_JNT/Left_Collar_JNT/Left_Shoulder_JNT/Left_Elbow_JNT/Weapon02_JNT";
    const string WEAPON_2H_FEMALE = "HybridModels/HYB_VampireFemale_Prefab(Clone)/VampireFemale_Standard_LOD00_rig/Root_JNT/Pelvis_JNT/Spine_JNT/Gut_JNT/Chest_JNT/Ribcage_JNT/Right_Collar_JNT/Right_Shoulder_JNT/Right_Elbow_JNT/Weapon01_JNT";
    const string WEAPON_1H_MALE = "HybridModels/HYB_VampireMale_Prefab(Clone)/VampireMale_Standard_LOD00_rig/Root_JNT/Pelvis_JNT/Spine_JNT/Gut_JNT/Chest_JNT/Ribcage_JNT/Left_Collar_JNT/Left_Shoulder_JNT/Left_Elbow_JNT/Weapon02_JNT";
    const string WEAPON_2H_MALE = "HybridModels/HYB_VampireMale_Prefab(Clone)/VampireMale_Standard_LOD00_rig/Root_JNT/Pelvis_JNT/Spine_JNT/Gut_JNT/Chest_JNT/Ribcage_JNT/Right_Collar_JNT/Right_Shoulder_JNT/Right_Elbow_JNT/Weapon01_JNT";
    static WaitForSeconds WaitForSeconds { get; } = new(WAIT_FOR_SECONDS);
    const float WAIT_FOR_SECONDS = 0.5f; // 1.5f -> 0.75f -> *0.5f;
    static PrimalItems Items { get; } = new(_primalItemData);
    static HashSet<PrefabGUID> EquipBuffs { get; } = [.._primalItemData.Select(data => data.EquipBuff)];
    static Dictionary<PrefabGUID, PrefabGUID> BuffWeaponLookup { get; } = _primalItemData.ToDictionary(data => data.EquipBuff, data => data.ItemWeapon);
    static readonly ConcurrentDictionary<PrefabGUID, (Material Material, Mesh Mesh)> _managedAssets = []; // EquipBuff -> (Material, Mesh)
    internal readonly record struct PrimalItems(ReadOnlyMemory<PrimalItem> PrimalItemData)
    {
        internal static readonly PrimalItem[] _primalItemData =
        [
			new(PrefabGUIDs.Item_Weapon_Spear_T09_ShadowMatter,
                PrefabGUIDs.EquipBuff_Weapon_Pollaxe_Ability01,
                new("Part_Dracula_SwordFloor_PLACEHOLDER/DraculaSword_1H_Static")),
			new(PrefabGUIDs.Item_Weapon_GreatSword_T09_ShadowMatter,
                PrefabGUIDs.EquipBuff_Weapon_Pollaxe_Ability02,
                new("Model_Weapon_SanguineGreatsword_Legendary_Unholy_Prefab/SanguineGreatsword_2H_Standard/LOD00_GRP/SteelGreatsword_GEO", true)),
        ];
        public ReadOnlySpan<PrimalItem>.Enumerator GetEnumerator()
            => PrimalItemData.Span.GetEnumerator();
    }
    public readonly struct PrimalItem(PrefabGUID itemWeapon, PrefabGUID equipBuff, PrimalShared.ModelData itemModel)
    {
        public readonly PrefabGUID ItemWeapon = itemWeapon;
        public readonly PrefabGUID EquipBuff = equipBuff;

        public readonly PrimalShared WeaponShared = new(itemWeapon.GuidHash, equipBuff.GuidHash, itemModel);
        public readonly struct PrimalShared(int weaponGuid, int buffGuid, PrimalShared.ModelData modelData)
        {
            public readonly int WeaponGuid = weaponGuid;
            public readonly int BuffGuid = buffGuid;

            public readonly ModelData ItemModel = modelData;
            public readonly struct ModelData(string scenePath, bool hasMesh = false)
            {
                public readonly string ScenePath = scenePath;
                public readonly bool HasMesh = hasMesh;
            }
        }
    }
    internal static IEnumerator GatherShadows()
    {
        while (!_managedAssets.Any())
        {
            yield return null;

            foreach (PrimalItem primalItem in Items)
            {
                primalItem.TravelPath();
            }
        }
    }
    static void TravelPath(this PrimalItem primalItem)
    {
        GameObject gameObject = GameObjects.FindByTransformPath(primalItem.WeaponShared.ItemModel.ScenePath);
        bool hasMesh = primalItem.WeaponShared.ItemModel.HasMesh;

        if (hasMesh)
        {
            Material material = gameObject.GetComponent<MeshRenderer>()?.material;
            Mesh mesh = gameObject.GetComponent<MeshFilter>()?.mesh;

            if (material != null && mesh != null)
                _managedAssets[primalItem.EquipBuff] = (material, mesh);
        }
        else
        {
            Material material = gameObject.GetComponent<MeshRenderer>()?.material;

            if (material != null)
                _managedAssets[primalItem.EquipBuff] = (material, null);
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
    {
        return IsPrimalBuff(buff) && HasPrimalItem(buff);
    }
    internal static IEnumerator YieldChange(PrefabGUID buff)
    {
        yield return WaitForSeconds;

        if (!Core.LocalCharacter.HasBuff(buff))
        {
            yield break;
        }

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
    public static void UnloadAssets()
    {
        _managedAssets.Clear();
    }
}
