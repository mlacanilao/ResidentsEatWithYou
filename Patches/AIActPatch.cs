using System.Collections.Generic;

namespace ResidentsEatWithYou
{
    internal class AIActPatch
    {
        internal static bool OnSuccessPrefix(AIAct __instance)
        {
            if (EClass.core?.IsGameStarted == false ||
                EClass._zone?.IsPCFaction == false ||
                EClass._zone?.branch == null ||
                __instance.owner?.IsInSpot<TraitSpotDining>() == false)
            {
                return true;
            }

            if (__instance is AI_Eat == true &&
                __instance.owner?.IsPC == false)
            {
                __instance.owner?.ModAffinity(c: EClass.pc, a: 1, show: true, showOnlyEmo: false);
            }

            if (__instance is AI_Eat == false ||
                __instance.owner?.IsPC == false)
            {
                return true;
            }
            
            foreach (Chara chara in EClass._zone?.branch?.members)
            {
                if (chara.memberType == FactionMemberType.Livestock ||
                    chara.IsPC == true)
                {
                    continue;
                }
                
                Thing thing = chara.things?.Find(func: (Thing a) => chara.CanEat(t: a, shouldEat: chara.IsPCFaction) && !a.c_isImportant, recursive: false);
                
                if (thing == null && chara.IsPCFaction)
                {
                    thing = chara.things?.Find(func: (Thing a) => chara.CanEat(t: a, shouldEat: false) && !a.c_isImportant, recursive: false);
                }
                if (thing == null && chara.IsPCFaction && EClass._zone.IsPCFaction)
                {
                    thing = EClass._zone.branch.GetMeal(c: chara);
                    if (thing != null)
                    {
                        chara.Pick(t: thing, msg: true, tryStack: true);
                    }
                }
                if (thing == null && !chara.IsPCParty)
                {
                    if (!chara.IsPCFaction && EClass.rnd(a: 8) != 0)
                    {
                        chara.hunger.Mod(a: -30);
                    }
                    else if (!chara.things.IsFull(y: 0))
                    {
                        thing = ThingGen.CreateFromCategory(idCat: "food", lv: EClass.rnd(a: EClass.rnd(a: 60) + 1) + 10);
                        thing.isNPCProperty = true;
                        if ((thing.ChildrenAndSelfWeight < 5000 || !chara.IsPCParty) && thing.trait.CanEat(c: chara))
                        {
                            thing = chara.AddThing(t: thing, tryStack: true, destInvX: -1, destInvY: -1);
                        }
                    }
                }

                if (thing != null)
                {
                    chara.TryMoveTowards(EClass.pc?.pos);

                    chara.SetAIImmediate(g: new AI_Eat
                    {
                        target = thing
                    });
                }
            }

            return false;
        }
    }
}