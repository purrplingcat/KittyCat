using Microsoft.Extensions.Logging;
using System.Reflection;

namespace PurrplingCore.Toolkit.DI;

public class GameHostBuilderContext
{
    public IGameHostBuilder Builder { get; }
    public ILoggerFactory LoggerFactory { get; }
    public string Directory { get; }
    

    internal GameHostBuilderContext(IGameHostBuilder builder, ILoggerFactory loggerFactory, Assembly executingAssembly)
    {
        Builder = builder;
        LoggerFactory = loggerFactory;
        Directory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? string.Empty;
    }
}
