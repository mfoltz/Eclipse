using Eclipse.Services;

namespace Eclipse.Tests;

public class DataServiceParseTests
{
    [Fact]
    public void ParsePlayerData_WithShortList_DoesNotThrow()
    {
        var shortList = new List<string> { "1" };
        var exception = Record.Exception(() => DataService.ParsePlayerData(shortList));
        Assert.Null(exception);
    }

    [Fact]
    public void ParsePlayerData_WithMalformedData_DoesNotThrow()
    {
        var malformed = Enumerable.Repeat("bad", 10).ToList();
        var exception = Record.Exception(() => DataService.ParsePlayerData(malformed));
        Assert.Null(exception);
    }

    [Fact]
    public void ParseConfigData_WithShortList_DoesNotThrow()
    {
        var shortList = new List<string> { "1" };
        var exception = Record.Exception(() => DataService.ParseConfigData(shortList));
        Assert.Null(exception);
    }

    [Fact]
    public void ParseConfigData_WithMalformedData_DoesNotThrow()
    {
        var malformed = Enumerable.Repeat("bad", 5).ToList();
        var exception = Record.Exception(() => DataService.ParseConfigData(malformed));
        Assert.Null(exception);
    }
}
