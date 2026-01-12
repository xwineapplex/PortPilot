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

    public ObservableCollection<LanguageOption> AvailableLanguages { get; } = new();

    [ObservableProperty]
    private LanguageOption? selectedLanguage;

    public event Action<bool?>? RequestClose;

    public SettingsWindowViewModel(ConfigStore configStore, Func<Task<bool>> showRestartPromptAsync)
    {
        _configStore = configStore;
        _showRestartPromptAsync = showRestartPromptAsync;

        AvailableLanguages.Add(new LanguageOption(Resources.Enum_Lang_Auto, "auto"));
        AvailableLanguages.Add(new LanguageOption(Resources.Enum_Lang_English, "en-US"));
        AvailableLanguages.Add(new LanguageOption(Resources.Enum_Lang_ZhHant, "zh-Hant"));

        _ = InitializeAsync();
    }

    private async Task InitializeAsync()
    {
        try
        {
            var config = await _configStore.LoadAsync();
            _initialLanguage = Normalize(config.Language);
            SelectedLanguage = AvailableLanguages.FirstOrDefault(o => o.CultureCode == _initialLanguage)
                               ?? AvailableLanguages.FirstOrDefault(o => o.CultureCode == "auto")
                               ?? AvailableLanguages.FirstOrDefault();
        }
        catch
        {
            _initialLanguage = "auto";
            SelectedLanguage = AvailableLanguages.FirstOrDefault(o => o.CultureCode == "auto")
                               ?? AvailableLanguages.FirstOrDefault();
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
        var selected = SelectedLanguage?.CultureCode ?? "auto";
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

public sealed record LanguageOption(string DisplayName, string CultureCode);
