using KittyCat;
using Microsoft.Extensions.Logging;
using PurrplingCore.Toolkit.DI;
using System;
using System.IO;

var builder = GameHost.CreateBuilder()
    .AddGame<KittyCatGame>()
    .ConfigureLogging(logging =>
    {
        logging.SetMinimumLevel(LogLevel.Trace);
        logging.AddDebug();
        logging.AddSimpleConsole(options =>
        {
            options.SingleLine = true;
            options.TimestampFormat = "HH:mm:ss ";
            options.IncludeScopes = true;
        });
    });

using var host = builder.Build();
host.Run();
