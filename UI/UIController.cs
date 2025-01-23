using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using BepInEx;
using EvilMask.Elin.ModOptions;
using EvilMask.Elin.ModOptions.UI;
using UnityEngine;

namespace ResidentsEatWithYou
{
    public class UIController
    {
        private static Dictionary<string, string> targetMap = new Dictionary<string, string>();
        
        public static void RegisterUI()
        {
            foreach (var obj in ModManager.ListPluginObject)
            {
                if (obj is BaseUnityPlugin plugin && plugin.Info.Metadata.GUID == ModInfo.ModOptionsGuid)
                {
                    var controller = ModOptionController.Register(guid: ModInfo.Guid, tooptipId: "mod.tooltip");
                    
                    var assemblyLocation = Path.GetDirectoryName(path: Assembly.GetExecutingAssembly().Location);
                    var xmlPath = Path.Combine(path1: assemblyLocation, path2: "ResidentsEatWithYouConfig.xml");
                    ResidentsEatWithYouConfig.InitializeXmlPath(xmlPath: xmlPath);
            
                    var xlsxPath = Path.Combine(path1: assemblyLocation, path2: "translations.xlsx");
                    ResidentsEatWithYouConfig.InitializeTranslationXlsxPath(xlsxPath: xlsxPath);
                    
                    if (File.Exists(path: ResidentsEatWithYouConfig.XmlPath))
                    {
                        using (StreamReader sr = new StreamReader(path: ResidentsEatWithYouConfig.XmlPath))
                            controller.SetPreBuildWithXml(xml: sr.ReadToEnd());
                    }
                    
                    if (File.Exists(path: ResidentsEatWithYouConfig.TranslationXlsxPath))
                    {
                        controller.SetTranslationsFromXslx(path: ResidentsEatWithYouConfig.TranslationXlsxPath);
                    }
                    
                    RegisterEvents(controller: controller);
                }
            }
        }

        private static void RegisterEvents(ModOptionController controller)
        {
            controller.OnBuildUI += builder =>
            {
                var enableGuestsEatWithYouToggle = builder.GetPreBuild<OptToggle>(id: "enableGuestsEatWithYouToggle");
                enableGuestsEatWithYouToggle.Checked = ResidentsEatWithYouConfig.EnableGuestsEatWithYou.Value;
                enableGuestsEatWithYouToggle.OnValueChanged += isChecked =>
                {
                    ResidentsEatWithYouConfig.EnableGuestsEatWithYou.Value = isChecked;
                };
                
                var dropdown01 = PopulateDropdownWithSelectedLivestockIds(builder: builder, dropdownId: "dropdown01");
                var button01 = builder.GetPreBuild<OptButton>(id: "button01");
                button01.OnClicked += () =>
                {
                    ResidentsEatWithYouConfig.UpdateSelectedLivestockIds(selectedLivestockIds: new List<string>());

                    if (dropdown01 != null)
                    {
                        dropdown01.Base.options.Clear();
                        dropdown01.Base.RefreshShownValue();
                    }
                };
                
                var dropdown02 = PopulateDropdown(
                    builder: builder,
                    dropdownId: "dropdown02",
                    targetMap: targetMap);
                
                var button02 = builder.GetPreBuild<OptButton>(id: "button02");
                ConfigureButton(builder: builder, button: button02, dropdown: dropdown02, targetMap: targetMap);
            };
        }
        
        private static OptDropdown PopulateDropdownWithSelectedLivestockIds(OptionUIBuilder builder, string dropdownId)
        {
            try
            {
                var dropdown = builder.GetPreBuild<OptDropdown>(id: dropdownId);

                if (dropdown == null)
                {
                    ResidentsEatWithYou.Log(payload: $"Dropdown with ID '{dropdownId}' not found.");
                    return null;
                }

                var selectedLivestockIds = ResidentsEatWithYouConfig.SelectedLivestockIds;

                dropdown.Base.options.Clear();

                foreach (var livestockId in selectedLivestockIds)
                {
                    dropdown.Base.options.Add(item: new UnityEngine.UI.Dropdown.OptionData(text: livestockId));
                }

                dropdown.Base.RefreshShownValue();
                return dropdown;
            }
            catch (Exception ex)
            {
                ResidentsEatWithYou.Log(payload: $"Error populating dropdown '{dropdownId}': {ex.Message}");
                return null;
            }
        }
        
        private static OptDropdown PopulateDropdown(OptionUIBuilder builder, string dropdownId, 
            Dictionary<string, string> targetMap)
        {
            try
            {
                var dropdown = builder.GetPreBuild<OptDropdown>(id: dropdownId);

                if (dropdown == null)
                {
                    ResidentsEatWithYou.Log(payload: $"Dropdown with ID '{dropdownId}' not found.");
                    return null;
                }

                // Clear the targetMap and existing options to prevent duplicates
                targetMap.Clear();
                dropdown.Base.options.Clear();
                
                if (EClass.core?.IsGameStarted == false)
                {
                    return dropdown;
                }

                // Get all livestock in the current zone
                foreach (var chara in EClass._zone?.branch?.members ?? Enumerable.Empty<Chara>())
                {
                    if (chara.memberType == FactionMemberType.Livestock && !string.IsNullOrEmpty(chara.id))
                    {
                        string localizedText = GetLocalizedText(chara.source); // Optional helper function for localized name
                        string livestockId = chara.id;

                        if (!string.IsNullOrEmpty(localizedText) && !targetMap.ContainsKey(localizedText))
                        {
                            targetMap[localizedText] = livestockId;
                        }
                    }
                }

                // Populate dropdown with localized text as options
                foreach (var localizedName in targetMap.Keys)
                {
                    dropdown.Base.options.Add(new UnityEngine.UI.Dropdown.OptionData(text: localizedName));
                }

                dropdown.Base.RefreshShownValue();
                return dropdown;
            }
            catch (Exception ex)
            {
                ResidentsEatWithYou.Log(payload: $"Error populating dropdown '{dropdownId}' with livestock IDs: {ex.Message}");
                return null;
            }
        }
        
        private static string GetLocalizedText(SourceChara.Row row)
        {
            if (row == null)
            {
                ResidentsEatWithYou.Log(payload: "Row is null; returning null for localized text.");
                return null;
            }

            string name;
            string aka;

            switch (Lang.langCode)
            {
                case "JP":
                    name = string.IsNullOrEmpty(value: row.name_JP) || row.name_JP == "*r" ? string.Empty : row.name_JP;
                    aka = row.aka_JP ?? row.aka;
                    break;

                case "CN":
                    name = string.IsNullOrEmpty(value: row.name) || row.name == "*r" ? string.Empty : row.GetText(id: "name", returnNull: false);
                    aka = row.GetText(id: "aka", returnNull: false);
                    break;

                case "EN":
                default:
                    name = string.IsNullOrEmpty(value: row.name) || row.name == "*r" ? string.Empty : row.name;
                    aka = row.aka;
                    if (Lang.langCode != "EN")
                    {
                        ResidentsEatWithYou.Log(payload: $"Unsupported language '{Lang.langCode}'; defaulting to English.");
                    }
                    break;
            }
            
            string namePart = !string.IsNullOrEmpty(name) ? $"「{name}」" : string.Empty;
            string akaPart = aka ?? string.Empty;
            string idPart = $"({row.id})";
            string result = $"{akaPart} {namePart} {idPart}".Trim();
            return result;
        }
        
        private static void ConfigureButton(OptionUIBuilder builder, OptButton button, OptDropdown dropdown,
            Dictionary<string, string> targetMap)
        {
            if (button == null || dropdown == null)
            {
                return;
            }

            button.OnClicked += () =>
            {
                if (dropdown.Base.options.Count == 0)
                {
                    return;
                }

                int selectedIndex = dropdown.Base.value; // Get selected option index
                string selectedText = dropdown.Base.options[selectedIndex].text; // Get localized text (e.g., livestock name)

                // Check if the selected livestock is in the targetMap
                if (targetMap.TryGetValue(key: selectedText, value: out string selectedId))
                {
                    List<string> selectedLivestockIds = ResidentsEatWithYouConfig.SelectedLivestockIds;

                    // Add the livestock ID if not already in the config
                    if (!selectedLivestockIds.Contains(selectedId))
                    {
                        selectedLivestockIds.Add(selectedId);
                        ResidentsEatWithYouConfig.UpdateSelectedLivestockIds(selectedLivestockIds); // Update config
                    }
                }
                else
                {
                    ResidentsEatWithYou.Log(payload: $"Failed to find ID for selected livestock '{selectedText}'.");
                }

                // Repopulate the dropdown with updated selection
                PopulateDropdownWithSelectedLivestockIds(builder: builder, dropdownId: "dropdown01");
            };
        }
    }
}