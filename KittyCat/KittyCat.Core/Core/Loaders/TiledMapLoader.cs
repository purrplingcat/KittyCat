using DotTiled.Serialization;
using Microsoft.Xna.Framework.Content;
using PurrplingCore.Toolkit.Content;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KittyCat.Core.Loaders;

public class TiledMapLoader(ContentManager content)
{
    private readonly Loader _loader = Loader.DefaultWith(new MonogameResourceReader(content));

    private class MonogameResourceReader(ContentManager content) : IResourceReader
    {
        public string Read(string resourcePath)
        {
            using var stream = content.OpenStream(resourcePath);
            using var reader = new StreamReader(stream);

            return reader.ReadToEnd();
        }
    }

    public DotTiled.Map Load(string path) => _loader.LoadMap(path);
}
