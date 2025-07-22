using Il2CppInterop.Runtime;
using ProjectM;
using ProjectM.Scripting;
using ProjectM.UI;
using Unity.Entities;

namespace Eclipse.Services;
internal class SystemService(World world)
{
    readonly World _world = world ?? throw new ArgumentNullException(nameof(world));

    ClientScriptMapper _clientScriptMapper;
    public ClientScriptMapper ClientScriptMapper => _clientScriptMapper ??= GetSystem<ClientScriptMapper>();

    PrefabCollectionSystem _prefabCollectionSystem;
    public PrefabCollectionSystem PrefabCollectionSystem => _prefabCollectionSystem ??= GetSystem<PrefabCollectionSystem>();

    GameDataSystem _gameDataSystem;
    public GameDataSystem GameDataSystem => _gameDataSystem ??= GetSystem<GameDataSystem>();

    ManagedDataSystem _managedDataSystem;
    public ManagedDataSystem ManagedDataSystem => _managedDataSystem ??= GetSystem<ManagedDataSystem>();

    UICanvasSystem _canvasSystem;
    public UICanvasSystem CanvasSystem => _canvasSystem ??= GetOrCreateSystem<UICanvasSystem>();

    TutorialSystem _tutorialSystem;
    public TutorialSystem TutorialSystem => _tutorialSystem ??= GetSystem<TutorialSystem>();

    InputActionSystem _inputActionSystem;
    public InputActionSystem InputActionSystem => _inputActionSystem ??= GetSystem<InputActionSystem>();

    InventorySubMenuMapper _inventorySubMenuMapper;
    public InventorySubMenuMapper InventorySubMenuMapper => _inventorySubMenuMapper ??= GetOrCreateSystem<InventorySubMenuMapper>();

    AbilityBarParentBinderSystem _abilityBarParentBinderSystem;
    public AbilityBarParentBinderSystem AbilityBarParentBinderSystem => _abilityBarParentBinderSystem ??= GetSystem<AbilityBarParentBinderSystem>();

    UIDataSystem _uiDataSystem;
    public UIDataSystem UIDataSystem => _uiDataSystem ??= GetSystem<UIDataSystem>();
    T GetSystem<T>() where T : ComponentSystemBase
    {
        return _world.GetExistingSystemManaged<T>() ?? throw new InvalidOperationException($"Failed to get {Il2CppType.Of<T>().FullName} from the Server...");
    }
    T GetOrCreateSystem<T>() where T : ComponentSystemBase
    {
        return _world.GetOrCreateSystemManaged<T>() ?? throw new InvalidOperationException($"Failed to get {Il2CppType.Of<T>().FullName} from the Server...");
    }
}