using Eclipse.Services;

namespace Eclipse.States;

internal class QuestState
{
    internal DataService.TargetType DailyTargetType { get; set; } = DataService.TargetType.Kill;
    internal int DailyProgress { get; set; }
    internal int DailyGoal { get; set; }
    internal string DailyTarget { get; set; } = string.Empty;
    internal bool DailyVBlood { get; set; }

    internal DataService.TargetType WeeklyTargetType { get; set; } = DataService.TargetType.Kill;
    internal int WeeklyProgress { get; set; }
    internal int WeeklyGoal { get; set; }
    internal string WeeklyTarget { get; set; } = string.Empty;
    internal bool WeeklyVBlood { get; set; }
}
