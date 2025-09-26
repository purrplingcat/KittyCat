using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PurrplingCore.Toolkit;

public interface IProvider<T> where T : class
{
    T Value { get; }
    bool HasValue { get; }
}
