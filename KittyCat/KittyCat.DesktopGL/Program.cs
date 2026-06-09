using KittyCat;
using KittyCat.Configuration;
using KittyCat.DesktopGL;
using PurrplingCore.Toolkit.Hosting;
using PurrplingCore.Toolkit.Modding;
using System;
using System.IO;
using System.Text;

Console.OutputEncoding = Encoding.UTF8;

var builder = GameHost.CreateBuilder(args);

builder.AddGame<KittyCatGame>();
builder.UsePurrplingCore();
builder.AddMods(Path.Combine(AppContext.BaseDirectory, "Mods"));
builder.Logging.AddDefaultLogging();

using var host = builder.Build();
host.Run();
