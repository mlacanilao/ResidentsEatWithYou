using HarmonyLib;

namespace ResidentsEatWithYou
{
    public class Patcher
    {
        [HarmonyPrefix]
        [HarmonyPatch(declaringType: typeof(AIAct), methodName: nameof(AIAct.OnSuccess))]
        public static bool AIActOnSuccess(AIAct __instance)
        {
            return AIActPatch.OnSuccessPrefix(__instance: __instance);
        }
    }
}