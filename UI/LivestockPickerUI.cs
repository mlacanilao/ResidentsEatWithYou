using System;
using System.Collections.Generic;
using System.Linq;
using EvilMask.Elin.ModOptions;
using EvilMask.Elin.ModOptions.UI;

namespace ResidentsEatWithYou.UI;

internal static class LivestockPickerUI
{
    private const string SelectedLivestockDropdownId = "dropdown01";
    private const string ClearSelectedLivestockButtonId = "button01";
    private const string RemoveSelectedLivestockButtonId = "button03";
    private const string LivestockSearchInputId = "input02";
    private const string LivestockSearchPlaceholderId = "input02.placeholder";
    private const string AvailableLivestockDropdownId = "dropdown02";
    private const string AddLivestockButtonId = "button02";
    private const string NoSelectedLivestockTextId = "dropdown01.item01";
    private const string NoAvailableLivestockTextId = "dropdown02.item01";
    private const string NoMatchesTextId = "dropdown02.no_matches";
    private const int SearchResultLimit = 75;

    private const string NoSelectedLivestockFallbackText = "No livestock selected";
    private const string NoAvailableLivestockFallbackText = "No faction livestock available";
    private const string NoMatchesFallbackText = "No matches";

    private static readonly List<LivestockOption> AvailableLivestockOptions = new List<LivestockOption>();
    private static readonly List<LivestockOption> FilteredLivestockOptions = new List<LivestockOption>();
    private static string? noSelectedLivestockDisplayText;
    private static string? noAvailableLivestockDisplayText;

    internal static bool Build(ModOptionController controller, OptionUIBuilder builder)
    {
        noSelectedLivestockDisplayText = GetTranslatedTextOrNull(
            controller: controller,
            contentId: NoSelectedLivestockTextId);
        noAvailableLivestockDisplayText = GetTranslatedTextOrNull(
            controller: controller,
            contentId: NoAvailableLivestockTextId);

        OptDropdown? selectedLivestockDropdown = UIController.GetRequiredPreBuild<OptDropdown>(builder: builder, id: SelectedLivestockDropdownId);
        OptButton? addLivestockButton = UIController.GetRequiredPreBuild<OptButton>(builder: builder, id: AddLivestockButtonId);
        OptInput? livestockSearchInput = UIController.GetRequiredPreBuild<OptInput>(builder: builder, id: LivestockSearchInputId);
        OptDropdown? availableLivestockDropdown = UIController.GetRequiredPreBuild<OptDropdown>(builder: builder, id: AvailableLivestockDropdownId);
        OptButton? removeSelectedLivestockButton = UIController.GetRequiredPreBuild<OptButton>(builder: builder, id: RemoveSelectedLivestockButtonId);
        OptButton? clearSelectedLivestockButton = UIController.GetRequiredPreBuild<OptButton>(builder: builder, id: ClearSelectedLivestockButtonId);

        if (selectedLivestockDropdown == null ||
            addLivestockButton == null ||
            livestockSearchInput == null ||
            availableLivestockDropdown == null ||
            removeSelectedLivestockButton == null ||
            clearSelectedLivestockButton == null)
        {
            FeatureTestLog.Log(
                feature: "Livestock Picker UI",
                detail: "build skipped; reason=missing-required-control");
            return false;
        }

        FeatureTestLog.Log(feature: "Livestock Picker UI", detail: "build started.");

        RefreshSelectedLivestockDropdown(dropdown: selectedLivestockDropdown);
        ConfigureRemoveSelectedLivestockButton(
            button: removeSelectedLivestockButton,
            selectedDropdown: selectedLivestockDropdown);
        ConfigureClearSelectedLivestockButton(
            button: clearSelectedLivestockButton,
            selectedDropdown: selectedLivestockDropdown);

        PopulateAvailableLivestockOptions();
        ConfigureLivestockSearchInput(
            input: livestockSearchInput,
            placeholderText: GetTranslatedTextOrNull(
                controller: controller,
                contentId: LivestockSearchPlaceholderId),
            noMatchesText: GetTranslatedTextOrNull(
                controller: controller,
                contentId: NoMatchesTextId),
            dropdown: availableLivestockDropdown,
            addButton: addLivestockButton);
        ConfigureAddLivestockButton(
            button: addLivestockButton,
            dropdown: availableLivestockDropdown,
            selectedDropdown: selectedLivestockDropdown);

        return true;
    }

    private static void ConfigureClearSelectedLivestockButton(OptButton? button, OptDropdown? selectedDropdown)
    {
        if (button == null)
        {
            return;
        }

        button.OnClicked += () =>
        {
            List<string> selectedLivestockIds = ResidentsEatWithYouConfig.SelectedLivestockIds;
            FeatureTestLog.Log(
                feature: "Livestock Picker Clear",
                detail: "clearing selected IDs; previousCount=" + selectedLivestockIds.Count.ToString());
            ResidentsEatWithYouConfig.UpdateSelectedLivestockIds(selectedLivestockIds: new List<string>());
            RefreshSelectedLivestockDropdown(dropdown: selectedDropdown);
        };
    }

    private static void ConfigureRemoveSelectedLivestockButton(OptButton? button, OptDropdown? selectedDropdown)
    {
        if (button == null ||
            selectedDropdown == null)
        {
            return;
        }

        button.OnClicked += () =>
        {
            int selectedIndex = selectedDropdown.Value;
            List<string> selectedLivestockIds = ResidentsEatWithYouConfig.SelectedLivestockIds;
            if (selectedIndex < 0 ||
                selectedIndex >= selectedLivestockIds.Count)
            {
                FeatureTestLog.Log(
                    feature: "Livestock Picker Remove",
                    detail:
                        "ignored stale selection; selectedIndex=" +
                        selectedIndex.ToString() +
                        ", selectedCount=" +
                        selectedLivestockIds.Count.ToString());
                return;
            }

            string removedId = selectedLivestockIds[index: selectedIndex];
            selectedLivestockIds.RemoveAt(index: selectedIndex);
            ResidentsEatWithYouConfig.UpdateSelectedLivestockIds(selectedLivestockIds: selectedLivestockIds);
            FeatureTestLog.Log(
                feature: "Livestock Picker Remove",
                detail: "removed selected ID; id=" + removedId + ", remainingCount=" + selectedLivestockIds.Count.ToString());

            int nextSelectedIndex = 0;
            if (selectedLivestockIds.Count > 0)
            {
                nextSelectedIndex = Math.Min(val1: selectedIndex, val2: selectedLivestockIds.Count - 1);
            }

            RefreshSelectedLivestockDropdown(dropdown: selectedDropdown, selectedIndex: nextSelectedIndex);
        };
    }

    private static void ConfigureLivestockSearchInput(
        OptInput? input,
        string? placeholderText,
        string? noMatchesText,
        OptDropdown? dropdown,
        OptButton? addButton)
    {
        RefreshFilteredLivestockDropdown(
            dropdown: dropdown,
            searchText: input?.Text,
            noMatchesText: noMatchesText,
            addButton: addButton);

        if (input == null)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(value: placeholderText) == false)
        {
            input.Placeholder = placeholderText;
        }

        input.OnValueChanged += searchText =>
        {
            RefreshFilteredLivestockDropdown(
                dropdown: dropdown,
                searchText: searchText,
                noMatchesText: noMatchesText,
                addButton: addButton);
        };
    }

    private static void ConfigureAddLivestockButton(OptButton? button, OptDropdown? dropdown, OptDropdown? selectedDropdown)
    {
        if (button == null ||
            dropdown == null)
        {
            return;
        }

        button.OnClicked += () =>
        {
            int selectedIndex = dropdown.Value;
            if (selectedIndex < 0 ||
                selectedIndex >= FilteredLivestockOptions.Count)
            {
                FeatureTestLog.Log(
                    feature: "Livestock Picker Add",
                    detail:
                        "ignored stale selection; selectedIndex=" +
                        selectedIndex.ToString() +
                        ", filteredCount=" +
                        FilteredLivestockOptions.Count.ToString());
                return;
            }

            List<string> selectedLivestockIds = ResidentsEatWithYouConfig.SelectedLivestockIds;
            string selectedId = FilteredLivestockOptions[index: selectedIndex].Id;
            if (selectedLivestockIds.Contains(item: selectedId) == true)
            {
                FeatureTestLog.Log(
                    feature: "Livestock Picker Add",
                    detail: "ignored duplicate ID; id=" + selectedId);
                return;
            }

            selectedLivestockIds.Add(item: selectedId);
            ResidentsEatWithYouConfig.UpdateSelectedLivestockIds(selectedLivestockIds: selectedLivestockIds);
            FeatureTestLog.Log(
                feature: "Livestock Picker Add",
                detail: "added selected ID; id=" + selectedId + ", selectedCount=" + selectedLivestockIds.Count.ToString());
            RefreshSelectedLivestockDropdown(dropdown: selectedDropdown);
        };
    }

    private static void PopulateAvailableLivestockOptions()
    {
        AvailableLivestockOptions.Clear();

        if (EClass.core?.IsGameStarted != true)
        {
            FeatureTestLog.LogOnce(
                feature: "Livestock Picker Source",
                key: "game-not-started",
                detail: "available livestock scan skipped; reason=game-not-started");
            return;
        }

        Zone? zone = EClass._zone;
        Map? map = zone?.map;
        if (zone == null ||
            map == null)
        {
            FeatureTestLog.Log(
                feature: "Livestock Picker Source",
                detail: "available livestock scan skipped; reason=no-zone-or-map");
            return;
        }

        HashSet<string> seenLivestockIds = new HashSet<string>();
        foreach (Chara chara in map.charas)
        {
            if (chara.memberType != FactionMemberType.Livestock ||
                chara.IsPCFaction == false ||
                string.IsNullOrEmpty(value: chara.id) == true ||
                seenLivestockIds.Add(item: chara.id) == false)
            {
                continue;
            }

            AvailableLivestockOptions.Add(item: CreateLivestockOption(chara: chara));
        }

        AvailableLivestockOptions.Sort(comparison: CompareLivestockOptions);
        FeatureTestLog.Log(
            feature: "Livestock Picker Source",
            detail: "available livestock scan complete; count=" + AvailableLivestockOptions.Count.ToString());
    }

    private static LivestockOption CreateLivestockOption(Chara chara)
    {
        string id = chara.id;
        string displayName = GetLivestockDisplayName(chara: chara);
        string text = id;

        if (string.IsNullOrWhiteSpace(value: displayName) == false &&
            displayName != id)
        {
            text = $"{id} - {displayName}";
        }

        string searchText = $"{id} {displayName}";
        return new LivestockOption(id: id, text: text, searchText: searchText);
    }

    private static string GetLivestockDisplayName(Chara chara)
    {
        SourceChara.Row? row = chara.source;
        if (row != null)
        {
            string displayName = row.GetName(c: chara, full: true);
            if (string.IsNullOrWhiteSpace(value: displayName) == false)
            {
                return displayName;
            }
        }

        return chara.id;
    }

    private static string GetSelectedLivestockText(string livestockId)
    {
        if (EClass.sources?.charas?.map == null ||
            EClass.sources.charas.map.TryGetValue(key: livestockId, value: out SourceChara.Row row) == false)
        {
            return livestockId;
        }

        string displayName = row.GetName(c: null, full: true);
        if (string.IsNullOrWhiteSpace(value: displayName) == true ||
            displayName == livestockId)
        {
            return livestockId;
        }

        return $"{livestockId} - {displayName}";
    }

    private static int CompareLivestockOptions(LivestockOption left, LivestockOption right)
    {
        return string.Compare(strA: left.Text, strB: right.Text, comparisonType: StringComparison.CurrentCultureIgnoreCase);
    }

    private static void RefreshFilteredLivestockDropdown(
        OptDropdown? dropdown,
        string? searchText,
        string? noMatchesText,
        OptButton? addButton)
    {
        FilteredLivestockOptions.Clear();
        FilteredLivestockOptions.AddRange(collection: FilterLivestockOptions(searchText: searchText));

        if (addButton != null)
        {
            addButton.Enabled = FilteredLivestockOptions.Count > 0;
        }

        string fallbackText = GetNoAvailableLivestockText();
        if (AvailableLivestockOptions.Count > 0 &&
            FilteredLivestockOptions.Count == 0)
        {
            fallbackText = GetNoMatchesText(noMatchesText: noMatchesText);
        }

        IEnumerable<string> optionTexts;
        if (FilteredLivestockOptions.Count == 0)
        {
            optionTexts = new[] { fallbackText };
        }
        else
        {
            optionTexts = FilteredLivestockOptions.Select(selector: option => option.Text);
        }

        UIController.SetDropdownOptions(dropdown: dropdown, texts: optionTexts);
        FeatureTestLog.Log(
            feature: "Livestock Picker Search",
            detail:
                "refreshed; availableCount=" +
                AvailableLivestockOptions.Count.ToString() +
                ", filteredCount=" +
                FilteredLivestockOptions.Count.ToString() +
                ", hasSearch=" +
                (string.IsNullOrWhiteSpace(value: searchText) == false).ToString());
    }

    private static IEnumerable<LivestockOption> FilterLivestockOptions(string? searchText)
    {
        if (string.IsNullOrWhiteSpace(value: searchText) == true)
        {
            return AvailableLivestockOptions.Take(count: SearchResultLimit);
        }

        string normalizedSearchText = searchText!.Trim();
        return AvailableLivestockOptions
            .Where(predicate: option => option.Matches(searchText: normalizedSearchText))
            .Take(count: SearchResultLimit);
    }

    private static void RefreshSelectedLivestockDropdown(OptDropdown? dropdown, int selectedIndex = 0)
    {
        List<string> selectedLivestockIds = ResidentsEatWithYouConfig.SelectedLivestockIds;
        IEnumerable<string> optionTexts;
        if (selectedLivestockIds.Count == 0)
        {
            optionTexts = new[] { GetNoSelectedLivestockText() };
        }
        else
        {
            optionTexts = selectedLivestockIds.Select(selector: GetSelectedLivestockText);
        }

        UIController.SetDropdownOptions(dropdown: dropdown, texts: optionTexts, selectedIndex: selectedIndex);
    }

    private static string GetNoSelectedLivestockText()
    {
        if (string.IsNullOrWhiteSpace(value: noSelectedLivestockDisplayText) == false)
        {
            return noSelectedLivestockDisplayText!;
        }

        return NoSelectedLivestockFallbackText;
    }

    private static string GetNoAvailableLivestockText()
    {
        if (string.IsNullOrWhiteSpace(value: noAvailableLivestockDisplayText) == false)
        {
            return noAvailableLivestockDisplayText!;
        }

        return NoAvailableLivestockFallbackText;
    }

    private static string GetNoMatchesText(string? noMatchesText)
    {
        if (string.IsNullOrWhiteSpace(value: noMatchesText) == false)
        {
            return noMatchesText!;
        }

        return NoMatchesFallbackText;
    }

    private static string? GetTranslatedTextOrNull(ModOptionController controller, string contentId)
    {
        string text = controller.Tr(contentId: contentId);
        if (string.IsNullOrWhiteSpace(value: text) == true ||
            text == contentId)
        {
            return null;
        }

        return text;
    }

    private readonly struct LivestockOption
    {
        internal LivestockOption(string id, string text, string searchText)
        {
            Id = id;
            Text = text;
            SearchText = searchText;
        }

        internal string Id { get; }

        internal string Text { get; }

        private string SearchText { get; }

        internal bool Matches(string searchText)
        {
            if (Id.IndexOf(value: searchText, comparisonType: StringComparison.CurrentCultureIgnoreCase) >= 0)
            {
                return true;
            }

            return Text.IndexOf(value: searchText, comparisonType: StringComparison.CurrentCultureIgnoreCase) >= 0 ||
                   SearchText.IndexOf(value: searchText, comparisonType: StringComparison.CurrentCultureIgnoreCase) >= 0;
        }
    }
}
