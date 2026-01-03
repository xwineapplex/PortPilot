using Avalonia.Controls;

namespace PortPilot_Project.Views
{
    public partial class MainWindow : Window
    {
        public static MainWindow? Current { get; private set; }

        public MainWindow()
        {
            InitializeComponent();
            Current = this;
        }
    }
}