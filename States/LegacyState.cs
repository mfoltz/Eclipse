using System.Collections.Generic;
using Eclipse.Services;

namespace Eclipse.States;

internal class LegacyState : LevelingState
{
    internal string LegacyType { get; set; } = string.Empty;
    internal List<string> BonusStats { get; set; } = new();
}
