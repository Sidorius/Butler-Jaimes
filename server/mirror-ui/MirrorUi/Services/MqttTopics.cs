namespace MirrorUi.Services;

/// <summary>
/// Proposed topic convention (Phase 2 task "MQTT-Topic-Konvention festlegen"):
///   butler/state                  payload: idle|listening|thinking|speaking|error|routine
///   butler/device/{id}/state      payload JSON: {"on":true,"reachable":true}
/// To be confirmed and written into the docs.
/// </summary>
public static class MqttTopics
{
    public const string ButlerState = "butler/state";
    public const string DevicePrefix = "butler/device/";
    public const string DeviceFilter = "butler/device/+/state";
}
