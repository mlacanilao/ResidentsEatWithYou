namespace ResidentsEatWithYou.Patches;

internal static class AIActPatch
{
    internal static bool OnSuccessPrefix(AIAct __instance)
    {
        if (EClass.core?.IsGameStarted != true ||
            EClass.pc == null)
        {
            return true;
        }

        Chara? owner = __instance.owner;
        if (owner == null)
        {
            return true;
        }

        if (__instance is AI_Eat eatAct &&
            AIEatPatch.TryConsumeInvitedEatAct(eatAct: eatAct) == true &&
            owner.IsPC == false &&
            owner.IsInSpot<TraitSpotDining>() == true)
        {
            FeatureTestLog.Log(
                feature: "Shared Meal Affinity",
                detail: "granting affinity; " + FeatureTestLog.FormatChara(chara: owner));
            owner.ModAffinity(c: EClass.pc, a: 1, show: true, showOnlyEmo: false);
        }

        return true;
    }

    internal static void ResetPrefix(AIAct __instance)
    {
        if (__instance is AI_Eat eatAct)
        {
            if (AIEatPatch.ClearInvitedEatAct(eatAct: eatAct) == false)
            {
                return;
            }

            FeatureTestLog.Log(
                feature: "Invited Meal Cleanup",
                detail: "reset received; " + FeatureTestLog.FormatChara(chara: eatAct.owner) + "; " + FeatureTestLog.FormatCard(card: eatAct.target));
        }
    }
}
