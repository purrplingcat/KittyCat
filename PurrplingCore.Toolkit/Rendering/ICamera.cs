using Microsoft.Xna.Framework;

namespace PurrplingCore.Toolkit.Rendering;

public interface ICamera
{
    Vector2 Position { get; }
    float Zoom { get; }
    Matrix GetViewMatrix();
    Rectangle GetViewport();
}
