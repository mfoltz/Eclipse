using System;
using System.Collections.Generic;
using System.Text.Json;
using Eclipse.DTOs;

namespace Eclipse.Services;

internal interface IDataParser
{
    void ParseConfig(string data);
    void ParsePlayer(string data);
}

internal class LegacyDataParser : IDataParser
{
    public void ParseConfig(string data)
    {
        List<string> parts = DataService.ParseMessageString(data);
        DataService.ParseConfigData(parts);
    }

    public void ParsePlayer(string data)
    {
        List<string> parts = DataService.ParseMessageString(data);
        DataService.ParsePlayerData(parts);
    }
}

internal class JsonDataParser : IDataParser
{
    public void ParseConfig(string data)
    {
        try
        {
            ConfigDto? dto = JsonSerializer.Deserialize<ConfigDto>(data);
            if (dto != null)
            {
                DataService.ApplyConfigDto(dto);
            }
        }
        catch (Exception ex)
        {
            Core.Log.LogWarning($"Failed to parse config JSON: {ex}");
        }
    }

    public void ParsePlayer(string data)
    {
        try
        {
            PlayerDataDto? dto = JsonSerializer.Deserialize<PlayerDataDto>(data);
            if (dto != null)
            {
                DataService.ApplyPlayerDto(dto);
            }
        }
        catch (Exception ex)
        {
            Core.Log.LogWarning($"Failed to parse player JSON: {ex}");
        }
    }
}
