using System.Windows;
using TCM.DesktopClient.ViewModels;

namespace TCM.DesktopClient
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            DataContext = new MainViewModel();
        }
    }
}