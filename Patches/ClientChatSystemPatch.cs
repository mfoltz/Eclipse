using Bloodcraft.Resources;
using Eclipse.Services;
using HarmonyLib;
using Il2CppInterop.Runtime;
using ProjectM;
using ProjectM.Network;
using ProjectM.UI;
using Stunlock.Core;
using System.Collections;
using System.Text;
using System.Text.RegularExpressions;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;
using Eclipse.Utilities.Extensions;
using CryptographicOperations = System.Security.Cryptography.CryptographicOperations;
using HMACSHA256 = System.Security.Cryptography.HMACSHA256;

namespace Eclipse.Patches;

[HarmonyPatch]
internal static class ClientChatSystemPatch
{
    static EntityManager EntityManager => Core.EntityManager;
    static Entity LocalCharacter => Core.LocalCharacter;
    static Entity LocalUser => Core.LocalUser;

    static readonly bool _shouldInitialize = Plugin.Leveling || Plugin.Expertise || Plugin.Legacies || Plugin.Quests || Plugin.Familiars || Plugin.Professions;
    public static bool _userRegistered = false;
    public static bool _pending = false;

    static readonly Regex _regexExtract = new(@"^\[(\d+)\]:");
    static readonly Regex _regexMAC = new(@";mac([^;]+)$");

    static readonly WaitForSeconds _registrationDelay = new(2.5f);
    static readonly WaitForSeconds _pendingDelay = new(10f);

    static readonly ComponentType[] _networkEventComponents =
    [
        ComponentType.ReadOnly(Il2CppType.Of<FromCharacter>()),
        ComponentType.ReadOnly(Il2CppType.Of<NetworkEventType>()),
        ComponentType.ReadOnly(Il2CppType.Of<SendNetworkEventTag>()),
        ComponentType.ReadOnly(Il2CppType.Of<ChatMessageEvent>())
    ];

    static readonly NetworkEventType _networkEventType = new()
    {
        IsAdminEvent = false,
        EventId = NetworkEvents.EventId_ChatMessageEvent,
        IsDebugEvent = false,
    };

    public const string V1_3 = "1.3";
    public const string VERSION = MyPluginInfo.PLUGIN_VERSION;
    public enum NetworkEventSubType
    {
        RegisterUser,
        ProgressToClient,
        ConfigsToClient
    }

    [HarmonyBefore("gg.deca.Bloodstone")]
    [HarmonyPatch(typeof(ClientChatSystem), nameof(ClientChatSystem.OnUpdate))]
    [HarmonyPrefix]
    static void OnUpdatePrefix(ClientChatSystem __instance)
    {
        if (!Core._initialized) return;
        else if (!LocalCharacter.Exists() || !LocalUser.Exists()) return;
        else if (!_userRegistered && !_pending)
        {
            _pending = true;

            try
            {
                string stringId = LocalUser.GetUser().PlatformId.ToString();
                string message = $"{VERSION};{stringId}";

                SendMessageDelayRoutine(message, VERSION).Start();
                // testing
                // Core.ScaleAndReplaceCaps(LocalCharacter, 2f);
            }
            catch (Exception ex)
            {
                Core.Log.LogError($"Failed sending registration payload to server! Error - {ex}");
            }
        }

        NativeArray<Entity> entities = __instance._ReceiveChatMessagesQuery.ToEntityArray(Allocator.Temp);

        try
        {
            foreach (Entity entity in entities)
            {
                if (entity.Has<ChatMessageServerEvent>())
                {
                    ChatMessageServerEvent chatMessage = entity.Read<ChatMessageServerEvent>();
                    string message = chatMessage.MessageText.Value;

                    if (chatMessage.MessageType.Equals(ServerChatMessageType.System) && CheckMAC(message, out string originalMessage))
                    {
                        HandleServerMessage(originalMessage);
                        EntityManager.DestroyEntity(entity);
                    }
                }
            }
        }
        finally
        {
            entities.Dispose();
        }
    }
    static IEnumerator SendMessageDelayRoutine(string message, string modVersion)
    {
        yield return _registrationDelay;

        // if (_userRegistered) yield break;

        SendMessage(NetworkEventSubType.RegisterUser, message, modVersion);
    }
    static void SendMessage(NetworkEventSubType subType, string message, string modVersion)
    {
        string intermediateMessage = $"[ECLIPSE][{(int)subType}]:{message}";
        string messageWithMAC;

        messageWithMAC = modVersion switch
        {
            _ when modVersion.StartsWith(V1_3) => $"{intermediateMessage};mac{GenerateMACV1_3(intermediateMessage)}",
            _ => string.Empty
        };

        if (string.IsNullOrEmpty(messageWithMAC)) return;

        ChatMessageEvent chatMessageEvent = new()
        {
            MessageText = new FixedString512Bytes(messageWithMAC),
            MessageType = ChatMessageType.Local,
            ReceiverEntity = LocalUser.GetNetworkId()
        };

        Entity networkEntity = EntityManager.CreateEntity(_networkEventComponents);
        networkEntity.Write(new FromCharacter { Character = LocalCharacter, User = LocalUser });
        networkEntity.Write(_networkEventType);
        networkEntity.Write(chatMessageEvent);

        // Core.Log.LogInfo($"Registration payload sent to server ({DateTime.Now}) - {messageWithMAC}");
    }
    static void HandleServerMessage(string message)
    {
        if (int.TryParse(_regexExtract.Match(message).Groups[1].Value, out int result))
        {
            try
            {
                switch (result)
                {
                    case (int)NetworkEventSubType.ProgressToClient:
                        List<string> playerData = DataService.ParseMessageString(_regexExtract.Replace(message, ""));
                        DataService.ParsePlayerData(playerData);

                        if (CanvasService._killSwitch)
                        {
                            CanvasService._killSwitch = false;
                        }

                        CanvasService._active = true;

                        break;
                    case (int)NetworkEventSubType.ConfigsToClient:
                        List<string> configData = DataService.ParseMessageString(_regexExtract.Replace(message, ""));
                        DataService.ParseConfigData(configData);

                        _userRegistered = true;

                        break;
                }
            }
            catch (Exception ex)
            {
                Core.Log.LogError($"{MyPluginInfo.PLUGIN_NAME}[{MyPluginInfo.PLUGIN_VERSION}] failed to handle message after parsing event type - {ex}");
            }
        }
        else
        {
            Core.Log.LogWarning($"{MyPluginInfo.PLUGIN_NAME}[{MyPluginInfo.PLUGIN_VERSION}] failed to parse event type after MAC verification - {message}");
        }
    }
    static bool CheckMAC(string receivedMessage, out string originalMessage)
    {
        Match match = _regexMAC.Match(receivedMessage);
        originalMessage = string.Empty;

        if (match.Success)
        {
            string receivedMAC = match.Groups[1].Value;
            string intermediateMessage = _regexMAC.Replace(receivedMessage, "");

            if (VerifyMAC(intermediateMessage, receivedMAC, Core.NEW_SHARED_KEY))
            {
                originalMessage = intermediateMessage;
                return true;
            }
            else
            {
                Core.Log.LogInfo($"MAC verification failed for matched RegEx message - {receivedMessage}");
            }
        }

        return false;
    }
    static bool VerifyMAC(string message, string receivedMAC, byte[] key)
    {
        using var hmac = new HMACSHA256(key);
        byte[] messageBytes = Encoding.UTF8.GetBytes(message);

        byte[] hashBytes = hmac.ComputeHash(messageBytes);
        string recalculatedMAC = Convert.ToBase64String(hashBytes);

        return CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(recalculatedMAC),
            Encoding.UTF8.GetBytes(receivedMAC));
    }
    static string GenerateMACV1_3(string message)
    {
        using var hmac = new HMACSHA256(Core.NEW_SHARED_KEY);
        byte[] messageBytes = Encoding.UTF8.GetBytes(message);

        byte[] hashBytes = hmac.ComputeHash(messageBytes);

        return Convert.ToBase64String(hashBytes);
    }
    static void OnInputChange()
    {
        Core.Log.LogWarning($"[OnInputChange]");
    }
}
