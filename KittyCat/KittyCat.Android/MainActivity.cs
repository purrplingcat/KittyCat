using Android.App;
using Android.Content.PM;
using Android.OS;
using Android.Views;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Xna.Framework;

namespace KittyCat.Android;
/// <summary>
/// The main activity for the Android application. It initializes the game instance,
/// sets up the rendering view, and starts the game loop.
/// </summary>
/// <remarks>
/// This class is responsible for managing the Android activity lifecycle and integrating
/// with the MonoGame framework.
/// </remarks>
[Activity(
    Label = "KittyCat",
    MainLauncher = true,
    Icon = "@drawable/icon",
    Theme = "@style/Theme.Splash",
    AlwaysRetainTaskState = true,
    LaunchMode = LaunchMode.SingleInstance,
    ScreenOrientation = ScreenOrientation.SensorLandscape,
    ConfigurationChanges = ConfigChanges.Orientation | ConfigChanges.Keyboard | ConfigChanges.KeyboardHidden
)]
public class MainActivity : AndroidGameActivity
{
    private GameHost _game;
    private View _view;

    /// <summary>
    /// Called when the activity is first created. Initializes the game instance,
    /// retrieves its rendering view, and sets it as the content view of the activity.
    /// Finally, starts the game loop.
    /// </summary>
    /// <param name="bundle">A Bundle containing the activity's previously saved state, if any.</param>
    protected override void OnCreate(Bundle bundle)
    {
        base.OnCreate(bundle);

        static void ConfigureServices(IServiceCollection services)
        {
            services.AddConfiguration(new KittyCatGameServices());
            services.AddSingleton(p => p.GetRequiredService<Game>().Services.GetService<View>());
        }

        _game = GameHost.Create<KittyCatGame>(ConfigureServices);
        _view = _game.Services.GetRequiredService<View>();

        SetContentView(_view);
        _game.Start();
    }
}
