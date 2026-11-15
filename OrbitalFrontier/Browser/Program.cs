using System;
using System.Runtime.InteropServices.JavaScript;
using System.Runtime.Versioning;
using System.Text.Json;
using System.Text.Json.Serialization;
using OrbitalFrontier.Core;

[SupportedOSPlatform("browser")]
public static partial class BrowserGame
{
    static GameSession session;
    static ExpeditionSession mission;
    readonly static BrowserJson Json = BrowserJson.Default;

    public static void Main() { }

    [JSExport]
    internal static string Start(string mission)
    {
        var catalog = JsonSerializer.Deserialize(mission, Json.GameCatalog) ??
            throw new ArgumentException("The mission catalog could not be read.");
        session = new GameSession(catalog);
        return Snapshot();
    }

    [JSExport]
    internal static string Apply(int command, int option, double value, double secondary)
    {
        session.Apply((Command)command, option, value, secondary);
        return Snapshot();
    }

    [JSExport]
    internal static string Tick(double realSeconds)
    {
        session.Tick(realSeconds);
        return Snapshot();
    }

    [JSExport]
    internal static string StartExpedition(string catalog, string moon, string mars)
    {
        var data = JsonSerializer.Deserialize(catalog, Json.ExpeditionCatalog) ??
            throw new ArgumentException("Unable to open the expedition catalog.");
        mission = new ExpeditionSession(data, new[] {
            new SurfaceGrid(data.worlds[0], Convert.FromBase64String(moon)),
            new SurfaceGrid(data.worlds[1], Convert.FromBase64String(mars))
        });
        return ExpeditionSnapshot();
    }

    [JSExport]
    internal static string ApplyExpedition(int command, int option, double a, double b, double c)
    {
        mission.Apply((ExpeditionCommand)command, 0, option, a, b, c);
        return ExpeditionSnapshot();
    }

    [JSExport]
    internal static string TickExpedition(double seconds)
    {
        mission.Tick(seconds);
        return ExpeditionSnapshot();
    }

    static string ExpeditionSnapshot() => JsonSerializer.Serialize(mission.View, Json.ExpeditionView);

    static string Snapshot() => JsonSerializer.Serialize(session.View, Json.GameView);
}

[JsonSourceGenerationOptions(IncludeFields = true, IgnoreReadOnlyProperties = true)]
[JsonSerializable(typeof(GameCatalog))]
[JsonSerializable(typeof(GameView))]
[JsonSerializable(typeof(ExpeditionCatalog))]
[JsonSerializable(typeof(ExpeditionView))]
internal partial class BrowserJson : JsonSerializerContext { }
