using Android.Content.PM;
using Avalonia.Android;

namespace OpenMcdf.Explorer.Android;

[Activity(
    Label = "OpenMcdf Explorer",
    Theme = "@style/MyTheme.NoActionBar",
    Icon = "@drawable/icon",
    MainLauncher = true,
    ConfigurationChanges = ConfigChanges.Orientation | ConfigChanges.ScreenSize | ConfigChanges.UiMode)]
public class MainActivity : AvaloniaMainActivity
{
}
