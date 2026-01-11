using Avalonia.Controls;
using PortPilot_Project.ViewModels;

namespace PortPilot_Project.Views
{
    public partial class MainWindow : Window
    {
        public static MainWindow? Current { get; private set; }

        public MainWindow()
        {
            InitializeComponent();
            Current = this;
            Closing += OnClosing;
        }

        private void OnClosing(object? sender, WindowClosingEventArgs e)
        {
            if (DataContext is not MainWindowViewModel vm)
                return;

            if (vm.IsExiting)
                return;

            if (vm.MinimizeToTrayOnClose)
            {
                e.Cancel = true;
                Hide();
            }
        }
    }
}