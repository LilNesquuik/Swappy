using System;
using System.Threading.Tasks;

namespace Swappy.API.Features;

public abstract class DependencyResource
{
    public abstract Task Resolve(Version current, string fileName, string outputPath);
}