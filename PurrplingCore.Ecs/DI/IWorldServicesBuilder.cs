using Microsoft.Extensions.DependencyInjection;

namespace PurrplingCore.Ecs.DI;

public interface IWorldServicesBuilder
{
    IServiceCollection Services { get; }
    WorldSignature Signature { get; }
    object Key { get; }
}
