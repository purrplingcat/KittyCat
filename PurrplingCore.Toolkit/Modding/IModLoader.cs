using Microsoft.Extensions.DependencyInjection;
using PurrplingCore.Toolkit.DI;

namespace PurrplingCore.Toolkit.Modding;

public interface IModLoader
{
    void Load(IServiceCollection services, GameHostBuilderContext context);
}
