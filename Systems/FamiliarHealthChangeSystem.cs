using Il2CppInterop.Runtime;
using ProjectM;
using Stunlock.Core;
using Unity.Collections;
using Unity.Entities;

namespace Eclipse.Systems;

public sealed class FamiliarHealthChangeSystem : SystemBase
{
    internal static FamiliarHealthChangeSystem Instance { get; set; }

    EntityQuery _familiarQuery;
    EntityTypeHandle _entityHandle;

    ComponentTypeHandle<PrefabGUID> _prefabGuidHandle;
    ComponentTypeHandle<Health> _healthHandle;
    public override void OnCreate()
    {
        Instance = this;

        _familiarQuery = GetEntityQuery(new EntityQueryDesc
        {
            All = new[]
            {
                ComponentType.ReadOnly(Il2CppType.Of<PrefabGUID>()),
                ComponentType.ReadOnly(Il2CppType.Of<Health>()),
                ComponentType.ReadOnly(Il2CppType.Of<Follower>()),
                ComponentType.ReadOnly(Il2CppType.Of<Movement>())
            }
        });

        _familiarQuery.SetChangedVersionFilter(ComponentType.ReadOnly(Il2CppType.Of<Health>()));
        _entityHandle = GetEntityTypeHandle();

        _prefabGuidHandle = GetComponentTypeHandle<PrefabGUID>(true);
        _healthHandle = GetComponentTypeHandle<Health>(true);

        RequireForUpdate(_familiarQuery);
        Enabled = true;
    }
    public override void OnUpdate()
    {
        _entityHandle.Update(this);
        _prefabGuidHandle.Update(this);
        _healthHandle.Update(this);

        var chunks = _familiarQuery.ToArchetypeChunkArray(Allocator.Temp);
        try
        {
            foreach (var chunk in chunks)
            {
                var entities = chunk.GetNativeArray(_entityHandle);
                var prefabGuids = chunk.GetNativeArray(_prefabGuidHandle);
                var healths = chunk.GetNativeArray(_healthHandle);

                for (int i = 0; i < chunk.Count; ++i)
                {
                    Entity entity = entities[i];
                    PrefabGUID prefabGuid = prefabGuids[i];
                    Health health = healths[i];

                    if (Exists(entity))
                    {
                        //
                    }
                }
            }
        }
        finally
        {
            chunks.Dispose();
        }
    }
}