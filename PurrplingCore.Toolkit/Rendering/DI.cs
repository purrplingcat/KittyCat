using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using PurrplingCore.Toolkit.DI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace PurrplingCore.Toolkit.Rendering;
public interface IRenderPipelineFactory
{
    // Tato metoda vezme základní seznam passů a vrátí hotovou pipeline.
    // Mod loader ji může "obalit" (Proxy pattern) nebo "patchnout".
    RenderPipeline CreatePipeline(IServiceProvider sp, IReadOnlyList<RenderPassDescriptor> coreDescriptors);
}

public class DefaultRenderPipelineFactory : IRenderPipelineFactory
{
    public RenderPipeline CreatePipeline(IServiceProvider sp, IReadOnlyList<RenderPassDescriptor> descriptors)
    {
        var instances = new IRenderPass[descriptors.Count];

        for (int i = 0; i < descriptors.Count; i++)
        {
            var desc = descriptors[i];

            // 1. Vytvoříme features pro tento pass
            var features = new IRenderFeature2D[desc.FeatureTypes.Count];
            for (int f = 0; f < desc.FeatureTypes.Count; f++)
            {
                features[f] = (IRenderFeature2D)sp.GetRequiredService(desc.FeatureTypes[f]);
            }

            // 2. Vytvoříme pass a předáme mu pole featur jako parametr
            // ActivatorUtilities se postará o to, aby pass dostal i kameru, batch atd.
            instances[i] = descriptors[i].AcceptsFeatures 
                ? (IRenderPass)ActivatorUtilities.CreateInstance(sp, desc.PassType, (object)features)
                : (IRenderPass)ActivatorUtilities.CreateInstance(sp, desc.PassType);
        }

        return new RenderPipeline(instances);
    }
}

public class RenderPipelineOptions
{
    public List<RenderPassDescriptor> Descriptors { get; } = [];
}

public static class RenderPipelineExtensions
{
    public static IRenderPipelineBuilder AddRenderPipeline(this IServiceCollection services)
    {

        services.AddOptions<RenderPipelineOptions>();
        services.AddSingleton<IRenderPipelineFactory, DefaultRenderPipelineFactory>();

        // Pipeline se sestaví pomocí factory
        services.AddSingleton<RenderPipeline>(sp => {
            var factory = sp.GetRequiredService<IRenderPipelineFactory>();
            var options = sp.GetRequiredService<IOptions<RenderPipelineOptions>>().Value;
            return factory.CreatePipeline(sp, options.Descriptors);
        });
        
        return new RenderPipelineBuilder(services);
    }
}

public sealed class RenderPassDescriptor(string name, Type passType)
{
    public string Name { get; } = name;
    public Type PassType { get; } = passType;
    public List<Type> FeatureTypes { get; } = [];
    public bool AcceptsFeatures { get; } = passType.HasConstructorWith(typeof(IRenderFeature2D[]));

    public void AddFeature<T>() where T : class, IRenderFeature2D
    {
        FeatureTypes.Add(typeof(T));
    }
}

public interface IRenderPipelineBuilder
{
    // Interface používá PassOptions, aby se modder/uživatel 
    // nedostal k vnitřnostem deskriptoru, pokud nechceš.
    IRenderPipelineBuilder AddPass<TPass>(string name, Action<RenderPassDescriptor>? configure = null)
        where TPass : class, IRenderPass;
}

public sealed class RenderPipelineBuilder(IServiceCollection services) : IRenderPipelineBuilder
{
    public IRenderPipelineBuilder AddPass<TPass>(string name, Action<RenderPassDescriptor>? configure = null)
        where TPass : class, IRenderPass
    {
        var descriptor = new RenderPassDescriptor(name, typeof(TPass));

        // Předáme PassOptions, který zapouzdřuje deskriptor
        configure?.Invoke(descriptor);

        services.TryAddTransient<TPass>();
        services.Configure<RenderPipelineOptions>(options => options.Descriptors.Add(descriptor));

        return this;
    }
}

public static class TypeExtensions
{
    public static bool HasConstructorWith(this Type type, params Type[] requiredTypes)
    {
        if (requiredTypes.Length == 0) return true;

        var constructors = type.GetConstructors();
        var requiredSpan = requiredTypes.AsSpan();

        for (int i = 0; i < constructors.Length; i++)
        {
            if (CheckConstructor(constructors[i], requiredSpan))
                return true;
        }

        return false;
    }

    private static bool CheckConstructor(ConstructorInfo ctor, ReadOnlySpan<Type> requiredTypes)
    {
        var parameters = ctor.GetParameters();
        if (parameters.Length < requiredTypes.Length) return false;

        // Pro každý požadovaný typ zkontrolujeme přítomnost v parametrech
        for (int i = 0; i < requiredTypes.Length; i++)
        {
            bool found = false;
            for (int j = 0; j < parameters.Length; j++)
            {
                if (parameters[j].ParameterType == requiredTypes[i])
                {
                    found = true;
                    break;
                }
            }

            if (!found) return false;
        }

        return true;
    }
}
