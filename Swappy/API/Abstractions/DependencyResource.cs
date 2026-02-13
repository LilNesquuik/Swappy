using System;
using System.Threading.Tasks;

namespace Swappy.API.Abstractions;

public abstract class DependencyResource
{
    public abstract Task Resolve(Version current);
}