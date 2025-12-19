using System.Diagnostics.Contracts;
using System.Runtime.CompilerServices;

namespace PurrplingCore.Toolkit.Extensions;

public static class SystemObjectExtensions
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool TryDispose(this object obj)
    {
        if (obj is IDisposable disposable)
        {
            disposable.Dispose();
            return true;
        }

        return false;
    }
}
