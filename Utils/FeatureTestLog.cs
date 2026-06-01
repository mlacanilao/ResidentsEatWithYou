using System.Collections.Generic;

namespace ResidentsEatWithYou;

internal static class FeatureTestLog
{
    private static readonly HashSet<string> LoggedOnceKeys = new HashSet<string>();

    internal static void Log(string feature, string detail)
    {
        ResidentsEatWithYou.LogDebug(message: "[FeatureTest] " + feature + ": " + detail);
    }

    internal static void LogOnce(string feature, string key, string detail)
    {
        string combinedKey = feature + "|" + key;
        if (LoggedOnceKeys.Add(item: combinedKey) == false)
        {
            return;
        }

        Log(feature: feature, detail: detail);
    }

    internal static string FormatChara(Chara? chara)
    {
        if (chara == null)
        {
            return "chara=<null>";
        }

        return "charaId=" +
               (chara.id ?? "<empty>") +
               ", name=" +
               chara.NameSimple +
               ", memberType=" +
               chara.memberType.ToString() +
               ", isPC=" +
               chara.IsPC.ToString();
    }

    internal static string FormatThing(Thing? thing)
    {
        if (thing == null)
        {
            return "thing=<null>";
        }

        return "thingId=" +
               (thing.id ?? "<empty>") +
               ", name=" +
               thing.NameSimple +
               ", count=" +
               thing.Num.ToString();
    }

    internal static string FormatCard(Card? card)
    {
        if (card == null)
        {
            return "card=<null>";
        }

        return "cardId=" +
               (card.id ?? "<empty>") +
               ", name=" +
               card.NameSimple;
    }

    internal static string FormatPoint(Point? point)
    {
        if (point == null)
        {
            return "point=<null>";
        }

        return "x=" + point.x.ToString() + ", z=" + point.z.ToString();
    }
}
