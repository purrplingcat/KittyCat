using KittyCat;
using PurrplingCore.Toolkit.Hosting;
using System.Windows.Forms;

internal class Program
{
    /// <summary>
    /// The main entry point for the application on Windows.
    /// Configures the application for high DPI awareness.
    /// It also creates an instance of your game and calls it's Run() method 
    /// </summary>
    /// <param name="args">Command-line arguments passed to the application.</param>
    private static void Main(string[] args)
    {
        // Configure the application to be DPI-aware for better display scaling.
        Application.SetHighDpiMode(HighDpiMode.SystemAware);

        // Create an instance of the game host and start the game loop.
        using var game = GameHost.CreateBuilder()
            .AddGame<KittyCatGame>()
            .Build();

        game.Run();
    }
}
