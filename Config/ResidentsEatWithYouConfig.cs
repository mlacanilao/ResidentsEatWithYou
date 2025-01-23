using System.Collections.Generic;
using System.IO;
using System.Linq;
using BepInEx.Configuration;

namespace ResidentsEatWithYou
{
    internal static class ResidentsEatWithYouConfig
    {
        internal static ConfigEntry<bool> EnableGuestsEatWithYou;
        private static ConfigEntry<string> _selectedLivestockIdsEntry;
        
        public static string XmlPath { get; private set; }
        public static string TranslationXlsxPath { get; private set; }
        
        internal static List<string> SelectedLivestockIds =>
            _selectedLivestockIdsEntry?.Value.Split(separator: ',')
                .Select(selector: id => id.Trim())
                .Where(predicate: id => !string.IsNullOrEmpty(value: id))
                .ToList() ?? new List<string>();
        
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
                description: "Enable or disable the ability for guests to eat with you when you start eating.\n" +
                             "Set to 'true' to allow guests to join meals, or 'false' to disable it.\n" +
                             "ゲストが食事を開始すると一緒に食事をする機能を有効または無効にします。\n" +
                             "'true' に設定するとゲストが食事に参加でき、'false' に設定すると無効になります。\n" +
                             "启用或禁用客人在您开始用餐时一起用餐的功能。\n" +
                             "设置为 'true' 允许客人参加用餐，设置为 'false' 禁用此功能。"
            );
            
            _selectedLivestockIdsEntry = config.Bind(
                section: ModInfo.Name,
                key: "Selected Livestock IDs",
                defaultValue: string.Empty,
                description: "Comma-separated list of livestock IDs allowed to eat.\n" +
                             "食事が可能な家畜IDのカンマ区切りリストです。\n" +
                             "允许进食的家畜ID的逗号分隔列表。"
            );
        }
        
        public static void InitializeXmlPath(string xmlPath)
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
        
        public static void InitializeTranslationXlsxPath(string xlsxPath)
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
}
