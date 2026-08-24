using System;
using System.Runtime.CompilerServices;
using BepInEx;
using HarmonyLib;
using ResidentsEatWithYou.UI;

namespace ResidentsEatWithYou;

internal static class ModInfo
{
    internal const string Guid = "omegaplatinum.elin.residentseatwithyou";
    internal const string Name = "Residents Eat With You";
    internal const string Version = "2.0.1";
    internal const string ModOptionsGuid = "evilmask.elinplugins.modoptions";
}

[BepInPlugin(GUID: ModInfo.Guid, Name: ModInfo.Name, Version: ModInfo.Version)]
[BepInDependency(ModInfo.ModOptionsGuid, BepInDependency.DependencyFlags.SoftDependency)]
internal class ResidentsEatWithYou : BaseUnityPlugin
{
    internal static ResidentsEatWithYou? Instance { get; private set; }

    private void Awake()
    {
        Instance = this;
        ResidentsEatWithYouConfig.LoadConfig(config: Config);
        Harmony.CreateAndPatchAll(type: typeof(Patcher), harmonyInstanceId: ModInfo.Guid);

        if (HasModOptionsPlugin() == false)
        {
            return;
        }

        try
        {
            RegisterModOptionsUI();
        }
        catch (Exception ex)
        {
            LogError(message: $"An error occurred during UI registration: {ex}");
        }
    }

    internal static void LogDebug(object message, [CallerMemberName] string caller = "")
    {
        Instance?.Logger.LogDebug(data: $"[{caller}] {message}");
    }

    internal static void LogInfo(object message)
    {
        Instance?.Logger.LogInfo(data: message);
    }

    internal static void LogError(object message)
    {
        Instance?.Logger.LogError(data: message);
    }

    [MethodImpl(methodImplOptions: MethodImplOptions.NoInlining)]
    private static void RegisterModOptionsUI()
    {
        UIController.RegisterUI();
    }

    private static bool HasModOptionsPlugin()
    {
        try
        {
            foreach (var obj in ModManager.ListPluginObject)
            {
                if (obj is not BaseUnityPlugin plugin)
                {
                    continue;
                }

                if (plugin.Info.Metadata.GUID == ModInfo.ModOptionsGuid)
                {
                    return true;
                }
            }

            return false;
        }
        catch (Exception ex)
        {
            LogError(message: $"Error while checking for Mod Options: {ex}");
            return false;
        }
    }
}
