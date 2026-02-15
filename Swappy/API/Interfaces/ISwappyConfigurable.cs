using Swappy.API.Features;

namespace Swappy.API.Interfaces;

public interface ISwappyConfigurable
{
    public DependencyResource Repository { get; }
}
