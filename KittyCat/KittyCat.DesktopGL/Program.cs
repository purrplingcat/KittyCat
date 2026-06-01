using KittyCat;
using KittyCat.Configuration;
using KittyCat.DesktopGL;
using Microsoft.Extensions.DependencyInjection;
using PurrplingCore.Toolkit.Hosting;
using PurrplingCore.Toolkit.Modding;
using PurrplingCore.Toolkit.Pak;
using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

Console.OutputEncoding = Encoding.UTF8;

var builder = GameHost.CreateBuilder(args);

builder.AddGame<KittyCatGame>();
builder.UsePurrplingCore();
builder.AddMods(Path.Combine(AppContext.BaseDirectory, "Mods"));
builder.Logging.AddDefaultLogging();

using var host = builder.Build();

var env = host.Services.GetRequiredService<IHostEnvironment>();
var paker = new PakPacker()
{
    Compression = CompressionMethod.LZ4,
};
paker.AddDirectory(Path.Combine(env.BaseDirectory, "Content"), "Content");
paker.Pack(Path.Combine(env.BaseDirectory, "Content.pak"));

var pak = new PakArchive(File.OpenRead(Path.Combine(env.BaseDirectory, "Content.pak")));
var stream = pak.OpenFile("Content/Test.txt");
var reader = new StreamReader(stream);

Console.ForegroundColor = ConsoleColor.Green;
Console.WriteLine(reader.ReadToEnd());
Console.ResetColor();

host.Run();
