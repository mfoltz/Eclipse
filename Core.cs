using BepInEx.Logging;
using BepInEx.Unity.IL2CPP.Utils.Collections;
using Eclipse.Resources;
using Eclipse.Services;
using ProjectM;
using ProjectM.Physics;
using ProjectM.Scripting;
using ProjectM.UI;
using Stunlock.Core;
using Stunlock.Localization;
using System.Collections;
using System.Security.Cryptography;
using System.Text;
using Unity.Entities;
using UnityEngine;

namespace Eclipse;
internal static class Core
{
    static World _client;
    static SystemService _systemService;

    static Entity _localCharacter = Entity.Null;
    static Entity _localUser = Entity.Null;
    public static Entity LocalCharacter =>
        _localCharacter.Exists()
        ? _localCharacter
        : (ConsoleShared.TryGetLocalCharacterInCurrentWorld(out _localCharacter, _client)
        ? _localCharacter
        : Entity.Null);
    public static Entity LocalUser =>
        _localUser.Exists()
        ? _localUser
        : (ConsoleShared.TryGetLocalUserInCurrentWorld(out _localUser, _client)
        ? _localUser
        : Entity.Null);
    public static EntityManager EntityManager => _client.EntityManager;
    public static SystemService SystemService => _systemService ??= new(_client);
    public static ClientGameManager ClientGameManager => SystemService.ClientScriptMapper._ClientGameManager;
    public static CanvasService CanvasService { get; set; }
    public static ServerTime ServerTime => ClientGameManager.ServerTime;
    public static ManualLogSource Log => Plugin.LogInstance;

    static MonoBehaviour _monoBehaviour;
    public static byte[] NEW_SHARED_KEY { get; set; }

    public static bool _initialized = false;
    public static void Initialize(GameDataManager __instance)
    {
        if (_initialized) return;

        _client = __instance.World;

        _ = new LocalizationService();

        NEW_SHARED_KEY = Convert.FromBase64String(SecretManager.GetNewSharedKey());

        if (SystemService.PrefabCollectionSystem._PrefabGuidToEntityMap.TryGetValue(PrefabGUIDs.SetBonus_AllLeech_T09, out Entity prefabEntity)
            && prefabEntity.TryGetBuffer<ModifyUnitStatBuff_DOTS>(out var buffer))
        {
            buffer.Clear();
        }

        _initialized = true;
    }
    public static void Reset()
    {
        _client = null;
        _systemService = null;
        CanvasService = null;
        _initialized = false;
        _localCharacter = Entity.Null;
        _localUser = Entity.Null;
    }
    public static void SetCanvas(UICanvasBase canvas)
    {
        CanvasService = new(canvas);
    }
    public static Coroutine StartCoroutine(IEnumerator routine)
    {
        if (_monoBehaviour == null)
        {
            var go = new GameObject(MyPluginInfo.PLUGIN_NAME);
            _monoBehaviour = go.AddComponent<IgnorePhysicsDebugSystem>();
            UnityEngine.Object.DontDestroyOnLoad(go);
        }

        return _monoBehaviour.StartCoroutine(routine.WrapToIl2Cpp());
    }
    public static void StopCoroutine(Coroutine routine)
    {
        if (_monoBehaviour == null) return;
        _monoBehaviour.StopCoroutine(routine);
    }
    public static void LogEntity(World world, Entity entity)
    {
        Il2CppSystem.Text.StringBuilder sb = new();

        try
        {
            EntityDebuggingUtility.DumpEntity(world, entity, true, sb);
            Log.LogInfo($"Entity Dump:\n{sb.ToString()}");
        }
        catch (Exception e)
        {
            Log.LogWarning($"Error dumping entity: {e.Message}");
        }
    }
    static AssetGuid GetAssetGuid(string textString)
    {
        using SHA256 sha256 = SHA256.Create();
        byte[] hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(textString));

        Il2CppSystem.Guid uniqueGuid = new(hashBytes[..16]);
        return AssetGuid.FromGuid(uniqueGuid);
    }
    public static LocalizationKey LocalizeString(string text)
    {
        AssetGuid assetGuid = GetAssetGuid(text);

        if (Localization.Initialized)
        {
            Localization._LocalizedStrings.TryAdd(assetGuid, text);
            return new(assetGuid);
        }
        else
        {
            Log.LogWarning("Stunlock.Localization not initialized yet!");
        }

        return LocalizationKey.Empty;
    }

    /*
    static void LogSystems()
    {
    foreach (var kvp in Client.m_SystemLookup)
    {
    Il2CppSystem.Type systemType = kvp.Key;
    ComponentSystemBase systemBase = kvp.Value;
    if (systemBase.EntityQueries.Length == 0) continue;

    Core.Log.LogInfo("=============================");
    Core.Log.LogInfo(systemType.FullName);
    foreach (EntityQuery query in systemBase.EntityQueries)
    {
    EntityQueryDesc entityQueryDesc = query.GetEntityQueryDesc();
    Core.Log.LogInfo($" All: {string.Join(",", entityQueryDesc.All)}");
    Core.Log.LogInfo($" Any: {string.Join(",", entityQueryDesc.Any)}");
    Core.Log.LogInfo($" Absent: {string.Join(",", entityQueryDesc.Absent)}");
    Core.Log.LogInfo($" None: {string.Join(",", entityQueryDesc.None)}");
    }
    Core.Log.LogInfo("=============================");
    }
    }
    */

    /*
    static readonly PrefabGUID _copperWires = new(-456161884);
    static readonly PrefabGUID _primalEssence = new(1566989408);
    static readonly PrefabGUID _extractShardRecipe = new(1743327679);
    static readonly PrefabGUID _itemBuildingEMP = new(-1447213995);
    static readonly PrefabGUID _depletedBattery = new(1270271716);
    static readonly PrefabGUID _chargedBatteryRecipe = new(-40415372);
    static readonly PrefabGUID _batteryCharge = new(-77555820);
    static readonly PrefabGUID _itemJewelTemplate = new(1075994038);
    static readonly PrefabGUID _lesserStygian = new(2103989354);
    static readonly PrefabGUID _bloodEssence = new(862477668);
    static readonly PrefabGUID _batHide = new(1262845777);
    static readonly PrefabGUID _techScrap = new(834864259);
    static readonly PrefabGUID _copperWiresRecipe = new(-2031309726);
    static void ModifyPrefabs()
    {
        if (PrefabCollectionSystem._PrefabGuidToEntityMap.TryGetValue(_itemBuildingEMP, out Entity prefabEntity))
        {
            if (!prefabEntity.Has<Salvageable>())
            {
                prefabEntity.AddWith((ref Salvageable salvageable) =>
                {
                    salvageable.RecipeGUID = PrefabGUID.Empty;
                    salvageable.SalvageFactor = 1f;
                    salvageable.SalvageTimer = 60f;
                });

                var recipeRequirementBuffer = EntityManager.AddBuffer<RecipeRequirementBuffer>(prefabEntity);
                recipeRequirementBuffer.Add(new RecipeRequirementBuffer { Guid = _depletedBattery, Amount = 5 });
                recipeRequirementBuffer.Add(new RecipeRequirementBuffer { Guid = _techScrap, Amount = 25 });
            }
        }

        if (PrefabCollectionSystem._PrefabGuidToEntityMap.TryGetValue(_extractShardRecipe, out prefabEntity))
        {
            if (prefabEntity.Has<RecipeOutputBuffer>())
            {
                var recipeOutputBuffer = prefabEntity.ReadBuffer<RecipeOutputBuffer>();
                recipeOutputBuffer.Add(new RecipeOutputBuffer { Guid = _itemJewelTemplate, Amount = 1 });
            }
        }

        if (PrefabCollectionSystem._PrefabGuidToEntityMap.TryGetValue(_chargedBatteryRecipe, out prefabEntity))
        {
            if (prefabEntity.Has<RecipeRequirementBuffer>())
            {
                var recipeRequirementBuffer = prefabEntity.ReadBuffer<RecipeRequirementBuffer>();
                recipeRequirementBuffer.Add(new RecipeRequirementBuffer { Guid = _batteryCharge, Amount = 1 });
            }
        }

        if (PrefabCollectionSystem._PrefabGuidToEntityMap.TryGetValue(_copperWires, out prefabEntity))
        {
            if (!prefabEntity.Has<Salvageable>())
            {
                prefabEntity.AddWith((ref Salvageable salvageable) =>
                {
                    salvageable.RecipeGUID = PrefabGUID.Empty;
                    salvageable.SalvageFactor = 1f;
                    salvageable.SalvageTimer = 15f;
                });

                var recipeRequirementBuffer = prefabEntity.AddBuffer<RecipeRequirementBuffer>();
                recipeRequirementBuffer.Add(new RecipeRequirementBuffer { Guid = _batteryCharge, Amount = 1 });
            }
        }

        if (PrefabCollectionSystem._PrefabGuidToEntityMap.TryGetValue(_primalEssence, out prefabEntity))
        {
            if (!prefabEntity.Has<Salvageable>())
            {
                prefabEntity.AddWith((ref Salvageable salvageable) =>
                {
                    salvageable.RecipeGUID = PrefabGUID.Empty;
                    salvageable.SalvageFactor = 1f;
                    salvageable.SalvageTimer = 5f;
                });

                var recipeRequirementBuffer = prefabEntity.AddBuffer<RecipeRequirementBuffer>();
                recipeRequirementBuffer.Add(new RecipeRequirementBuffer { Guid = _batteryCharge, Amount = 5 });
            }
        }

        if (PrefabCollectionSystem._PrefabGuidToEntityMap.TryGetValue(_batHide, out prefabEntity))
        {
            if (!prefabEntity.Has<Salvageable>())
            {
                prefabEntity.Add<Salvageable>();
            }

            prefabEntity.With((ref Salvageable salvageable) =>
            {
                salvageable.RecipeGUID = PrefabGUID.Empty;
                salvageable.SalvageFactor = 1f;
                salvageable.SalvageTimer = 15f;
            });

            if (!prefabEntity.Has<RecipeRequirementBuffer>())
            {
                prefabEntity.AddBuffer<RecipeRequirementBuffer>();
            }

            var recipeRequirementBuffer = prefabEntity.ReadBuffer<RecipeRequirementBuffer>();
            recipeRequirementBuffer.Add(new RecipeRequirementBuffer { Guid = _lesserStygian, Amount = 3 });
            recipeRequirementBuffer.Add(new RecipeRequirementBuffer { Guid = _bloodEssence, Amount = 5 });
        }     
    }
    */
}