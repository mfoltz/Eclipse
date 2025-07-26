namespace Eclipse.Elements.States;

internal class FamiliarState
{
    public float Progress { get; set; }
    public int Level { get; set; } = 1;
    public int Prestige { get; set; }
    public int MaxLevel { get; set; } = 90;
    public string Name { get; set; } = string.Empty;
    public List<string> Stats { get; set; } = ["", "", ""];
}
