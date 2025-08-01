namespace Eclipse.Elements.States;

internal class LegacyState : LevelingState
{
    public string LegacyType { get; set; } = string.Empty;
    public List<string> BonusStats { get; set; } = ["None", "None", "None"];
}
