using Microsoft.Extensions.Logging;
using PurrplingCore.Toolkit.DI;

namespace KittyCat.DesktopGL;

public static class LoggingExtensions
{
    public static ILoggingBuilder AddDefaultLogging(this ILoggingBuilder builder)
    {
        builder.SetMinimumLevel(LogLevel.Trace)
               .AddDebug()
               .AddSimpleConsole(options =>
               {
                   options.SingleLine = true;
                   options.TimestampFormat = "HH:mm:ss ";
                   options.IncludeScopes = true;
               });

        return builder;
    }
}
