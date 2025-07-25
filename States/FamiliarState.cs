namespace Eclipse.States;

internal class FamiliarState
{
    internal float Progress { get; set; }
    internal int Level { get; set; } = 1;
    internal int Prestige { get; set; }
    internal int MaxLevel { get; set; } = 90;
    internal string Name { get; set; } = string.Empty;
    internal List<string> Stats { get; set; } = new() { "", "", "" };
}
