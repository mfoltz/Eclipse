using Eclipse.Services;

namespace Eclipse.Elements.States;

internal class LevelingState
{
    public float Progress { get; set; }
    public int Level { get; set; }
    public int Prestige { get; set; }
    public DataService.PlayerClass Class { get; set; }
    public int MaxLevel { get; set; }
}
