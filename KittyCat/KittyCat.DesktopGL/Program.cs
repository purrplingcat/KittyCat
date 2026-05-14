using KittyCat;
using KittyCat.DesktopGL;
using PurrplingCore.Toolkit.Hosting;
using PurrplingCore.Toolkit.Modding;
using System;
using System.IO;

var builder = GameHost.CreateBuilder(args);
    
builder.AddGame<KittyCatGame>();
builder.AddMods(Path.Combine(AppContext.BaseDirectory, "Mods"));
builder.Logging.AddDefaultLogging();

using var host = builder.Build();
host.Run();
