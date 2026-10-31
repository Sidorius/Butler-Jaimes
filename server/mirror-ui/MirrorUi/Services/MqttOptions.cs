namespace MirrorUi.Services;

public sealed class MqttOptions
{
    public const string Section = "Mqtt";

    public string Host { get; set; } = "localhost";
    public int Port { get; set; } = 1883;
    public string? Username { get; set; }
    // Password belongs into User Secrets / env var Mqtt__Password, never into appsettings.json.
    public string? Password { get; set; }
}
