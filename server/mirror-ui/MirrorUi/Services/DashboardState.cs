using System.Collections.Concurrent;

namespace MirrorUi.Services;

/// <summary>
/// Shared in-memory state for the dashboard (singleton).
/// The MQTT listener writes, Razor components subscribe to <see cref="Changed"/>
/// (like INotifyPropertyChanged in WPF, but one event for everything).
/// </summary>
public sealed class DashboardState
{
    private readonly ConcurrentDictionary<string, DeviceStatus> _devices = new();

    public ButlerState Butler { get; private set; } = ButlerState.Idle;
    public bool MqttConnected { get; private set; }
    public DateTimeOffset? LastMessageAt { get; private set; }

    public IReadOnlyCollection<DeviceStatus> Devices =>
        _devices.Values.OrderBy(d => d.Id).ToList();

    public event Action? Changed;

    public void SetButler(ButlerState state)
    {
        Butler = state;
        Touch();
    }

    public void SetMqttConnected(bool connected)
    {
        MqttConnected = connected;
        Changed?.Invoke();
    }

    public void SetDevice(DeviceStatus device)
    {
        _devices[device.Id] = device;
        Touch();
    }

    private void Touch()
    {
        LastMessageAt = DateTimeOffset.Now;
        Changed?.Invoke();
    }
}
