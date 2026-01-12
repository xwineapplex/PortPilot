using System;
using System.Threading.Tasks;
using Avalonia.Controls;
using PortPilot_Project.Config;
using PortPilot_Project.Utils;
using PortPilot_Project.ViewModels;

namespace PortPilot_Project.Views;

public partial class SettingsWindow : Window
{
    public SettingsWindow()
        : this(new ConfigStore())
    {
    }

    public SettingsWindow(ConfigStore configStore)
    {
        InitializeComponent();

        var vm = new SettingsWindowViewModel(configStore, ShowRestartPromptAsync);
        vm.RequestClose += _ => Close();
        DataContext = vm;
    }

    private async Task<bool> ShowRestartPromptAsync()
    {
        var owner = this;
        var result = await MessageBoxWindow.ShowOkRestartAsync(
            owner,
            PortPilot_Project.Properties.Resources.Settings_Title,
            PortPilot_Project.Properties.Resources.Msg_LanguageChangedRestart,
            PortPilot_Project.Properties.Resources.Common_Ok,
            PortPilot_Project.Properties.Resources.Common_RestartNow);

        if (result == MessageBoxWindowResult.RestartNow)
        {
            AppRestart.RestartApplication();
            return true;
        }

        return false;
    }
}
