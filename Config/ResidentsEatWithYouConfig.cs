using System.Collections.Generic;
using System.IO;
using System.Linq;
using BepInEx.Configuration;

namespace ResidentsEatWithYou;

internal static class ResidentsEatWithYouConfig
{
    internal static ConfigEntry<bool> EnableGuestsEatWithYou = null!;
    private static ConfigEntry<string> _selectedLivestockIdsEntry = null!;

    internal static string XmlPath { get; private set; } = string.Empty;
    internal static string TranslationXlsxPath { get; private set; } = string.Empty;

    internal static List<string> SelectedLivestockIds
    {
        get
        {
            if (_selectedLivestockIdsEntry != null)
            {
                return _selectedLivestockIdsEntry.Value
                    .Split(separator: ',')
                    .Select(selector: id => id.Trim())
                    .Where(predicate: id => string.IsNullOrEmpty(value: id) == false)
                    .ToList();
            }

            return new List<string>();
        }
    }

    internal static void UpdateSelectedLivestockIds(List<string> selectedLivestockIds)
    {
        if (_selectedLivestockIdsEntry != null)
        {
            _selectedLivestockIdsEntry.Value = string.Join(separator: ",", values: selectedLivestockIds);
        }
    }

    internal static void LoadConfig(ConfigFile config)
    {
        EnableGuestsEatWithYou = config.Bind(
            section: ModInfo.Name,
            key: "Enable Guests Eat With You",
            defaultValue: false,
            description: "Enable or disable whether guests can join shared meals when you start eating inside a dining spot area.\n" +
                         "Set to 'true' to allow guests to join those meals, or 'false' to disable guest participation.\n" +
                         "食堂の立札で指定された範囲内で食事を始めたとき、ゲストが一緒に食事に参加できるかを設定します。\n" +
                         "'true' に設定するとゲストがその食事に参加でき、'false' に設定するとゲストの参加を無効にします。\n" +
                         "设置当您在食堂标牌指定范围内开始用餐时，客人是否可以一起参加用餐。\n" +
                         "设置为 'true' 允许客人参加这些用餐，设置为 'false' 禁用客人参加。"
        );

        _selectedLivestockIdsEntry = config.Bind(
            section: ModInfo.Name,
            key: "Selected Livestock IDs",
            defaultValue: string.Empty,
            description: "Comma-separated list of your faction livestock IDs allowed to join shared meals inside a dining spot area. Leave empty to disable livestock participation.\n" +
                         "食堂の立札で指定された範囲内で一緒に食事へ参加できるプレイヤー勢力の家畜IDのカンマ区切りリストです。空欄の場合、家畜は参加しません。\n" +
                         "允许在食堂标牌指定范围内参加共同用餐的己方家畜ID的逗号分隔列表。留空时家畜不会参加。"
        );
    }

    internal static void InitializeXmlPath(string xmlPath)
    {
        if (File.Exists(path: xmlPath))
        {
            XmlPath = xmlPath;
        }
        else
        {
            XmlPath = string.Empty;
        }
    }

    internal static void InitializeTranslationXlsxPath(string xlsxPath)
    {
        if (File.Exists(path: xlsxPath))
        {
            TranslationXlsxPath = xlsxPath;
        }
        else
        {
            TranslationXlsxPath = string.Empty;
        }
    }
}
