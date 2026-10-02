using System.Reflection;
using System.Text.Json.Serialization;
using SPTarkov.DI.Annotations;
using SPTarkov.Reflection.Patching;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Helpers.Traders;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Models.Eft.Ws;
using SPTarkov.Server.Core.Models.Enums;
using SPTarkov.Server.Core.Models.Spt.Mod;
using SPTarkov.Server.Core.Servers.Ws;
using SPTarkov.Server.Core.Services.Commerce;

namespace TraderRestockRefresh;

public record ModMetadata : IModMetadata
{
    public string ModGuid { get; init; } = "com.fenrirthegray.traderrestockrefresh";
    public string Name { get; init; } = "TraderRestockRefresh";
    public string Author { get; init; } = "FenrirTheGray";
    public List<string>? Contributors { get; init; }
    public SemanticVersioning.Version Version { get; init; } = new("1.0.0");
    public SemanticVersioning.Range SptVersion { get; init; } = new("~4.1.0");
    public List<string>? Incompatibilities { get; init; }
    public Dictionary<string, SemanticVersioning.Range>? ModDependencies { get; init; }
    public string? Url { get; init; } = "https://github.com/FenrirTheGray/spt-trader-restock-refresh";
    public string License { get; init; } = "MIT";
    public bool HasPrepatcher { get; init; } = false;
}

[Injectable(TypePriority = OnLoadOrder.Preload)]
public class TraderRestockRefresh(SptWebSocketConnectionHandler webSocket) : IOnLoad
{
    private static SptWebSocketConnectionHandler? _webSocket;

    public Task OnLoadAsync(CancellationToken cancellationToken)
    {
        _webSocket = webSocket;
        new ResetExpiredTraderPatch().Enable();
        new FenceRefreshPatch().Enable();
        return Task.CompletedTask;
    }

    public static void Notify(string traderId)
    {
        _webSocket?.SendMessageToAll(
            new WsTraderSupply { EventType = NotificationEventType.trader_supply, EventIdentifier = new MongoId(), TraderId = traderId }
        );
    }
}

public record WsTraderSupply : WsNotificationEvent
{
    [JsonPropertyName("tid")]
    public required string TraderId { get; init; }
}

public class ResetExpiredTraderPatch : AbstractPatch
{
    protected override MethodBase GetTargetMethod() => typeof(TraderAssortHelper).GetMethod(nameof(TraderAssortHelper.ResetExpiredTrader))!;

    [PatchPostfix]
    public static void Postfix(Trader trader) => TraderRestockRefresh.Notify(trader.Base.Id);
}

public class FenceRefreshPatch : AbstractPatch
{
    protected override MethodBase GetTargetMethod() => typeof(FenceService).GetMethod(nameof(FenceService.GenerateFenceAssorts))!;

    [PatchPostfix]
    public static void Postfix() => TraderRestockRefresh.Notify(Traders.FENCE);
}
