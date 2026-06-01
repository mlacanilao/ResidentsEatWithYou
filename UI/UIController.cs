using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using EvilMask.Elin.ModOptions;
using EvilMask.Elin.ModOptions.UI;
using UnityEngine.UI;

namespace ResidentsEatWithYou.UI;

internal static class UIController
{
    private const string EnableGuestsEatWithYouToggleId = "enableGuestsEatWithYouToggle";

    public static void RegisterUI()
    {
        ModOptionController controller = ModOptionController.Register(guid: ModInfo.Guid, tooptipId: "mod.tooltip");
        if (controller == null)
        {
            ResidentsEatWithYou.LogError(message: "Failed to register Mod Options controller.");
            return;
        }

        string assemblyLocation = Path.GetDirectoryName(path: Assembly.GetExecutingAssembly().Location) ?? string.Empty;
        string xmlPath = Path.Combine(path1: assemblyLocation, path2: "ResidentsEatWithYouConfig.xml");
        string xlsxPath = Path.Combine(path1: assemblyLocation, path2: "translations.xlsx");

        ResidentsEatWithYouConfig.InitializeXmlPath(xmlPath: xmlPath);
        ResidentsEatWithYouConfig.InitializeTranslationXlsxPath(xlsxPath: xlsxPath);

        if (File.Exists(path: ResidentsEatWithYouConfig.XmlPath))
        {
            controller.SetPreBuildWithXml(xml: File.ReadAllText(path: ResidentsEatWithYouConfig.XmlPath));
            FeatureTestLog.Log(
                feature: "Mod Options Assets",
                detail: "loaded XML; path=" + ResidentsEatWithYouConfig.XmlPath);
        }
        else
        {
            ResidentsEatWithYou.LogError(message: $"Mod Options XML not found: {xmlPath}");
            FeatureTestLog.Log(
                feature: "Mod Options Assets",
                detail: "missing XML; path=" + xmlPath);
        }

        if (File.Exists(path: ResidentsEatWithYouConfig.TranslationXlsxPath))
        {
            controller.SetTranslationsFromXslx(path: ResidentsEatWithYouConfig.TranslationXlsxPath);
            FeatureTestLog.Log(
                feature: "Mod Options Assets",
                detail: "loaded translations; path=" + ResidentsEatWithYouConfig.TranslationXlsxPath);
        }
        else
        {
            ResidentsEatWithYou.LogError(message: $"Mod Options translations not found: {xlsxPath}");
            FeatureTestLog.Log(
                feature: "Mod Options Assets",
                detail: "missing translations; path=" + xlsxPath);
        }

        RegisterEvents(controller: controller);
    }

    private static void RegisterEvents(ModOptionController controller)
    {
        controller.OnBuildUI += builder =>
        {
            BindEnableGuestsEatWithYouToggle(
                toggle: GetRequiredPreBuild<OptToggle>(builder: builder, id: EnableGuestsEatWithYouToggleId));
            if (LivestockPickerUI.Build(controller: controller, builder: builder) == false)
            {
                return;
            }
        };
    }

    private static void BindEnableGuestsEatWithYouToggle(OptToggle? toggle)
    {
        if (toggle == null)
        {
            return;
        }

        toggle.Checked = ResidentsEatWithYouConfig.EnableGuestsEatWithYou.Value;
        toggle.OnValueChanged += isChecked =>
        {
            ResidentsEatWithYouConfig.EnableGuestsEatWithYou.Value = isChecked;
        };
    }

    internal static void SetDropdownOptions(OptDropdown? dropdown, IEnumerable<string> texts, int selectedIndex = 0)
    {
        if (dropdown?.Base == null)
        {
            return;
        }

        List<string> optionTexts = texts.ToList();
        dropdown.Base.options.Clear();
        foreach (string text in optionTexts)
        {
            dropdown.Base.options.Add(item: new Dropdown.OptionData(text: text));
        }

        if (optionTexts.Count == 0)
        {
            dropdown.Value = 0;
        }
        else
        {
            dropdown.Value = Math.Max(val1: 0, val2: Math.Min(val1: selectedIndex, val2: optionTexts.Count - 1));
        }

        dropdown.Base.RefreshShownValue();
    }

    internal static T? GetRequiredPreBuild<T>(OptionUIBuilder builder, string id) where T : OptUIElement
    {
        T? element = builder.GetPreBuild<T>(id: id);
        if (element == null)
        {
            ResidentsEatWithYou.LogError(message: $"Missing Mod Options prebuilt element: {id}");
            FeatureTestLog.Log(
                feature: "Mod Options Binding",
                detail: "missing prebuilt element; id=" + id);
        }

        return element;
    }
}
