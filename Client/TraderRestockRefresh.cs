using System.Diagnostics;
using BepInEx;
using BepInEx.Bootstrap;
using EFT.Communications;
using EFT.Trading;
using EFT.UI;
using HarmonyLib;
using UnityEngine;

namespace TraderRestockRefresh;

[BepInPlugin("com.fenrirthegray.traderrestockrefresh", "fenrirthegray-traderrestockrefresh", "1.0.0")]
public class Plugin : BaseUnityPlugin
{
    private const int EftBuild = 40743;

    private void Awake()
    {
        var running = FileVersionInfo.GetVersionInfo(BepInEx.Paths.ExecutablePath).FilePrivatePart;
        if (running != EftBuild)
        {
            var error = $"{Info.Metadata.Name} v{Info.Metadata.Version} was built for Tarkov {EftBuild}, but you are running {running}. Download the matching version.";
            Logger.LogError(error);
            Chainloader.DependencyErrors.Add(error);
            return;
        }

        Harmony.CreateAndPatchAll(typeof(Plugin));
    }

    [HarmonyPatch(typeof(NotificationManager), nameof(NotificationManager.AddNotification))]
    [HarmonyPrefix]
    private static bool AddNotificationPrefix(Notification notification)
    {
        if (notification is not NotificationTraderSupply supply)
        {
            return true;
        }

        var screen = FindObjectOfType<TraderDealScreen>();
        if (screen != null && Traverse.Create(screen).Field<Trader>("_trader").Value?.Id == supply.TraderId)
        {
            screen.UpdateAssortmentHandler();
        }

        return false;
    }
}
