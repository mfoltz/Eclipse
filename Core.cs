using BepInEx.Logging;
using BepInEx.Unity.IL2CPP.Utils.Collections;
using Eclipse.Resources;
using Eclipse.Services;
using Il2CppInterop.Runtime;
using ProjectM;
using ProjectM.Hybrid;
using ProjectM.Network;
using ProjectM.Physics;
using ProjectM.Scripting;
using ProjectM.UI;
using Stunlock.Core;
using Stunlock.Localization;
using System.Collections;
using System.Security.Cryptography;
using System.Text;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;
using static Eclipse.Utilities.ShadowMatter;

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
    public static bool HasInitialized => _initialized;
    public static bool _initialized;
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

        GatherShadows().Run();
        _initialized = true;
    }
    public static void Reset()
    {
        UnloadAssets();

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
        if (_monoBehaviour == null)
            return;

        _monoBehaviour.StopCoroutine(routine);
    }
    public static void DumpEntity(this Entity entity)
    {
        Il2CppSystem.Text.StringBuilder sb = new();

        try
        {
            EntityDebuggingUtility.DumpEntity(_client, entity, true, sb);
            Log.LogInfo($"Entity Dump:\n{sb.ToString()}");
        }
        catch (Exception ex)
        {
            Log.LogWarning($"{ex}");
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
            if (!Localization._LocalizedStrings.TryAdd(assetGuid, text))
                Localization._LocalizedStrings[assetGuid] = text;

            return new(assetGuid);
        }
        else
        {
            Log.LogWarning("Stunlock.Localization isn't ready yet!");
        }

        return LocalizationKey.Empty;
    }
    public static EntityQuery BuildEntityQuery(
        this EntityManager entityManager,
        ComponentType[] all,
        EntityQueryOptions options)
    {
        var builder = new EntityQueryBuilder(Allocator.Temp);

        foreach (var componentType in all)
            builder.AddAll(componentType);

        builder.WithOptions(options);

        return entityManager.CreateEntityQuery(ref builder);
    }
    public static Il2CppSystem.Type Il2CppTypeOf<T>()
        => Il2CppType.Of<T>();
}