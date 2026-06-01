using HarmonyLib;
using ResidentsEatWithYou.Patches;

namespace ResidentsEatWithYou;

internal static class Patcher
{
    [HarmonyPostfix]
    [HarmonyPatch(declaringType: typeof(AI_Eat), methodName: nameof(AI_Eat.OnStart))]
    internal static void AI_EatOnStart(AI_Eat __instance)
    {
        AIEatPatch.OnStartPostfix(__instance: __instance);
    }

    [HarmonyPrefix]
    [HarmonyPatch(declaringType: typeof(AIAct), methodName: nameof(AIAct.OnSuccess))]
    internal static bool AIActOnSuccess(AIAct __instance)
    {
        return AIActPatch.OnSuccessPrefix(__instance: __instance);
    }

    [HarmonyPrefix]
    [HarmonyPatch(declaringType: typeof(AIAct), methodName: nameof(AIAct.Reset))]
    internal static void AIActReset(AIAct __instance)
    {
        AIActPatch.ResetPrefix(__instance: __instance);
    }
}
