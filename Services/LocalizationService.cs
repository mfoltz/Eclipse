using Stunlock.Core;
using System.Reflection;
using System.Text.Json;
using static Eclipse.Resources.Localization.PrefabNames;

namespace Eclipse.Services;
internal class LocalizationService
{
    struct Codes
    {
        public string Key { get; set; }
        public string Value { get; set; }
        public string Description { get; set; }
    }
    struct Nodes
    {
        public string Guid { get; set; }
        public string Text { get; set; }
    }
    struct Words
    {
        public string Original { get; set; }
        public string Translation { get; set; }
    }
    struct LocalizationFile
    {
        public Codes[] Codes { get; set; }
        public Nodes[] Nodes { get; set; }
        public Words[] Words { get; set; }
    }

    /*
    static readonly string _language = ConfigService.LanguageLocalization;
    static readonly Dictionary<string, string> _localizedLanguages = new()
    {
        {"English", "Bloodcraft.Localization.English.json"},
        {"German", "Bloodcraft.Localization.German.json"},
        {"French", "Bloodcraft.Localization.French.json"},
        {"Spanish", "Bloodcraft.Localization.Spanish.json"},
        {"Italian", "Bloodcraft.Localization.Italian.json"},
        {"Japanese", "Bloodcraft.Localization.Japanese.json"},
        {"Koreana", "Bloodcraft.Localization.Koreana.json"},
        {"Portuguese", "Bloodcraft.Localization.Portuguese.json"},
        {"Russian", "Bloodcraft.Localization.Russian.json"},
        {"SimplifiedChinese", "Bloodcraft.Localization.SChinese.json"},
        {"TraditionalChinese", "Bloodcraft.Localization.TChinese.json"},
        {"Hungarian", "Bloodcraft.Localization.Hungarian.json"},
        {"Latam", "Bloodcraft.Localization.Latam.json"},
        {"Polish", "Bloodcraft.Localization.Polish.json"},
        {"Thai", "Bloodcraft.Localization.Thai.json"},
        {"Turkish", "Bloodcraft.Localization.Turkish.json"},
        {"Vietnamese", "Bloodcraft.Localization.Vietnamese.json"},
        {"Brazilian", "Bloodcraft.Localization.Brazilian.json"}
    };
    */

    static IReadOnlyDictionary<PrefabGUID, string> LocalizedNameKeys => _localizedNameKeys;
    static IReadOnlyDictionary<string, PrefabGUID> NameKeysToPrefabGuid => _nameKeysToPrefabGuid;
    static Dictionary<string, PrefabGUID> _nameKeysToPrefabGuid;

    static readonly Dictionary<string, string> _guidStringsToLocalizedNames = [];
    static readonly Dictionary<string, string> _localizedNamesToGuidStrings = [];
    public static IReadOnlyDictionary<PrefabGUID, string> PrefabGuidsToNames => _prefabGuidsToNames;
    static readonly Dictionary<PrefabGUID, string> _prefabGuidsToNames = [];
    public LocalizationService()
    {
        InitializeLocalizations();
    }
    static void InitializeLocalizations()
    {
        LoadGuidStringsToLocalizedNames();
    }
    static void InitializePrefabGuidNames()
    {
        var namesToPrefabGuids = Core.SystemService.PrefabCollectionSystem.SpawnableNameToPrefabGuidDictionary;

        foreach (var kvp in namesToPrefabGuids)
        {
            _prefabGuidsToNames[kvp.Value] = kvp.Key;
        }
    }
    static void LoadGuidStringsToLocalizedNames()
    {
        // string resourceName = _localizedLanguages.ContainsKey(_language) ? _localizedLanguages[_language] : "Eclipse.Localization.English.json";
        string resourceName = "Eclipse.Resources.Localization.English.json";

        Assembly assembly = Assembly.GetExecutingAssembly();
        Stream stream = assembly.GetManifestResourceStream(resourceName);

        using StreamReader localizationReader = new(stream);
        string jsonContent = localizationReader.ReadToEnd();

        var localizationFile = JsonSerializer.Deserialize<LocalizationFile>(jsonContent);
        var nodesDict = localizationFile.Nodes
            .ToDictionary(x => x.Guid, x => x.Text);

        nodesDict.ForEach(kvp => _guidStringsToLocalizedNames[kvp.Key] = kvp.Value);
        nodesDict.ForEach(kvp => _localizedNamesToGuidStrings[kvp.Value] = kvp.Key);

        _nameKeysToPrefabGuid = _localizedNameKeys.Reverse();
    }
    public static string GetGuidString(PrefabGUID prefabGuid)
    {
        if (LocalizedNameKeys.TryGetValue(prefabGuid, out string guidString))
        {
            return guidString;
        }

        return string.Empty;
    }
    public static string GetNameFromGuidString(string guidString)
    {
        if (_guidStringsToLocalizedNames.TryGetValue(guidString, out string localizedName))
        {
            return localizedName;
        }

        return string.Empty;
    }
    public static PrefabGUID GetPrefabGuidFromName(string name)
    {
        if (_localizedNamesToGuidStrings.TryGetValue(name, out string guidString) && NameKeysToPrefabGuid.TryGetValue(guidString, out PrefabGUID prefabGuid))
        {
            return prefabGuid;
        }

        return PrefabGUID.Empty;
    }
}
