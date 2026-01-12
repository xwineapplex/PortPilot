using System.Threading.Tasks;
using Avalonia.Controls;

namespace PortPilot_Project.Views;

public enum MessageBoxWindowResult
{
    Ok = 0,
    RestartNow = 1,
}

public partial class MessageBoxWindow : Window
{
    private TaskCompletionSource<MessageBoxWindowResult>? _tcs;

    public MessageBoxWindow()
    {
        InitializeComponent();

        OkButton.Click += (_, __) => CloseWith(MessageBoxWindowResult.Ok);
        RestartButton.Click += (_, __) => CloseWith(MessageBoxWindowResult.RestartNow);
    }

    private void CloseWith(MessageBoxWindowResult result)
    {
        _tcs?.TrySetResult(result);
        Close(result);
    }

    public static async Task<MessageBoxWindowResult> ShowOkRestartAsync(
        Window owner,
        string title,
        string message,
        string okText,
        string restartText)
    {
        var win = new MessageBoxWindow
        {
            Title = title,
        };

        win.MessageText.Text = message;

        win.OkButton.Content = okText;
        win.RestartButton.Content = restartText;
        win.RestartButton.IsVisible = true;

        win._tcs = new TaskCompletionSource<MessageBoxWindowResult>();

        await win.ShowDialog(owner);

        if (win._tcs.Task.IsCompleted)
            return await win._tcs.Task;

        return MessageBoxWindowResult.Ok;
    }
}
