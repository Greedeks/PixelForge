using System.Windows;
using PixelForge.Core.Services;

namespace PixelForge
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            SettingsService.Load();
        }

        protected override void OnExit(ExitEventArgs e)
        {
            SettingsService.Save();

            base.OnExit(e);
        }
    }

}
