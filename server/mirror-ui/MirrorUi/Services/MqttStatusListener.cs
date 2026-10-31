using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using MQTTnet;

namespace MirrorUi.Services;

/// <summary>
/// Background service (like a C# IHostedService worker) that keeps one MQTT
/// connection alive and feeds <see cref="DashboardState"/>.
/// Error path: if the broker is unreachable the dashboard shows "offline" and we retry.
/// </summary>
public sealed class MqttStatusListener(
    DashboardState state,
    IOptions<MqttOptions> options,
    ILogger<MqttStatusListener> logger) : BackgroundService
{
    private static readonly TimeSpan RetryDelay = TimeSpan.FromSeconds(5);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var opts = options.Value;
        var factory = new MqttClientFactory();

        while (!stoppingToken.IsCancellationRequested)
        {
            using var client = factory.CreateMqttClient();
            var disconnected = new TaskCompletionSource();

            client.ApplicationMessageReceivedAsync += e =>
            {
                HandleMessage(e.ApplicationMessage.Topic, e.ApplicationMessage.ConvertPayloadToString());
                return Task.CompletedTask;
            };
            client.DisconnectedAsync += _ =>
            {
                disconnected.TrySetResult();
                return Task.CompletedTask;
            };

            try
            {
                var builder = new MqttClientOptionsBuilder()
                    .WithTcpServer(opts.Host, opts.Port)
                    .WithClientId($"mirror-ui-{Environment.MachineName}");
                if (!string.IsNullOrEmpty(opts.Username))
                    builder.WithCredentials(opts.Username, opts.Password);

                await client.ConnectAsync(builder.Build(), stoppingToken);
                await client.SubscribeAsync(
                    new MqttClientSubscribeOptionsBuilder()
                        .WithTopicFilter(MqttTopics.ButlerState)
                        .WithTopicFilter(MqttTopics.DeviceFilter)
                        .Build(),
                    stoppingToken);

                state.SetMqttConnected(true);
                logger.LogInformation("MQTT connected to {Host}:{Port}", opts.Host, opts.Port);

                await disconnected.Task.WaitAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogWarning("MQTT unavailable ({Message}), retrying in {Delay}s", ex.Message, RetryDelay.TotalSeconds);
            }

            state.SetMqttConnected(false);
            try { await Task.Delay(RetryDelay, stoppingToken); }
            catch (OperationCanceledException) { break; }
        }
    }

    private void HandleMessage(string topic, string payload)
    {
        try
        {
            if (topic == MqttTopics.ButlerState)
            {
                if (Enum.TryParse<ButlerState>(payload.Trim(), ignoreCase: true, out var s))
                    state.SetButler(s);
                else
                    logger.LogWarning("Unknown butler state '{Payload}'", payload);
                return;
            }

            if (topic.StartsWith(MqttTopics.DevicePrefix, StringComparison.Ordinal) && topic.EndsWith("/state", StringComparison.Ordinal))
            {
                var id = topic[MqttTopics.DevicePrefix.Length..^"/state".Length];
                using var doc = JsonDocument.Parse(payload);
                var root = doc.RootElement;
                bool? on = root.TryGetProperty("on", out var o) && o.ValueKind is JsonValueKind.True or JsonValueKind.False
                    ? o.GetBoolean() : null;
                var reachable = !root.TryGetProperty("reachable", out var r) || r.ValueKind != JsonValueKind.False;
                state.SetDevice(new DeviceStatus(id, on, reachable, DateTimeOffset.Now));
            }
        }
        catch (JsonException ex)
        {
            logger.LogWarning("Invalid payload on {Topic}: {Message}", topic, ex.Message);
        }
    }
}
