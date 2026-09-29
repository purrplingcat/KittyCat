using KittyCat;
using KittyCat.DesktopGL;
using PurrplingCore.Hosting;
using PurrplingCore.Toolkit.Hosting;
using PurrplingCore.Toolkit.Modding;
using System;
using System.IO;
using System.Text;

GameCrashLogger.Register();

#if DEBUG
Console.OutputEncoding = Encoding.UTF8;
#endif

var builder = GameHost.CreateBuilder(args);

builder.UsePurrplingCore();
builder.AddGame<KittyCatGame>();
builder.AddMods(Path.Combine(AppContext.BaseDirectory, "Mods"));
builder.Logging.AddDefaultLogging();

using var host = builder.Build();
return host.Run();
