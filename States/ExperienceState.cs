using Eclipse.Services;

namespace Eclipse.States;

internal class ExperienceState
{
    internal float Progress { get; set; }
    internal int Level { get; set; }
    internal int Prestige { get; set; }
    internal DataService.PlayerClass Class { get; set; }
    internal int MaxLevel { get; set; }
}
