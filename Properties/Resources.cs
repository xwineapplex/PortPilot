using System.Globalization;
using System.Resources;

namespace PortPilot_Project.Properties;

public static class Resources
{
    private static readonly ResourceManager ResourceManager = new(
        "PortPilot_Project.Properties.Resources",
        typeof(Resources).Assembly);

    /// <summary>
    /// Override resource culture; use CurrentUICulture when null.
    /// </summary>
    public static CultureInfo? Culture { get; set; }

    private static string Get(string name)
        => ResourceManager.GetString(name, Culture) ?? name;

    public static string Common_AppName => Get(nameof(Common_AppName));
    public static string Common_Error => Get(nameof(Common_Error));
    public static string Common_Save => Get(nameof(Common_Save));
    public static string Common_Cancel => Get(nameof(Common_Cancel));
    public static string Common_Ok => Get(nameof(Common_Ok));
    public static string Common_RestartNow => Get(nameof(Common_RestartNow));
    public static string Common_Unknown => Get(nameof(Common_Unknown));

    public static string Enum_Lang_Auto => Get(nameof(Enum_Lang_Auto));
    public static string Enum_Lang_English => Get(nameof(Enum_Lang_English));
    public static string Enum_Lang_ZhHant => Get(nameof(Enum_Lang_ZhHant));

    public static string Enum_InputSource_DisplayPort1 => Get(nameof(Enum_InputSource_DisplayPort1));
    public static string Enum_InputSource_DisplayPort2 => Get(nameof(Enum_InputSource_DisplayPort2));
    public static string Enum_InputSource_HDMI1 => Get(nameof(Enum_InputSource_HDMI1));
    public static string Enum_InputSource_HDMI2 => Get(nameof(Enum_InputSource_HDMI2));
    public static string Enum_InputSource_VGA => Get(nameof(Enum_InputSource_VGA));
    public static string Enum_InputSource_NoAction => Get(nameof(Enum_InputSource_NoAction));

    public static string Main_Title => Get(nameof(Main_Title));
    public static string Main_Step1_Title => Get(nameof(Main_Step1_Title));
    public static string Main_Step2_Title => Get(nameof(Main_Step2_Title));
    public static string Main_Step3_Title => Get(nameof(Main_Step3_Title));

    public static string Main_Btn_Refresh => Get(nameof(Main_Btn_Refresh));
    public static string Main_Btn_AddRule => Get(nameof(Main_Btn_AddRule));
    public static string Main_Btn_Test => Get(nameof(Main_Btn_Test));
    public static string Main_Btn_Settings => Get(nameof(Main_Btn_Settings));

    public static string Main_Check_ShowAllUsbEvents => Get(nameof(Main_Check_ShowAllUsbEvents));
    public static string Main_Check_EnableMonitoring => Get(nameof(Main_Check_EnableMonitoring));
    public static string Main_Check_MinimizeToTray => Get(nameof(Main_Check_MinimizeToTray));

    public static string Main_Header_ActiveRules => Get(nameof(Main_Header_ActiveRules));
    public static string Main_Btn_OpenConfigFolder => Get(nameof(Main_Btn_OpenConfigFolder));

    public static string Main_GridHeader_Monitor => Get(nameof(Main_GridHeader_Monitor));
    public static string Main_GridHeader_Vid => Get(nameof(Main_GridHeader_Vid));
    public static string Main_GridHeader_Pid => Get(nameof(Main_GridHeader_Pid));
    public static string Main_GridHeader_OnAdded => Get(nameof(Main_GridHeader_OnAdded));
    public static string Main_GridHeader_OnRemoved => Get(nameof(Main_GridHeader_OnRemoved));
    public static string Main_GridHeader_Actions => Get(nameof(Main_GridHeader_Actions));
    public static string Main_Btn_Delete => Get(nameof(Main_Btn_Delete));

    public static string Main_Label_OnAdded => Get(nameof(Main_Label_OnAdded));
    public static string Main_Label_OnRemoved => Get(nameof(Main_Label_OnRemoved));

    public static string Main_Tooltip_TestInputSource => Get(nameof(Main_Tooltip_TestInputSource));

    public static string Main_Debug_DebugMode => Get(nameof(Main_Debug_DebugMode));
    public static string Main_Debug_Copy => Get(nameof(Main_Debug_Copy));
    public static string Main_Debug_Save => Get(nameof(Main_Debug_Save));

    public static string Tray_Tooltip_Running => Get(nameof(Tray_Tooltip_Running));
    public static string Tray_Menu_Open => Get(nameof(Tray_Menu_Open));
    public static string Tray_Menu_AutoStart => Get(nameof(Tray_Menu_AutoStart));
    public static string Tray_Menu_Exit => Get(nameof(Tray_Menu_Exit));
    public static string Tray_Menu_MonitoringActive => Get(nameof(Tray_Menu_MonitoringActive));
    public static string Tray_Menu_MonitoringInactive => Get(nameof(Tray_Menu_MonitoringInactive));

    public static string Settings_Title => Get(nameof(Settings_Title));
    public static string Settings_Label_Language => Get(nameof(Settings_Label_Language));

    public static string Msg_LanguageChangedRestart => Get(nameof(Msg_LanguageChangedRestart));
    public static string Msg_DeviceConnected => Get(nameof(Msg_DeviceConnected));
    public static string Msg_DeviceDisconnected => Get(nameof(Msg_DeviceDisconnected));

    public static string Msg_Error_SwitchInputFailed => Get(nameof(Msg_Error_SwitchInputFailed));
    public static string Msg_Error_NotSupported => Get(nameof(Msg_Error_NotSupported));
    public static string Msg_Error_ConfigLoadFailed => Get(nameof(Msg_Error_ConfigLoadFailed));

    public static string Msg_Error_LinuxUsbWatcher => Get(nameof(Msg_Error_LinuxUsbWatcher));
    public static string Msg_Error_UdevadmScanFailed => Get(nameof(Msg_Error_UdevadmScanFailed));
    public static string Msg_Error_DetectMonitorsFailed => Get(nameof(Msg_Error_DetectMonitorsFailed));
    public static string Msg_Error_SetInputSourceFailed => Get(nameof(Msg_Error_SetInputSourceFailed));
    public static string Msg_Error_DdcutilExited => Get(nameof(Msg_Error_DdcutilExited));

    public static string Msg_Error_InvalidMonitorIdFormat => Get(nameof(Msg_Error_InvalidMonitorIdFormat));
    public static string Msg_Error_PhysicalMonitorIndexOutOfRange => Get(nameof(Msg_Error_PhysicalMonitorIndexOutOfRange));


    public static string Msg_Error_NoValidMonitorSelected => Get(nameof(Msg_Error_NoValidMonitorSelected));
    public static string Msg_Error_SelectedUsbMissingVidPid => Get(nameof(Msg_Error_SelectedUsbMissingVidPid));
    public static string Msg_Error_InputSourceZero => Get(nameof(Msg_Error_InputSourceZero));

    public static string Msg_Status_Saved => Get(nameof(Msg_Status_Saved));
    public static string Msg_Status_Opened => Get(nameof(Msg_Status_Opened));
    public static string Msg_Status_FoundMonitors => Get(nameof(Msg_Status_FoundMonitors));
    public static string Msg_Status_RefreshingMonitors => Get(nameof(Msg_Status_RefreshingMonitors));
    public static string Msg_Status_MonitoringStarted => Get(nameof(Msg_Status_MonitoringStarted));
    public static string Msg_Status_MonitoringStopped => Get(nameof(Msg_Status_MonitoringStopped));
    public static string Msg_Status_DebugLogCopied => Get(nameof(Msg_Status_DebugLogCopied));
    public static string Msg_Status_DebugLogSaved => Get(nameof(Msg_Status_DebugLogSaved));

    public static string Msg_Error_ClipboardNotAvailable => Get(nameof(Msg_Error_ClipboardNotAvailable));
    public static string Msg_Error_InvalidConfigDirectory => Get(nameof(Msg_Error_InvalidConfigDirectory));
    public static string Msg_Error_ConfigDirectoryMissing => Get(nameof(Msg_Error_ConfigDirectoryMissing));
    public static string Msg_Error_OpenFolderFailed => Get(nameof(Msg_Error_OpenFolderFailed));

    public static string Msg_Error_SelectMonitorFirst => Get(nameof(Msg_Error_SelectMonitorFirst));
    public static string Msg_Error_InvalidInputSourceOption => Get(nameof(Msg_Error_InvalidInputSourceOption));
    public static string Msg_Error_NoActionToTest => Get(nameof(Msg_Error_NoActionToTest));
    public static string Msg_Status_TestSwitchingTo => Get(nameof(Msg_Status_TestSwitchingTo));
    public static string Msg_Status_CommandSent => Get(nameof(Msg_Status_CommandSent));
    public static string Msg_Error_Prefix => Get(nameof(Msg_Error_Prefix));

    public static string Msg_Error_RuleAtLeastOneActionRequired => Get(nameof(Msg_Error_RuleAtLeastOneActionRequired));

    public static string Msg_Status_RuleSet => Get(nameof(Msg_Status_RuleSet));
    public static string Msg_Status_RuleDeleted => Get(nameof(Msg_Status_RuleDeleted));
    public static string Msg_Status_RuleMatched => Get(nameof(Msg_Status_RuleMatched));
    public static string Msg_Status_RuleApplied => Get(nameof(Msg_Status_RuleApplied));
}
