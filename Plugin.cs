using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using BepInEx.Unity.IL2CPP;
using HarmonyLib;
using System.Reflection;
using UnityEngine;

namespace Eclipse;

[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
internal class Plugin : BasePlugin
{
    Harmony _harmony;
    internal static Plugin Instance { get; set; }
    public static ManualLogSource LogInstance => Instance.Log;

    /*
    public static readonly List<string> DirectoryPaths =
    [
        Path.Combine(Paths.ConfigPath, MyPluginInfo.PLUGIN_NAME) // 0
    ];
    */

    static ConfigEntry<bool> _leveling;
    static ConfigEntry<bool> _prestige;
    static ConfigEntry<bool> _legacies;
    static ConfigEntry<bool> _expertise;
    static ConfigEntry<bool> _familiars;
    static ConfigEntry<bool> _professions;
    static ConfigEntry<bool> _quests;
    static ConfigEntry<bool> _shiftSlot;
    static ConfigEntry<bool> _eclipsed;
    public static bool Leveling => _leveling.Value;
    public static bool Prestige => _prestige.Value;
    public static bool Legacies => _legacies.Value;
    public static bool Expertise => _expertise.Value;
    public static bool Familiars => _familiars.Value;
    public static bool Professions => _professions.Value;
    public static bool Quests => _quests.Value;
    public static bool ShiftSlot => _shiftSlot.Value;
    public static bool Eclipsed => _eclipsed.Value;
    public override void Load()
    {
        Instance = this;

        if (Application.productName == "VRisingServer")
        {
            Core.Log.LogInfo($"{MyPluginInfo.PLUGIN_NAME}[{MyPluginInfo.PLUGIN_VERSION}] is a client mod! ({Application.productName})");
            return;
        }

        _harmony = Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly());
        InitConfig();
        Core.Log.LogInfo($"{MyPluginInfo.PLUGIN_NAME}[{MyPluginInfo.PLUGIN_VERSION}] loaded on client!");
    }
    static void InitConfig()
    {
        /*
        foreach (string path in DirectoryPaths)
        {
            CreateDirectory(path);
        }
        */

        _leveling = InitConfigEntry("UIOptions", "ExperienceBar", true, "Enable/Disable the experience bar, requires both ClientCompanion/LevelingSystem to be enabled in Bloodcraft.");
        _prestige = InitConfigEntry("UIOptions", "ShowPrestige", true, "Enable/Disable showing prestige level in front of experience bar, requires both ClientCompanion/PrestigeSystem to be enabled in Bloodcraft.");
        _legacies = InitConfigEntry("UIOptions", "LegacyBar", true, "Enable/Disable the legacy bar, requires both ClientCompanion/BloodSystem to be enabled in Bloodcraft.");
        _expertise = InitConfigEntry("UIOptions", "ExpertiseBar", true, "Enable/Disable the expertise bar, requires both ClientCompanion/ExpertiseSystem to be enabled in Bloodcraft.");

        _familiars = InitConfigEntry("UIOptions", "Familiars", true, "Enable/Disable showing basic familiar details bar, requires both ClientCompanion/FamiliarSystem to be enabled in Bloodcraft.");
        _professions = InitConfigEntry("UIOptions", "Professions", true, "Enable/Disable the professions tab, requires both ClientCompanion/ProfessionSystem to be enabled in Bloodcraft.");
        _quests = InitConfigEntry("UIOptions", "QuestTrackers", true, "Enable/Disable the quest tracker, requires both ClientCompanion/QuestSystem to be enabled in Bloodcraft.");
        _shiftSlot = InitConfigEntry("UIOptions", "ShiftSlot", true, "Enable/Disable the shift slot, requires both ClientCompanion and shift slot spell to be enabled in Bloodcraft.");

        _eclipsed = InitConfigEntry("UIOptions", "Eclipsed", true, "Set to false for slower update intervals (0.1s -> 1s) if performance is negatively impacted.");
    }
    static ConfigEntry<T> InitConfigEntry<T>(string section, string key, T defaultValue, string description)
    {
        // Bind the configuration entry and get its value
        var entry = Instance.Config.Bind(section, key, defaultValue, description);

        // Check if the key exists in the configuration file and retrieve its current value
        var newFile = Path.Combine(Paths.ConfigPath, $"{MyPluginInfo.PLUGIN_GUID}.cfg");

        if (File.Exists(newFile))
        {
            var config = new ConfigFile(newFile, true);
            if (config.TryGetEntry(section, key, out ConfigEntry<T> existingEntry))
            {
                // If the entry exists, update the value to the existing value
                entry.Value = existingEntry.Value;
            }
        }
        return entry;
    }
    public override bool Unload()
    {
        Config.Clear();
        _harmony.UnpatchSelf();
        return true;
    }
    static void CreateDirectory(string path)
    {
        if (!Directory.Exists(path))
        {
            Directory.CreateDirectory(path);
        }
    }
}
