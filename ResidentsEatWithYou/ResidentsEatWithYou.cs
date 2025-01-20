using BepInEx;
using HarmonyLib;

namespace ResidentsEatWithYou
{
    internal static class ModInfo
    {
        internal const string Guid = "omegaplatinum.elin.residentseatwithyou";
        internal const string Name = "Residents Eat with You";
        internal const string Version = "1.0.0.0";
    }

    [BepInPlugin(GUID: ModInfo.Guid, Name: ModInfo.Name, Version: ModInfo.Version)]
    internal class ResidentsEatWithYou : BaseUnityPlugin
    {
        internal static ResidentsEatWithYou Instance { get; private set; }

        private void Awake()
        {
            Instance = this;
            
            Harmony.CreateAndPatchAll(type: typeof(Patcher), harmonyInstanceId: ModInfo.Guid);
        }

        internal static void Log(object payload)
        {
            Instance?.Logger.LogInfo(data: payload);
        }
    }
}