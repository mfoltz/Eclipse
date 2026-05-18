using BloodcraftEclipseBridge.Messages;
using Eclipse.Patches;
using ProjectM.Network;
using System.Reflection;

namespace Eclipse.Services;

internal static class EmberglassEclipseBridge
{
    const string EMBERGLASS_ASSEMBLY_NAME = "Emberglass";
    const string VNETWORK_TYPE_NAME = "Emberglass.API.Shared.VNetwork";

    static bool _initialized;
    static bool _available;
    static bool _clientReady;
    static bool _disabledForSession;
    static bool _unavailableLogged;
    static bool _clientReadyLogged;
    static bool _notReadyLogged;
    static bool _progressReceiptLogged;
    static MethodInfo _sendToServer;
    static PropertyInfo _isReady;
    static EventInfo _onReady;
    static EventInfo _onClientReady;
    static string _pendingRegistrationMessage;

    public static void Initialize()
    {
        if (_initialized || !Plugin.UseEmberglassBridge || _disabledForSession)
        {
            return;
        }

        _initialized = true;

        if (!TryResolveVNetwork(out Type vNetworkType))
        {
            LogUnavailable("Emberglass is not loaded");
            return;
        }

        try
        {
            MethodInfo registerClientbound = GetGenericMethod(vNetworkType, "RegisterClientbound", 1);
            _sendToServer = GetGenericMethod(vNetworkType, "SendToServer", 1);
            _isReady = vNetworkType.GetProperty("IsReady", BindingFlags.Public | BindingFlags.Static);
            _onReady = vNetworkType.GetEvent("OnReady", BindingFlags.Public | BindingFlags.Static);
            _onClientReady = vNetworkType.GetEvent("OnClientReady", BindingFlags.Public | BindingFlags.Static);

            registerClientbound
                .MakeGenericMethod(typeof(EclipseServerMessagePacket))
                .Invoke(null, [new Action<User, EclipseServerMessagePacket>(OnServerMessagePacket)]);

            SubscribeReadinessEvent(_onClientReady);
            SubscribeReadinessEvent(_onReady);

            _available = true;
            Core.Log.LogInfo("[EclipseBridge:Emberglass] registered");
        }
        catch (Exception ex)
        {
            DisableForSession($"failed to register bridge ({ex.GetType().Name}: {ex.Message})");
        }
    }

    public static bool TrySendRegistration(string message)
    {
        if (!Plugin.UseEmberglassBridge || _disabledForSession)
        {
            return false;
        }

        Initialize();

        if (!_available)
        {
            return false;
        }

        if (!IsClientReady())
        {
            _pendingRegistrationMessage = message;
            LogNotReady();
            return false;
        }

        bool sent = TrySendRegistrationNow(message);
        if (sent)
        {
            _pendingRegistrationMessage = null;
        }

        return sent;
    }

    static bool TrySendRegistrationNow(string message)
    {
        try
        {
            _sendToServer
                .MakeGenericMethod(typeof(EclipseRegistrationPacket))
                .Invoke(null, [new EclipseRegistrationPacket(message)]);

            Core.Log.LogInfo("[EclipseBridge:Emberglass] registration sent");
            return true;
        }
        catch (Exception ex)
        {
            DisableForSession($"failed to send registration ({ex.GetType().Name}: {ex.Message})");
            return false;
        }
    }

    static void OnServerMessagePacket(User sender, EclipseServerMessagePacket packet)
    {
        if (string.IsNullOrWhiteSpace(packet.Message))
        {
            Core.Log.LogWarning("[EclipseBridge:Emberglass] empty server message received");
            return;
        }

        if (!ClientChatSystemPatch.TryHandleBridgeServerMessage(packet.Message, out string messageKind))
        {
            Core.Log.LogWarning("[EclipseBridge:Emberglass] failed to verify server message MAC");
            return;
        }

        LogReceivedServerMessage(messageKind);
    }

    static void LogReceivedServerMessage(string messageKind)
    {
        if (string.Equals(messageKind, "configs", StringComparison.OrdinalIgnoreCase))
        {
            _progressReceiptLogged = false;
            Core.Log.LogInfo("[EclipseBridge:Emberglass] configs received");
            return;
        }

        if (string.Equals(messageKind, "progress", StringComparison.OrdinalIgnoreCase))
        {
            if (_progressReceiptLogged)
            {
                return;
            }

            _progressReceiptLogged = true;
            Core.Log.LogInfo("[EclipseBridge:Emberglass] progress received");
            return;
        }

        Core.Log.LogInfo($"[EclipseBridge:Emberglass] {messageKind} received");
    }

    static void OnClientReady()
    {
        _clientReady = true;

        if (_clientReadyLogged)
        {
            TrySendPendingRegistration();
            return;
        }

        _clientReadyLogged = true;
        Core.Log.LogInfo("[EclipseBridge:Emberglass] client ready");
        TrySendPendingRegistration();
    }

    static void OnReady(User user)
    {
        OnClientReady();
    }

    static void SubscribeReadinessEvent(EventInfo readinessEvent)
    {
        if (readinessEvent?.EventHandlerType is not { } handlerType)
        {
            return;
        }

        MethodInfo handlerMethod = ResolveReadinessHandler(handlerType);
        if (handlerMethod == null)
        {
            return;
        }

        readinessEvent.AddEventHandler(null, Delegate.CreateDelegate(handlerType, handlerMethod));
    }

    static MethodInfo ResolveReadinessHandler(Type handlerType)
    {
        MethodInfo invoke = handlerType.GetMethod("Invoke");
        if (invoke?.ReturnType != typeof(void))
        {
            return null;
        }

        ParameterInfo[] parameters = invoke.GetParameters();
        if (parameters.Length == 0)
        {
            return typeof(EmberglassEclipseBridge).GetMethod(
                nameof(OnClientReady),
                BindingFlags.Static | BindingFlags.NonPublic);
        }

        if (parameters.Length == 1 && parameters[0].ParameterType == typeof(User))
        {
            return typeof(EmberglassEclipseBridge).GetMethod(
                nameof(OnReady),
                BindingFlags.Static | BindingFlags.NonPublic);
        }

        return null;
    }

    static bool TryResolveVNetwork(out Type vNetworkType)
    {
        Assembly assembly = AppDomain.CurrentDomain
            .GetAssemblies()
            .FirstOrDefault(loadedAssembly => loadedAssembly.GetName().Name == EMBERGLASS_ASSEMBLY_NAME);

        vNetworkType = assembly?.GetType(VNETWORK_TYPE_NAME, throwOnError: false);
        return vNetworkType != null;
    }

    static MethodInfo GetGenericMethod(Type declaringType, string name, int parameterCount)
    {
        return declaringType
            .GetMethods(BindingFlags.Public | BindingFlags.Static)
            .Single(method =>
                method.Name == name
                && method.IsGenericMethodDefinition
                && method.GetParameters().Length == parameterCount);
    }

    static bool IsClientReady()
    {
        if (_clientReady)
        {
            return true;
        }

        if (IsReady())
        {
            OnClientReady();
            return true;
        }

        return false;
    }

    static bool IsReady()
    {
        return _isReady?.GetValue(null) is true;
    }

    static void TrySendPendingRegistration()
    {
        if (string.IsNullOrWhiteSpace(_pendingRegistrationMessage) || !_available || _disabledForSession)
        {
            return;
        }

        string message = _pendingRegistrationMessage;
        if (TrySendRegistrationNow(message))
        {
            _pendingRegistrationMessage = null;
            Core.Log.LogInfo("[EclipseBridge:Emberglass] pending registration sent after client ready");
        }
    }

    static void LogUnavailable(string reason)
    {
        if (_unavailableLogged)
        {
            return;
        }

        _unavailableLogged = true;
        Core.Log.LogInfo($"[EclipseBridge:Emberglass] unavailable; using ChatMessage bridge ({reason})");
    }
    static void LogNotReady()
    {
        if (_notReadyLogged)
        {
            return;
        }

        _notReadyLogged = true;
        Core.Log.LogInfo("[EclipseBridge:Emberglass] client not ready; queued Emberglass retry and using ChatMessage bridge");
    }

    static void DisableForSession(string reason)
    {
        _available = false;
        _disabledForSession = true;
        Core.Log.LogWarning($"[EclipseBridge:Emberglass] disabled for this session; using ChatMessage bridge ({reason})");
    }
}
