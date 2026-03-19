using Eclipse.Services;
using Il2CppInterop.Runtime;
using ProjectM;
using ProjectM.Gameplay.Systems;
using ProjectM.Network;
using ProjectM.Scripting;
using ProjectM.Shared;
using Stunlock.Core;
using Stunlock.Localization;
using System.Collections;
using System.Runtime.InteropServices;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using static Eclipse.Services.LocalizationService;

namespace Eclipse;
internal static class VExtensions
{
    static EntityManager EntityManager
        => Core.EntityManager;
    static ClientGameManager ClientGameManager
        => Core.ClientGameManager;
    static PrefabCollectionSystem PrefabCollectionSystem
        => Core.SystemService.PrefabCollectionSystem;

    const string EMPTY_KEY = "LocalizationKey.Empty";
    const string PREFIX = "Entity(";
    const int LENGTH = 7;

    public delegate void WithRefHandler<T>(ref T item);

    public static void With<T>(this Entity entity, WithRefHandler<T> action) where T : struct
    {
        if (!entity.Has<T>())
            return;

        T item = entity.Read<T>();
        action(ref item);

        EntityManager.SetComponentData(entity, item);
    }
    public static void WithEdit<T>(this Entity entity, int index, WithRefHandler<T> action) where T : struct
    {
        if (!entity.TryGetBuffer<T>(out var buffer))
        {
            Core.Log.LogWarning($"Entity is missing DynamicBuffer<{typeof(T)}>!");
            return;
        }

        if (!buffer.IsIndexWithinRange(index))
        {
            Core.Log.LogWarning($"Index ({index}) OoR ({index}/{buffer.Length}) for DynamicBuffer<{typeof(T)}>!");
            return;
        }

        var element = buffer[index];
        action(ref element);
        buffer[index] = element;
    }
    public static void WithInsert<T>(this Entity entity, int index, T element) where T : struct
    {
        if (!entity.TryGetBuffer<T>(out var buffer))
        {
            Core.Log.LogWarning($"Entity is missing DynamicBuffer<{typeof(T)}>!");
            return;
        }

        if (!buffer.IsIndexWithinRange(index))
        {
            Core.Log.LogWarning($"Index ({index}) OoR ({index}/{buffer.Length}) for DynamicBuffer<{typeof(T)}>!");
            return;
        }

        buffer.Insert(index, element);
    }
    public static void WithAdd<T>(this Entity entity, T element) where T : struct
    {
        if (!entity.TryGetBuffer<T>(out var buffer))
        {
            Core.Log.LogWarning($"Entity is missing DynamicBuffer<{typeof(T)}>!");
            return;
        }

        buffer.Add(element);
    }
    public static void WithClear<T>(this Entity entity) where T : struct
    {
        if (!entity.TryGetBuffer<T>(out var buffer))
        {
            Core.Log.LogWarning($"Entity is missing DynamicBuffer<{typeof(T)}>!");
            return;
        }

        buffer.Clear();
    }
    public static void AddWith<T>(this Entity entity, WithRefHandler<T> action) where T : struct
    {
        if (!entity.Has<T>())
        {
            entity.Add<T>();
        }

        entity.With(action);
    }
    public static void Write<T>(this Entity entity, T componentData) where T : struct
    {
        if (!entity.Has<T>())
            return;

        EntityManager.SetComponentData(entity, componentData);
    }
    public static T Read<T>(this Entity entity) where T : struct
    {
        return EntityManager.TryGetComponentData<T>(entity, out T componentData)
            ? componentData : default;
    }
    public static T Lookup<T>(this Entity entity, ref ComponentLookup<T> componentLookup)
    {
        return componentLookup.TryGetComponent(entity, out T componentData)
            ? componentData : default;
    }
    public static bool TryGetBuffer<T>(this Entity entity, out DynamicBuffer<T> dynamicBuffer) where T : struct
    {
        if (GameManager_Shared.TryGetBuffer(EntityManager, entity, out dynamicBuffer))
        {
            return true;
        }

        dynamicBuffer = default;
        return false;
    }
    public static DynamicBuffer<T> ReadBuffer<T>(this Entity entity) where T : struct
    {
        return EntityManager.GetBuffer<T>(entity);
    }
    public static DynamicBuffer<T> AddBuffer<T>(this Entity entity) where T : struct
    {
        return EntityManager.AddBuffer<T>(entity);
    }
    public static bool TryGetComponent<T>(this Entity entity, out T componentData) where T : struct
    {
        componentData = default;

        if (entity.Has<T>())
        {
            componentData = entity.Read<T>();

            return true;
        }

        return false;
    }
    public static bool TryGetComponentObject<T>(this Entity entity, EntityManager entityManager, out T componentObject) where T : class
    {
        componentObject = default;

        if (entityManager.HasComponent<T>(entity))
        {
            componentObject = entityManager.GetComponentObject<T>(entity);
            return componentObject != null;
        }

        return false;
    }
    public static bool Has<T>(this Entity entity)
    {
        return EntityManager.HasComponent<T>(entity);
    }
    public static bool HasBuffer<T>(this Entity entity)
    {
        return EntityManager.HasBuffer<T>(entity);
    }
    public static string GetPrefabName(this PrefabGUID prefabGUID)
    {
        return PrefabGuidsToNames.TryGetValue(prefabGUID, out string prefabName) ? $"{prefabName} {prefabGUID}" : "String.Empty";
    }
    public static string GetLocalizedName(this PrefabGUID prefabGUID)
    {
        string localizedName = GetNameFromGuidString(GetGuidString(prefabGUID));

        if (!string.IsNullOrEmpty(localizedName))
        {
            return localizedName;
        }

        return EMPTY_KEY;
    }
    public static void Add<T>(this Entity entity)
    {
        if (!entity.Has<T>()) EntityManager.AddComponent(entity, new(Il2CppType.Of<T>()));
    }
    public static void Remove<T>(this Entity entity)
    {
        if (entity.Has<T>()) EntityManager.RemoveComponent(entity, new(Il2CppType.Of<T>()));
    }
    public static bool TryGetFollowedPlayer(this Entity entity, out Entity player)
    {
        player = Entity.Null;

        if (entity.TryGetComponent(out Follower follower))
        {
            if (follower.Followed._Value.TryGetPlayer(out player))
            {
                return true;
            }
        }

        return false;
    }
    public static bool TryGetPlayer(this Entity entity, out Entity player)
    {
        player = Entity.Null;

        if (entity.Has<PlayerCharacter>())
        {
            player = entity;

            return true;
        }

        return false;
    }
    public static bool IsPlayer(this Entity entity)
    {
        if (entity.Has<VampireTag>())
        {
            return true;
        }

        return false;
    }
    public static bool IsDifferentPlayer(this Entity entity, Entity target)
    {
        if (entity.IsPlayer() && target.IsPlayer() && !entity.Equals(target))
        {
            return true;
        }

        return false;
    }
    public static bool IsFollowingPlayer(this Entity entity)
    {
        if (entity.TryGetComponent(out Follower follower))
        {
            if (follower.Followed._Value.IsPlayer())
            {
                return true;
            }
        }

        return false;
    }
    public static Entity GetBuffTarget(this Entity entity)
    {
        return CreateGameplayEventServerUtility.GetBuffTarget(EntityManager, entity);
    }
    public static Entity GetPrefabEntity(this Entity entity)
    {
        return GameManager_Shared.GetPrefabEntity(PrefabCollectionSystem._PrefabLookupMap, entity.GetPrefabGUID());
    }
    public static Entity GetPrefabEntity(this PrefabGUID prefabGuid)
    {
        return prefabGuid.HasValue() ? GameManager_Shared.GetPrefabEntity(PrefabCollectionSystem._PrefabLookupMap, prefabGuid) : Entity.Null;
    }
    public static Entity GetSpellTarget(this Entity entity)
    {
        return CreateGameplayEventServerUtility.GetSpellTarget(EntityManager, entity);
    }
    public static bool TryGetTeamEntity(this Entity entity, out Entity teamEntity)
    {
        teamEntity = Entity.Null;

        if (entity.TryGetComponent(out TeamReference teamReference))
        {
            Entity teamReferenceEntity = teamReference.Value._Value;

            if (teamReferenceEntity.Exists())
            {
                teamEntity = teamReferenceEntity;

                return true;
            }
        }

        return false;
    }
    public static bool Exists(this Entity entity)
    {
        return entity.HasValue() && entity.IndexWithinCapacity() && EntityManager.Exists(entity);
    }
    public static bool IndexWithinCapacity(this Entity entity)
    {
        string entityStr = entity.ToString();
        ReadOnlySpan<char> span = entityStr.AsSpan();

        if (!span.StartsWith(PREFIX)) return false;
        span = span[LENGTH..];

        int colon = span.IndexOf(':');
        if (colon <= 0) return false;

        ReadOnlySpan<char> tail = span[(colon + 1)..];

        int closeRel = tail.IndexOf(')');
        if (closeRel <= 0) return false;

        // Parse numbers
        if (!int.TryParse(span[..colon], out int index)) return false;
        if (!int.TryParse(tail[..closeRel], out _)) return false;

        // Single unsigned capacity check
        int capacity = EntityManager.EntityCapacity;
        bool isValid = (uint)index < (uint)capacity;

        if (!isValid)
        {
            // Core.Log.LogWarning($"Entity index out of range! ({index}>{capacity})");
        }

        return isValid;
    }
    public static bool IsDisabled(this Entity entity)
    {
        return entity.Has<Disabled>();
    }
    public static bool HasConnectedCoffin(this Entity entity)
    {
        return entity.TryGetComponent(out ServantConnectedCoffin servantConnectedCoffin) && servantConnectedCoffin.CoffinEntity.GetEntityOnServer().Exists();
    }
    public static bool IsVBlood(this Entity entity)
    {
        return entity.Has<VBloodUnit>();
    }
    public static ulong GetSteamId(this Entity entity)
    {
        if (entity.TryGetComponent(out PlayerCharacter playerCharacter))
        {
            return playerCharacter.UserEntity.Read<User>().PlatformId;
        }
        else if (entity.TryGetComponent(out User user))
        {
            return user.PlatformId;
        }

        return 0;
    }
    public static NetworkId GetNetworkId(this Entity entity)
    {
        if (entity.TryGetComponent(out NetworkId networkId))
            return networkId;

        return NetworkId.Empty;
    }
    public static void ForEach<T>(this IEnumerable<T> source, Action<T> action)
    {
        foreach (var item in source)
        {
            action(item);
        }
    }
    public static PrefabGUID GetPrefabGUID(this Entity entity)
    {
        if (entity.TryGetComponent(out PrefabGUID prefabGUID))
            return prefabGUID;

        return PrefabGUID.Empty;
    }
    public static Entity GetUserEntity(this Entity character)
    {
        if (character.TryGetComponent(out PlayerCharacter playerCharacter))
            return playerCharacter.UserEntity;

        return Entity.Null;
    }
    public static User GetUser(this Entity entity)
    {
        if (entity.TryGetComponent(out PlayerCharacter playerCharacter) && playerCharacter.UserEntity.TryGetComponent(out User user)) return user;
        else if (entity.TryGetComponent(out user)) return user;

        return User.Empty;
    }
    public static Equipment GetEquipment(this Entity entity)
    {
        if (entity.TryGetComponent(out Equipment equipment))
            return equipment;

        return default;
    }
    public static CustomizationFeatures GetCustomizationFeatures(this Entity entity)
    {
        if (entity.TryGetComponent(out CustomizationFeatures customizationFeatures))
            return customizationFeatures;

        return default;
    }
    public static T GetExistingDataManaged<T>(this PrefabGUID prefabGuid) where T : class
    {
        if (Core.SystemService.ManagedDataSystem.ManagedDataRegistry.TryGet(prefabGuid, out T managedData))
        {
            return managedData;
        }

        return default;
    }
    public static bool HasBuff(this Entity entity, PrefabGUID buffPrefabGUID)
    {
        return GameManager_Shared.HasBuff(EntityManager, entity, buffPrefabGUID.ToIdentifier());
    }
    public static bool TryGetBuff(this Entity entity, PrefabGUID buffPrefabGUID, out Entity buffEntity)
    {
        return GameManager_Shared.TryGetBuff(EntityManager, entity, buffPrefabGUID.ToIdentifier(), out buffEntity);
    }
    public static float3 GetAimPosition(this Entity entity)
    {
        if (entity.TryGetComponent(out EntityInput entityInput))
        {
            return entityInput.AimPosition;
        }

        return float3.zero;
    }
    public static bool TryGetPosition(this Entity entity, out float3 position)
    {
        position = float3.zero;

        if (entity.TryGetComponent(out Translation translation))
        {
            position = translation.Value;

            return true;
        }

        return false;
    }
    public static float3 GetPosition(this Entity entity)
    {
        if (entity.TryGetComponent(out Translation translation))
        {
            return translation.Value;
        }

        return float3.zero;
    }
    public static bool TryGetMatch(this HashSet<(ulong, ulong)> hashSet, ulong value, out (ulong, ulong) matchingPair)
    {
        matchingPair = default;

        foreach (var pair in hashSet)
        {
            if (pair.Item1 == value || pair.Item2 == value)
            {
                matchingPair = pair;

                return true;
            }
        }

        return false;
    }
    public static bool IsCustomSpawned(this Entity entity)
    {
        return entity.TryGetComponent(out IsMinion isMinion) && isMinion.Value;
    }
    public static void Destroy(this Entity entity)
    {
        if (entity.Exists()) DestroyUtility.Destroy(EntityManager, entity);
    }
    public static void SetTeam(this Entity entity, Entity teamSource)
    {
        if (entity.Has<Team>() && entity.Has<TeamReference>() && teamSource.TryGetComponent(out Team sourceTeam) && teamSource.TryGetComponent(out TeamReference sourceTeamReference))
        {
            Entity teamRefEntity = sourceTeamReference.Value._Value;
            int teamId = sourceTeam.Value;

            entity.With((ref TeamReference teamReference) => teamReference.Value._Value = teamRefEntity);
            entity.With((ref Team team) => team.Value = teamId);
        }
    }
    public static void SetFaction(this Entity entity, PrefabGUID factionPrefabGUID)
    {
        if (entity.Has<FactionReference>())
        {
            entity.With((ref FactionReference factionReference) => factionReference.FactionGuid._Value = factionPrefabGUID);
        }
    }
    public static bool HasValue(this Entity entity)
    {
        return entity != Entity.Null;
    }
    public static bool IsAllies(this Entity entity, Entity player)
    {
        return ClientGameManager.IsAllies(entity, player);
    }
    public static Coroutine Run(this IEnumerator routine)
    {
        return Core.StartCoroutine(routine);
    }
    public static void Stop(this Coroutine routine)
    {
        if (routine != null)
            Core.StopCoroutine(routine);
    }
    public static Dictionary<TValue, TKey> Reverse<TKey, TValue>(this IDictionary<TKey, TValue> source)
    {
        var reversed = new Dictionary<TValue, TKey>();

        foreach (var kvp in source)
        {
            reversed[kvp.Value] = kvp.Key;
        }

        return reversed;
    }
    public static bool Equals<T>(this T value, params T[] options)
    {
        foreach (var option in options)
        {
            if (value.Equals(option)) return true;
        }

        return false;
    }
    public static LocalizationKey LocalizeText(this string text)
        => Core.LocalizeString(text);
    public static void PreloadSprite(this string iconName)
        => CanvasService.DataHUD.SpriteNames.Add(iconName);
    public static Sprite GetExistingSprite(this string iconName)
    {
        return CanvasService.DataHUD.Sprites.TryGetValue(iconName, out Sprite sprite) ? sprite : default;
    }
    public static bool IsEmpty(this string str)
    {
        return string.IsNullOrEmpty(str);
    }
    public static bool HasValue<T>(this T obj) where T : class
        => obj != null;
    public static bool HasValue(this UnityEngine.Object obj)
        => obj;

    // public static unsafe
}