using Eclipse.Services;

namespace Eclipse.Elements.States;

internal class QuestState
{
    public DataService.TargetType DailyTargetType { get; set; } = DataService.TargetType.Kill;
    public int DailyProgress { get; set; }
    public int DailyGoal { get; set; }
    public string DailyTarget { get; set; } = string.Empty;
    public bool DailyVBlood { get; set; }

    public DataService.TargetType WeeklyTargetType { get; set; } = DataService.TargetType.Kill;
    public int WeeklyProgress { get; set; }
    public int WeeklyGoal { get; set; }
    public string WeeklyTarget { get; set; } = string.Empty;
    public bool WeeklyVBlood { get; set; }
}
