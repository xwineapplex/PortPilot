using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PortPilot_Project.Config;
using PortPilot_Project.Properties;

namespace PortPilot_Project.ViewModels;

public sealed partial class SettingsWindowViewModel : ObservableObject
{
    private readonly ConfigStore _configStore;
    private readonly Func<Task<bool>> _showRestartPromptAsync;

    private string _initialLanguage = "auto";

    public ObservableCollection<LanguageOptionItem> LanguageOptions { get; } = new();

    [ObservableProperty]
    private LanguageOptionItem? selectedLanguageOption;

    public event Action<bool?>? RequestClose;

    public SettingsWindowViewModel(ConfigStore configStore, Func<Task<bool>> showRestartPromptAsync)
    {
        _configStore = configStore;
        _showRestartPromptAsync = showRestartPromptAsync;

        LanguageOptions.Add(new LanguageOptionItem("auto", Resources.Enum_Lang_Auto));
        LanguageOptions.Add(new LanguageOptionItem("en-US", Resources.Enum_Lang_English));
        LanguageOptions.Add(new LanguageOptionItem("zh-Hant", Resources.Enum_Lang_ZhHant));

        _ = InitializeAsync();
    }

    private async Task InitializeAsync()
    {
        try
        {
            var config = await _configStore.LoadAsync();
            _initialLanguage = Normalize(config.Language);
            SelectedLanguageOption = LanguageOptions.FirstOrDefault(o => o.Value == _initialLanguage)
                                     ?? LanguageOptions.FirstOrDefault(o => o.Value == "auto")
                                     ?? LanguageOptions.FirstOrDefault();
        }
        catch
        {
            _initialLanguage = "auto";
            SelectedLanguageOption = LanguageOptions.FirstOrDefault(o => o.Value == "auto")
                                     ?? LanguageOptions.FirstOrDefault();
        }
    }

    [RelayCommand]
    private void Cancel()
    {
        RequestClose?.Invoke(false);
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        var selected = SelectedLanguageOption?.Value ?? "auto";
        var normalized = Normalize(selected);

        var config = await _configStore.LoadAsync();
        config.Language = normalized;
        await _configStore.SaveAsync(config);

        var changed = !string.Equals(Normalize(_initialLanguage), normalized, StringComparison.OrdinalIgnoreCase);
        if (changed)
        {
            await _showRestartPromptAsync();
        }

        RequestClose?.Invoke(true);
    }

    private static string Normalize(string? value)
    {
        var v = (value ?? "auto").Trim();
        return string.IsNullOrWhiteSpace(v) ? "auto" : v;
    }
}

public sealed record LanguageOptionItem(string Value, string DisplayName);
