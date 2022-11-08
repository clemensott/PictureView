using System.IO;
using System.Windows;
using System.Windows.Threading;

namespace PictureView
{
    /// <summary>
    /// Interaktionslogik für "App.xaml"
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            DispatcherUnhandledException += OnDispatcherUnhandledException;
        }

        private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            File.WriteAllText("./UnhandledException.txt", e.Exception.ToString());
        }
    }
}
