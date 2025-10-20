using Eclipse.Resources;
using Eclipse.Services;
using ProjectM;
using ProjectM.Shared;
using Stunlock.Core;
using Stunlock.Localization;
using System.Security.Cryptography;
using System.Text;
using Unity.Entities;
using UnityEngine;

namespace Eclipse;
internal static class Recipes
{
    static EntityManager EntityManager => Core.EntityManager;
    static SystemService SystemService => Core.SystemService;
    static PrefabCollectionSystem PrefabCollectionSystem => SystemService.PrefabCollectionSystem;
    static GameDataSystem GameDataSystem => SystemService.GameDataSystem;

    static readonly PrefabGUID _advancedGrinder = new(-178579946); // vampiric dust
    static readonly PrefabGUID _advancedFurnace = new(-222851985); // silver ingot
    static readonly PrefabGUID _fabricator = new(-465055967);      // copper wires, iron body
    static readonly PrefabGUID _shardExtractor = new(1794206684);  // shards, probably :p
    static readonly PrefabGUID _gemCuttingTable = new(-21483617);  // extractor hates being alive so using this instead

    static readonly PrefabGUID _refinementInventoryLarge = new(1436956144);
    static readonly PrefabGUID _extractorInventory = new(-1814907421);

    static readonly PrefabGUID _ironBodyRecipe = new(-1270503528);
    static readonly PrefabGUID _vampiricDustRecipe = new(311920560);
    static readonly PrefabGUID _copperWiresRecipe = new(-2031309726);
    static readonly PrefabGUID _silverIngotRecipe = new(-1633898285);
    static readonly PrefabGUID _fakeFlowerRecipe = new(-2095604835);
    static readonly PrefabGUID _chargedBatteryRecipe = new(-40415372);

    static readonly PrefabGUID _batHide = new(1262845777);
    static readonly PrefabGUID _lesserStygian = new(2103989354);
    static readonly PrefabGUID _bloodEssence = new(862477668);
    static readonly PrefabGUID _plantThistle = new(-598100816);
    static readonly PrefabGUID _batteryCharge = new(-77555820);
    static readonly PrefabGUID _techScrap = new(834864259);
    static readonly PrefabGUID _primalEssence = new(1566989408);
    static readonly PrefabGUID _copperWires = new(-456161884);
    static readonly PrefabGUID _itemBuildingEMP = new(-1447213995);
    static readonly PrefabGUID _depletedBattery = new(1270271716);
    static readonly PrefabGUID _itemJewelTemplate = new(1075994038);

    static readonly PrefabGUID _pristineHeart = new(-1413694594);
    static readonly PrefabGUID _radiantFibre = new(-182923609);
    static readonly PrefabGUID _resonator = new(-1629804427);
    static readonly PrefabGUID _document = new(1334469825);
    static readonly PrefabGUID _demonFragment = new(-77477508);
    static readonly PrefabGUID _magicalComponent = new(1488205677);
    static readonly PrefabGUID _tailoringComponent = new(828271620);
    static readonly PrefabGUID _gemGrindStone = new(2115367516);

    static readonly PrefabGUID _perfectAmethyst = new(-106283194);
    static readonly PrefabGUID _perfectEmerald = new(1354115931);
    static readonly PrefabGUID _perfectRuby = new(188653143);
    static readonly PrefabGUID _perfectSapphire = new(-2020212226);
    static readonly PrefabGUID _perfectTopaz = new(-1983566585);
    static readonly PrefabGUID _perfectMiststone = new(750542699);

    static readonly PrefabGUID _goldOre = new(660533034);
    static readonly PrefabGUID _goldJewelry = new(-1749304196);

    static readonly PrefabGUID _extractShardRecipe = new(1743327679);
    static readonly PrefabGUID _solarusShardRecipe = new(-958598508);
    static readonly PrefabGUID _monsterShardRecipe = new(1791150988);
    static readonly PrefabGUID _manticoreShardRecipe = new(-111826090);
    static readonly PrefabGUID _draculaShardRecipe = new(-414358988);
    static readonly PrefabGUID _itemBuildingManticore = new(-222860772);

    static readonly PrefabGUID _solarusShard = new(-21943750);
    static readonly PrefabGUID _monsterShard = new(-1581189572);
    static readonly PrefabGUID _manticoreShard = new(-1260254082);
    static readonly PrefabGUID _draculaShard = new(666638454);

    static readonly PrefabGUID _solarusShardContainer = new(-824445631);
    static readonly PrefabGUID _monsterShardContainer = new(-1996942061);
    static readonly PrefabGUID _manticoreShardContainer = new(653759442);
    static readonly PrefabGUID _draculaShardContainer = new(1495743889);

    static readonly PrefabGUID _primalStygianRecipe = new(-259193408);
    static readonly PrefabGUID _greaterStygian = new(576389135); // salvage for lesser stygian shards
    static readonly PrefabGUID _primalStygian = new(28358550);

    static readonly PrefabGUID _bloodCrystalRecipe = new(-597461125);  // using perfect topaz gemdust recipe for this
    static readonly PrefabGUID _crystal = new(-257494203);
    static readonly PrefabGUID _bloodCrystal = new(-1913156733);
    static readonly PrefabGUID _greaterEssence = new(271594022); // salvage for normal blood essence

    static readonly List<PrefabGUID> _shardRecipes =
    [
        _solarusShardRecipe,
        _monsterShardRecipe,
        _manticoreShardRecipe,
        _draculaShardRecipe
    ];

    static readonly List<PrefabGUID> _soulShards =
    [
        _solarusShard,
        _monsterShard,
        _manticoreShard,
        _draculaShard
    ];

    static readonly List<PrefabGUID> _shardContainers =
    [
        _solarusShardContainer,
        _monsterShardContainer,
        _manticoreShardContainer,
        _draculaShardContainer
    ];

    static readonly Dictionary<PrefabGUID, PrefabGUID> _recipesToShards = new()
    {
        { _solarusShardRecipe, _solarusShard },
        { _monsterShardRecipe, _monsterShard },
        { _manticoreShardRecipe, _manticoreShard },
        { _draculaShardRecipe, _draculaShard }
    };

    const string PRIMAL_JEWEL = "Stunlock_Icon_Item_Jewel_Collection4";
    static AssetGuid HashStringToGuidString(string hashString)
    {
        using SHA256 sha256 = SHA256.Create();
        byte[] hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(hashString));

        Il2CppSystem.Guid uniqueGuid = new(hashBytes[..16]);
        return AssetGuid.FromGuid(uniqueGuid);
    }
    public static void ModifyRecipes()
    {
        var recipeMap = GameDataSystem.RecipeHashLookupMap;

        Entity itemEntity = PrefabCollectionSystem._PrefabGuidToEntityMap[_itemBuildingEMP];

        var recipeRequirementBuffer = EntityManager.AddBuffer<RecipeRequirementBuffer>(itemEntity);
        recipeRequirementBuffer.Add(new RecipeRequirementBuffer { Guid = _depletedBattery, Amount = 2 });
        recipeRequirementBuffer.Add(new RecipeRequirementBuffer { Guid = _techScrap, Amount = 15 });

        if (!itemEntity.Has<Salvageable>())
        {
            itemEntity.AddWith((ref Salvageable salvageable) =>
            {
                salvageable.RecipeGUID = PrefabGUID.Empty;
                salvageable.SalvageFactor = 1f;
                salvageable.SalvageTimer = 20f;
            });
        }

        itemEntity = PrefabCollectionSystem._PrefabGuidToEntityMap[_greaterStygian];

        if (!itemEntity.Has<Salvageable>())
        {
            itemEntity.AddWith((ref Salvageable salvageable) =>
            {
                salvageable.RecipeGUID = PrefabGUIDs.Recipe_CastleUpkeep_T02;
                salvageable.SalvageFactor = 1f;
                salvageable.SalvageTimer = 10f;
            });
        }

        itemEntity = PrefabCollectionSystem._PrefabGuidToEntityMap[_greaterEssence];

        if (!itemEntity.Has<Salvageable>())
        {
            itemEntity.AddWith((ref Salvageable salvageable) =>
            {
                salvageable.RecipeGUID = PrefabGUIDs.Recipe_CastleUpkeep_T02;
                salvageable.SalvageFactor = 1f;
                salvageable.SalvageTimer = 5f;
            });
        }

        Entity recipeEntity = PrefabCollectionSystem._PrefabGuidToEntityMap[_primalStygianRecipe];
        recipeRequirementBuffer = recipeEntity.ReadBuffer<RecipeRequirementBuffer>();

        RecipeRequirementBuffer recipeRequirement = recipeRequirementBuffer[0];
        recipeRequirement.Guid = _greaterStygian;
        recipeRequirement.Amount = 8;

        recipeRequirementBuffer[0] = recipeRequirement;

        var recipeOutputBuffer = recipeEntity.ReadBuffer<RecipeOutputBuffer>();

        RecipeOutputBuffer recipeOutput = recipeOutputBuffer[0];
        recipeOutput.Guid = _primalStygian;
        recipeOutput.Amount = 1;

        recipeOutputBuffer[0] = recipeOutput;

        recipeEntity.With((ref RecipeData recipeData) =>
        {
            recipeData.CraftDuration = 10f;
            recipeData.AlwaysUnlocked = true;
            recipeData.HideInStation = false;
            recipeData.HudSortingOrder = 0;
        });

        recipeMap[_primalStygianRecipe] = recipeEntity.Read<RecipeData>();

        recipeEntity = PrefabCollectionSystem._PrefabGuidToEntityMap[_bloodCrystalRecipe];

        recipeRequirementBuffer = recipeEntity.ReadBuffer<RecipeRequirementBuffer>();

        recipeRequirement = recipeRequirementBuffer[0];
        recipeRequirement.Guid = _crystal;
        recipeRequirement.Amount = 100;

        recipeRequirementBuffer[0] = recipeRequirement;
        recipeRequirement.Guid = _greaterEssence;
        recipeRequirement.Amount = 1;
        recipeRequirementBuffer.Add(recipeRequirement);

        recipeOutputBuffer = recipeEntity.ReadBuffer<RecipeOutputBuffer>();

        recipeOutput = recipeOutputBuffer[0];
        recipeOutput.Guid = _bloodCrystal;
        recipeOutput.Amount = 100;

        recipeOutputBuffer[0] = recipeOutput;

        recipeEntity.With((ref RecipeData recipeData) =>
        {
            recipeData.CraftDuration = 10f;
            recipeData.AlwaysUnlocked = true;
            recipeData.HideInStation = false;
            recipeData.HudSortingOrder = 0;
        });

        recipeMap[_bloodCrystalRecipe] = recipeEntity.Read<RecipeData>();

        if (CanvasService.DataHUD.Sprites.TryGetValue(PRIMAL_JEWEL, out Sprite jewelSprite))
        {
            ManagedItemData managedItemData = Core.SystemService.ManagedDataSystem.ManagedDataRegistry.GetOrDefault<ManagedItemData>(_itemJewelTemplate);

            if (managedItemData != null && jewelSprite != null)
            {
                managedItemData.Icon = jewelSprite;

                AssetGuid nameGuid = HashStringToGuidString($"{managedItemData.Name.Key}{MyPluginInfo.PLUGIN_NAME}");
                string outputName = "Primal Jewel";

                Stunlock.Localization.Localization._LocalizedStrings.TryAdd(nameGuid, outputName);

                LocalizationKey nameKey = new(nameGuid);
                managedItemData.Name = nameKey;
            }
        }

        if (PrefabCollectionSystem._PrefabGuidToEntityMap.TryGetValue(_primalEssence, out Entity prefabEntity))
        {
            if (!prefabEntity.Has<Salvageable>())
            {
                prefabEntity.Add<Salvageable>();
            }

            prefabEntity.With((ref Salvageable salvageable) =>
            {
                salvageable.RecipeGUID = PrefabGUID.Empty;
                salvageable.SalvageFactor = 1f;
                salvageable.SalvageTimer = 10f;
            });

            if (!prefabEntity.Has<RecipeRequirementBuffer>())
            {
                prefabEntity.AddBuffer<RecipeRequirementBuffer>();
            }

            recipeRequirementBuffer = prefabEntity.ReadBuffer<RecipeRequirementBuffer>();
            recipeRequirementBuffer.Add(new RecipeRequirementBuffer { Guid = _batteryCharge, Amount = 5 });
        }

        if (PrefabCollectionSystem._PrefabGuidToEntityMap.TryGetValue(_copperWires, out prefabEntity))
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

            recipeRequirementBuffer = prefabEntity.ReadBuffer<RecipeRequirementBuffer>();
            recipeRequirementBuffer.Add(new RecipeRequirementBuffer { Guid = _batteryCharge, Amount = 1 });
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

            recipeRequirementBuffer = prefabEntity.ReadBuffer<RecipeRequirementBuffer>();
            recipeRequirementBuffer.Add(new RecipeRequirementBuffer { Guid = _lesserStygian, Amount = 3 });
            recipeRequirementBuffer.Add(new RecipeRequirementBuffer { Guid = _bloodEssence, Amount = 5 });
        }

        if (PrefabCollectionSystem._PrefabGuidToEntityMap.TryGetValue(_goldOre, out prefabEntity))
        {
            if (!prefabEntity.Has<Salvageable>())
            {
                prefabEntity.Add<Salvageable>();
            }

            prefabEntity.With((ref Salvageable salvageable) =>
            {
                salvageable.RecipeGUID = PrefabGUID.Empty;
                salvageable.SalvageFactor = 1f;
                salvageable.SalvageTimer = 10f;
            });

            if (!prefabEntity.Has<RecipeRequirementBuffer>())
            {
                prefabEntity.AddBuffer<RecipeRequirementBuffer>();
            }

            recipeRequirementBuffer = prefabEntity.ReadBuffer<RecipeRequirementBuffer>();
            recipeRequirementBuffer.Add(new RecipeRequirementBuffer { Guid = _goldJewelry, Amount = 2 });
        }

        if (PrefabCollectionSystem._PrefabGuidToEntityMap.TryGetValue(_radiantFibre, out prefabEntity))
        {
            if (!prefabEntity.Has<Salvageable>())
            {
                prefabEntity.Add<Salvageable>();
            }

            prefabEntity.With((ref Salvageable salvageable) =>
            {
                salvageable.RecipeGUID = PrefabGUID.Empty;
                salvageable.SalvageFactor = 1f;
                salvageable.SalvageTimer = 10f;
            });

            if (!prefabEntity.Has<RecipeRequirementBuffer>())
            {
                prefabEntity.AddBuffer<RecipeRequirementBuffer>();
            }

            recipeRequirementBuffer = prefabEntity.ReadBuffer<RecipeRequirementBuffer>();
            recipeRequirementBuffer.Add(new RecipeRequirementBuffer { Guid = Prefabs.Item_Ingredient_Gemdust, Amount = 8 });
            recipeRequirementBuffer.Add(new RecipeRequirementBuffer { Guid = Prefabs.Item_Ingredient_Plant_PlantFiber, Amount = 16 });
            recipeRequirementBuffer.Add(new RecipeRequirementBuffer { Guid = Prefabs.Item_Ingredient_Pollen, Amount = 24 });
        }

        if (DataService._primalCost.HasValue() && PrefabCollectionSystem._PrefabGuidToEntityMap.TryGetValue(DataService._primalCost, out Entity costEntity) && costEntity.Has<ItemData>())
        {
            recipeEntity = PrefabCollectionSystem._PrefabGuidToEntityMap[_extractShardRecipe];

            recipeRequirementBuffer = recipeEntity.ReadBuffer<RecipeRequirementBuffer>();

            RecipeRequirementBuffer extractRequirement = recipeRequirementBuffer[0];

            extractRequirement.Guid = DataService._primalCost;
            recipeRequirementBuffer[0] = extractRequirement;

            recipeOutputBuffer = recipeEntity.ReadBuffer<RecipeOutputBuffer>();
            recipeOutputBuffer.Add(new RecipeOutputBuffer { Guid = _itemJewelTemplate, Amount = 1 });

            foreach (PrefabGUID shardRecipe in _shardRecipes)
            {
                recipeEntity = PrefabCollectionSystem._PrefabGuidToEntityMap[shardRecipe];
                PrefabGUID soulShard = _recipesToShards[shardRecipe];

                recipeRequirementBuffer = recipeEntity.ReadBuffer<RecipeRequirementBuffer>();
                recipeRequirementBuffer.Add(new RecipeRequirementBuffer { Guid = soulShard, Amount = 1 });
                recipeRequirementBuffer.Add(new RecipeRequirementBuffer { Guid = DataService._primalCost, Amount = 1 });
            }
        }

        if (PrefabCollectionSystem._PrefabGuidToEntityMap.TryGetValue(_batteryCharge, out prefabEntity))
        {
            if (prefabEntity.Has<Salvageable>()) prefabEntity.Remove<Salvageable>();
            if (prefabEntity.Has<RecipeRequirementBuffer>()) prefabEntity.Remove<RecipeRequirementBuffer>();
        }

        Entity stationEntity = PrefabCollectionSystem._PrefabGuidToEntityMap[_advancedGrinder];
        recipeEntity = PrefabCollectionSystem._PrefabGuidToEntityMap[_vampiricDustRecipe];

        recipeEntity.With((ref RecipeData recipeData) =>
        {
            recipeData.AlwaysUnlocked = true;
            recipeData.HideInStation = false;
            recipeData.HudSortingOrder = 0;
        });

        recipeMap[_vampiricDustRecipe] = recipeEntity.Read<RecipeData>();

        var refinementBuffer = stationEntity.ReadBuffer<RefinementstationRecipesBuffer>();
        refinementBuffer.Add(new RefinementstationRecipesBuffer { RecipeGuid = _vampiricDustRecipe, Disabled = false, Unlocked = true });

        stationEntity = PrefabCollectionSystem._PrefabGuidToEntityMap[_fabricator];
        recipeEntity = PrefabCollectionSystem._PrefabGuidToEntityMap[_copperWiresRecipe];

        recipeEntity.With((ref RecipeData recipeData) =>
        {
            recipeData.AlwaysUnlocked = true;
            recipeData.HideInStation = false;
            recipeData.HudSortingOrder = 0;
            recipeData.CraftDuration = 10f;
        });

        recipeMap[_copperWiresRecipe] = recipeEntity.Read<RecipeData>();

        recipeEntity = PrefabCollectionSystem._PrefabGuidToEntityMap[_chargedBatteryRecipe];

        recipeRequirementBuffer = recipeEntity.ReadBuffer<RecipeRequirementBuffer>();
        recipeRequirementBuffer.Add(new RecipeRequirementBuffer { Guid = _batteryCharge, Amount = 1 });

        recipeEntity.With((ref RecipeData recipeData) =>
        {
            recipeData.CraftDuration = 90f;
            recipeData.AlwaysUnlocked = true;
            recipeData.HideInStation = false;
            recipeData.HudSortingOrder = 0;
        });

        recipeMap[_chargedBatteryRecipe] = recipeEntity.Read<RecipeData>();

        refinementBuffer = stationEntity.ReadBuffer<RefinementstationRecipesBuffer>();
        refinementBuffer.Add(new RefinementstationRecipesBuffer { RecipeGuid = _copperWiresRecipe, Disabled = false, Unlocked = true });
        refinementBuffer.Add(new RefinementstationRecipesBuffer { RecipeGuid = _chargedBatteryRecipe, Disabled = false, Unlocked = true });
    }
}

