using System;
using System.Windows;

namespace TCM.DesktopClient
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            AppSettings.Load();

            this.DispatcherUnhandledException += (s, args) =>
            {
                MessageBox.Show(
                    $"Krytyczny błąd aplikacji:\n\n{args.Exception.Message}\n\nLokalizacja:\n{args.Exception.StackTrace}",
                    "Awaria Systemu",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error
                );

                args.Handled = true;
            };
        }
    }
}