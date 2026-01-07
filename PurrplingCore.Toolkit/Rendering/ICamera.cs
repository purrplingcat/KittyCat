using Microsoft.Xna.Framework;

namespace PurrplingCore.Toolkit.Rendering;

public interface ICamera 
{
    Matrix ViewMatrix { get; }
    Matrix ProjectionMatrix { get; }
    BoundingFrustum Frustum { get; } 

    void UpdateState(Matrix view, Matrix projection);
}
