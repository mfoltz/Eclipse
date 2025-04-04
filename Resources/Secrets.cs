﻿using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Eclipse.Resources;
internal class Secrets
{
    [JsonPropertyName("NEW_SHARED_KEY")]
    public string NewSharedKey { get; set; }
}
internal static class SecretManager
{
    static Secrets _secrets;
    static SecretManager()
    {
        LoadSecrets();
    }
    static void LoadSecrets()
    {
        var resourceName = "Eclipse.Resources.secrets.json"; // Replace with your actual namespace and file path
        var assembly = Assembly.GetExecutingAssembly();

        using var stream = assembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException($"Resource '{resourceName}' not found in assembly.");
        using var reader = new StreamReader(stream);

        var jsonContent = reader.ReadToEnd();
        _secrets = JsonSerializer.Deserialize<Secrets>(jsonContent)
            ?? throw new InvalidOperationException("Failed to deserialize secrets.json.");
    }
    public static string GetNewSharedKey()
    {
        return _secrets.NewSharedKey;
    }
}