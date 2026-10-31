// Dev-only tool: runs an embedded MQTT broker on port 1883 and publishes demo
// states, so the dashboard can be tried without Mosquitto / real hardware.
// Usage: dotnet run --project server/mirror-ui/MqttSimulator
using MQTTnet;
using MQTTnet.Server;

var port = args.Length > 0 ? int.Parse(args[0]) : 1883;

var serverFactory = new MqttServerFactory();
var server = serverFactory.CreateMqttServer(
    new MqttServerOptionsBuilder().WithDefaultEndpoint().WithDefaultEndpointPort(port).Build());
await server.StartAsync();
Console.WriteLine($"Embedded broker on port {port}. Ctrl+C to stop.");

async Task Publish(string topic, string payload)
{
    // Retained so a dashboard that connects later still sees the last state.
    await server.InjectApplicationMessage(new InjectedMqttApplicationMessage(
        new MqttApplicationMessageBuilder()
            .WithTopic(topic).WithPayload(payload).WithRetainFlag().Build()));
    Console.WriteLine($"{topic} <- {payload}");
}

using var cts = new CancellationTokenSource();
Console.CancelKeyPress += (_, e) => { e.Cancel = true; cts.Cancel(); };

var states = new[] { "idle", "listening", "thinking", "speaking", "idle", "routine", "error" };
var i = 0;
try
{
    while (!cts.IsCancellationRequested)
    {
        await Publish("butler/state", states[i % states.Length]);
        await Publish("butler/device/wohnzimmerlicht/state", $$"""{"on":{{(i % 2 == 0 ? "true" : "false")}},"reachable":true}""");
        // Every 3rd round the coffee plug is "unreachable" to show the UC-17 path.
        await Publish("butler/device/kaffeemaschine/state", $$"""{"on":false,"reachable":{{(i % 3 == 2 ? "false" : "true")}}}""");
        i++;
        await Task.Delay(TimeSpan.FromSeconds(4), cts.Token);
    }
}
catch (OperationCanceledException) { }

await server.StopAsync();
