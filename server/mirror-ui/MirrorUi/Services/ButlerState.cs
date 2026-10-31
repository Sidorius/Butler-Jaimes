namespace MirrorUi.Services;

/// <summary>Butler states, mirrors the LED states from CLAUDE.md section 7.</summary>
public enum ButlerState
{
    Idle,
    Listening,
    Thinking,
    Speaking,
    Error,
    Routine,
}

public sealed record DeviceStatus(string Id, bool? IsOn, bool Reachable, DateTimeOffset UpdatedAt);
