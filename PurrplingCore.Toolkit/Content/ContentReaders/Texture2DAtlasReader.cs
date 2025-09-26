using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using PurrplingCore.Toolkit.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PurrplingCore.Toolkit.Content.ContentReaders;

public class Texture2DAtlasReader : ContentTypeReader<Texture2DAtlas>
{
    protected override Texture2DAtlas Read(ContentReader input, Texture2DAtlas existingInstance)
    {
        var texture = input.ReadTexture2D();
        var atlas = new Texture2DAtlas(texture.Name, texture);

        var regionCount = input.ReadInt32();

        for (int i = 0; i < regionCount; i++)
        {
            var bounds = input.ReadRectangle();
            string regionName = input.ReadString();

            atlas.CreateRegion(regionName, bounds);
        }

        return atlas;
    }
}
