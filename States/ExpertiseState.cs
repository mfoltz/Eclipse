using System.Collections.Generic;

namespace Eclipse.States;

internal class ExpertiseState
{
    internal string ExpertiseType { get; set; } = string.Empty;
    internal float Progress { get; set; }
    internal int Level { get; set; }
    internal int Prestige { get; set; }
    internal int MaxLevel { get; set; } = 100;
    internal List<string> BonusStats { get; set; } = new() { "", "", "" };
}
