namespace Eclipse.Elements.States;

internal class ExpertiseState
{
    public string ExpertiseType { get; set; } = string.Empty;
    public float Progress { get; set; }
    public int Level { get; set; }
    public int Prestige { get; set; }
    public int MaxLevel { get; set; } = 100;
    public List<string> BonusStats { get; set; } = ["", "", ""];
}
