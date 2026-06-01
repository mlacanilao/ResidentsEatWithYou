using System.Collections.Generic;
using UnityEngine;

namespace ResidentsEatWithYou.Patches;

internal static class AIEatPatch
{
    private static readonly HashSet<AI_Eat> InvitedMealActs = new HashSet<AI_Eat>();

    internal static void OnStartPostfix(AI_Eat __instance)
    {
        Chara? owner = __instance.owner;
        Zone? zone = owner?.currentZone;
        Map? map = zone?.map;
        if (owner == null ||
            zone == null ||
            map == null ||
            owner.pos == null ||
            ShouldInviteResidents(eatAct: __instance, owner: owner, zone: zone) == false)
        {
            return;
        }

        TraitSpotDining? diningSpot = GetDiningSpotAtPoint(map: map, point: owner.pos);
        if (diningSpot == null)
        {
            return;
        }

        FeatureTestLog.Log(
            feature: "Shared Meal Trigger",
            detail:
                "PC started eating at dining spot; " +
                FeatureTestLog.FormatChara(chara: owner) +
                ", " +
                FeatureTestLog.FormatPoint(point: owner.pos) +
                ", diningSpot=" +
                FeatureTestLog.FormatCard(card: diningSpot.owner));

        foreach (Chara? chara in zone.branch.members)
        {
            if (chara == null)
            {
                FeatureTestLog.Log(
                    feature: "Meal Invitation Guard",
                    detail: "skipped; reason=null-branch-member; " + FeatureTestLog.FormatChara(chara: chara));
                continue;
            }

            if (chara.memberType != FactionMemberType.Default)
            {
                continue;
            }

            TryStartResidentMeal(chara: chara, zone: zone, diningSpot: diningSpot);
        }

        foreach (Chara chara in map.charas)
        {
            if (chara.memberType != FactionMemberType.Guest &&
                chara.memberType != FactionMemberType.Livestock)
            {
                continue;
            }

            TryStartResidentMeal(chara: chara, zone: zone, diningSpot: diningSpot);
        }
    }

    private static bool ShouldInviteResidents(AI_Eat eatAct, Chara owner, Zone zone)
    {
        if (EClass.core?.IsGameStarted != true ||
            zone.IsPCFaction == false ||
            zone != EClass._zone ||
            zone.branch == null ||
            owner.IsPC == false ||
            HasValidPlayerMeal(eatAct: eatAct, owner: owner) == false)
        {
            return false;
        }

        return true;
    }

    private static TraitSpotDining? GetDiningSpotAtPoint(Map map, Point point)
    {
        foreach (TraitSpotDining spot in map.props.installed.traits.List<TraitSpotDining>())
        {
            foreach (Point spotPoint in spot.ListPoints())
            {
                if (point.Equals(obj: spotPoint) == true)
                {
                    return spot;
                }
            }
        }

        return null;
    }

    private static bool HasValidPlayerMeal(AI_Eat eatAct, Chara owner)
    {
        if (eatAct.target != null &&
            eatAct.IsValidTarget(c: eatAct.target) == true)
        {
            return true;
        }

        if (owner.held != null &&
            eatAct.IsValidTarget(c: owner.held) == true)
        {
            return true;
        }

        return false;
    }

    private static void TryStartResidentMeal(Chara chara, Zone zone, TraitSpotDining diningSpot)
    {
        if (CanJoinMeal(chara: chara, reason: out string reason) == false)
        {
            FeatureTestLog.Log(
                feature: "Meal Invitation Guard",
                detail: "skipped; reason=" + reason + "; " + FeatureTestLog.FormatChara(chara: chara));
            return;
        }

        Thing? thing = FindMeal(chara: chara, zone: zone, mealSource: out string mealSource);
        if (thing == null)
        {
            FeatureTestLog.Log(
                feature: "Meal Source",
                detail: "no meal found; " + FeatureTestLog.FormatChara(chara: chara));
            return;
        }

        if (chara.memberType == FactionMemberType.Livestock)
        {
            FeatureTestLog.Log(
                feature: "Livestock Meal Invitation",
                detail:
                    "source=" +
                    mealSource +
                    "; " +
                    FeatureTestLog.FormatChara(chara: chara) +
                    "; " +
                    FeatureTestLog.FormatThing(thing: thing));
            chara.SetAIImmediate(g: CreateEatAct(thing: thing, diningSpot: diningSpot, mealZone: zone));
            return;
        }

        string feature = chara.memberType == FactionMemberType.Guest ? "Guest Meal Invitation" : "Resident Meal Invitation";
        FeatureTestLog.Log(
            feature: feature,
            detail:
                "source=" +
                mealSource +
                "; " +
                FeatureTestLog.FormatChara(chara: chara) +
                "; " +
                FeatureTestLog.FormatThing(thing: thing));
        chara.SetAIImmediate(g: CreateEatAct(thing: thing, diningSpot: diningSpot, mealZone: zone));
    }

    private static bool CanJoinMeal(Chara chara, out string reason)
    {
        bool enableGuestsEatWithYou = ResidentsEatWithYouConfig.EnableGuestsEatWithYou.Value;
        List<string> selectedLivestockIds = ResidentsEatWithYouConfig.SelectedLivestockIds;

        if (chara.IsPC == true)
        {
            reason = "pc";
            return false;
        }

        if (chara.IsAliveInCurrentZone == false)
        {
            reason = "not-alive-in-current-zone";
            return false;
        }

        if (chara.IsDeadOrSleeping == true)
        {
            reason = "dead-or-sleeping";
            return false;
        }

        if (chara.IsDisabled == true)
        {
            reason = "disabled";
            return false;
        }

        if (chara.IsInCombat == true)
        {
            reason = "in-combat";
            return false;
        }

        if (chara.HasHost == true)
        {
            reason = "has-host";
            return false;
        }

        if (chara.isRestrained == true)
        {
            reason = "restrained";
            return false;
        }

        if (chara.noMove == true)
        {
            reason = "no-move";
            return false;
        }

        if (chara.IsPCParty == true)
        {
            reason = "pc-party";
            return false;
        }

        if (chara.ai?.GetChild<AI_Eat>() != null)
        {
            reason = "already-eating";
            return false;
        }

        if (chara.memberType == FactionMemberType.Default)
        {
            reason = chara.IsPCFaction == true ? string.Empty : "resident-not-pc-faction";
            return chara.IsPCFaction;
        }

        if (chara.memberType == FactionMemberType.Guest)
        {
            reason = enableGuestsEatWithYou == true ? string.Empty : "guest-disabled";
            return enableGuestsEatWithYou;
        }

        if (chara.memberType == FactionMemberType.Livestock)
        {
            if (chara.IsPCFaction == false)
            {
                reason = "livestock-not-pc-faction";
                return false;
            }

            if (selectedLivestockIds.Contains(item: chara.id) == false)
            {
                reason = "livestock-not-selected";
                return false;
            }

            reason = string.Empty;
            return true;
        }

        reason = "unsupported-member-type-" + chara.memberType.ToString();
        return false;
    }

    private static Thing? FindMeal(Chara chara, Zone zone, out string mealSource)
    {
        mealSource = "none";
        Thing? thing;
        if (chara.IsPCFaction == true)
        {
            thing = chara.FindBestFoodToEat();
        }
        else
        {
            thing = chara.things?.Find(func: (Thing a) => chara.CanEat(t: a, shouldEat: false) && a.c_isImportant == false, recursive: false);
        }

        if (thing != null)
        {
            mealSource = "inventory-should-eat";
            return thing;
        }

        if (chara.IsPCFaction == true)
        {
            thing = chara.things?.Find(func: (Thing a) => chara.CanEat(t: a, shouldEat: false) && a.c_isImportant == false, recursive: true);
            if (thing != null)
            {
                mealSource = "inventory-can-eat";
                return thing;
            }
        }

        if (chara.IsPCFaction == true &&
            zone.IsPCFaction == true)
        {
            thing = zone.branch.GetMeal(c: chara);
            if (thing != null)
            {
                thing = chara.Pick(t: thing, msg: false, tryStack: true);
                if (thing == null ||
                    thing.isDestroyed == true ||
                    thing.trait.CanEat(c: chara) == false)
                {
                    mealSource = "none";
                    return null;
                }

                mealSource = "branch-meal";
                return thing;
            }
        }

        if (chara.IsPCParty == false)
        {
            thing = CreateFallbackMeal(chara: chara);
            if (thing != null)
            {
                mealSource = "generated-fallback";
            }
        }

        return thing;
    }

    private static Thing? CreateFallbackMeal(Chara chara)
    {
        if (chara.things == null ||
            chara.things.IsFull(y: 0) == true)
        {
            return null;
        }

        Thing thing = ThingGen.CreateFromCategory(idCat: "food", lv: EClass.rnd(a: EClass.rnd(a: 60) + 1) + 10);
        thing.isNPCProperty = true;

        if (thing.trait.CanEat(c: chara) == false)
        {
            FeatureTestLog.Log(
                feature: "Meal Source",
                detail: "generated fallback rejected; reason=cannot-eat; " + FeatureTestLog.FormatChara(chara: chara) + "; " + FeatureTestLog.FormatThing(thing: thing));
            return null;
        }

        if (thing.ChildrenAndSelfWeight >= 5000 &&
            chara.IsPCParty == true)
        {
            FeatureTestLog.Log(
                feature: "Meal Source",
                detail: "generated fallback rejected; reason=too-heavy-for-party; " + FeatureTestLog.FormatChara(chara: chara) + "; " + FeatureTestLog.FormatThing(thing: thing));
            return null;
        }

        Thing? addedThing = chara.AddThing(t: thing, tryStack: true, destInvX: -1, destInvY: -1);
        if (addedThing != null)
        {
            FeatureTestLog.Log(
                feature: "Meal Source",
                detail: "generated fallback created; " + FeatureTestLog.FormatChara(chara: chara) + "; " + FeatureTestLog.FormatThing(thing: addedThing));
        }

        return addedThing;
    }

    private static AI_Eat CreateEatAct(Thing thing, TraitSpotDining diningSpot, Zone mealZone)
    {
        AI_Eat eatAct = new AI_EatAtDiningSpot(meal: thing, diningSpot: diningSpot, mealZone: mealZone);

        InvitedMealActs.Add(item: eatAct);
        return eatAct;
    }

    internal static bool TryConsumeInvitedEatAct(AI_Eat eatAct)
    {
        if (InvitedMealActs.Contains(item: eatAct) == false)
        {
            return false;
        }

        InvitedMealActs.Remove(item: eatAct);
        return true;
    }

    internal static bool ClearInvitedEatAct(AI_Eat eatAct)
    {
        return InvitedMealActs.Remove(item: eatAct);
    }

    private sealed class AI_EatAtDiningSpot : AI_Eat
    {
        private readonly TraitSpotDining diningSpot;
        private readonly Zone mealZone;

        internal AI_EatAtDiningSpot(Thing meal, TraitSpotDining diningSpot, Zone mealZone)
        {
            target = meal;
            this.diningSpot = diningSpot;
            this.mealZone = mealZone;
        }

        public override IEnumerable<Status> Run()
        {
            FeatureTestLog.Log(
                feature: "Invited Meal Route",
                detail: "started; " + FeatureTestLog.FormatChara(chara: owner) + "; " + FeatureTestLog.FormatCard(card: diningSpot.owner));
            if (target != null &&
                (target.GetRootCard() == owner || target.parent == null))
            {
                owner.HoldCard(target, 1);
            }
            else if (target != null)
            {
                yield return DoGrab(target, 1);
            }
            else
            {
                if (IsValidTarget(c: owner.held) == false)
                {
                    yield return DoGrab<TraitFood>();
                    if (IsValidTarget(c: owner.held) == false)
                    {
                        yield return Cancel();
                    }
                }

                if (cook == true)
                {
                    yield return Do(_seq: new AI_Cook(), _onChildFail: KeepRunning);
                    if (IsValidTarget(c: owner.held) == false)
                    {
                        yield return Cancel();
                    }

                    yield return DoGotoSpot<TraitHearth>(KeepRunning);
                }
            }

            target = owner.held;
            if (target == null)
            {
                yield return Cancel();
                yield break;
            }

            if ((mealZone.IsPCFaction == true || EClass.rnd(a: 4) != 0) &&
                owner.IsPCParty == false &&
                owner.noMove == false)
            {
                if (IsDiningSpotAvailable() == false)
                {
                    yield return Cancel();
                    yield break;
                }

                Point diningPoint = diningSpot.GetRandomPoint(accessChara: owner);
                yield return DoGoto(pos: diningPoint, _onChildFail: KeepRunning);
            }

            int num;
            if (target.SelfWeight < 100)
            {
                num = 1;
            }
            else
            {
                num = 2 + (int)Mathf.Sqrt(f: target.SelfWeight * 2 / 3);
            }
            int turn = 0;
            bool isFeastFood = mealZone.HasField(10001) == true && target.GetBool(id: 128) == true;
            num = num * 100 / (100 + owner.Evalue(ele: 1663) * 100);
            if (num < 1)
            {
                num = 1;
            }

            Progress_Custom seq = new Progress_Custom
            {
                cancelWhenMoved = false,
                canProgress = () => IsValidTarget(c: target) == true && owner.held == target,
                onProgressBegin = delegate
                {
                    owner.Say("eat_start", owner, target.GetName(style: NameStyle.Full, num: 1));
                    owner.PlaySound(id: "eat");
                },
                onProgress = delegate(Progress_Custom p)
                {
                    target.PlayAnime(id: AnimeID.Eat);
                    if (turn == 1 &&
                        owner.IsPC == true &&
                        owner.hunger.GetPhase() == 0 &&
                        EClass.debug.godFood == false &&
                        isFeastFood == false)
                    {
                        owner.Say("eat_full");
                        p.Cancel();
                    }

                    if (turn == 1)
                    {
                        foreach (Element value in target.elements.dict.Values)
                        {
                            if (value.source.foodEffect.IsEmpty() == false)
                            {
                                string[] foodEffect = value.source.foodEffect;
                                if (foodEffect[0] == "poison" ||
                                    foodEffect[0] == "love")
                                {
                                    owner.Talk("eatWeird");
                                    break;
                                }
                            }
                        }

                        CardRow refCard = target.refCard;
                        if (refCard != null &&
                            refCard.id == "mammoth")
                        {
                            EClass.player.forceTalk = true;
                            owner.Talk("eatammoth");
                        }
                    }

                    turn++;
                },
                onProgressComplete = delegate
                {
                    if (owner.IsPC == true &&
                        owner.hunger.GetPhase() == 0 &&
                        EClass.debug.godFood == false &&
                        isFeastFood == false)
                    {
                        owner.Say("eat_full");
                    }
                    else
                    {
                        owner.Say("eat_end", owner, target.GetName(style: NameStyle.Full, num: 1));
                        owner.ShowEmo(Emo.happy);
                        FoodEffect.Proc(owner, target.Thing);
                    }
                }
            }.SetDuration(num, 5);
            yield return Do(_seq: seq);
        }

        private bool IsDiningSpotAvailable()
        {
            Card? spotOwner = diningSpot.owner;
            if (mealZone != EClass._zone ||
                spotOwner == null ||
                spotOwner.isDestroyed == true ||
                spotOwner.ExistsOnMap == false ||
                spotOwner.pos == null)
            {
                return false;
            }

            return true;
        }
    }
}
