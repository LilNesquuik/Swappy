using Swappy.API.Abstractions;

namespace Swappy.API.Interfaces;

public interface ISwappyConfigurable
{
    public DependencyResource RepositoryConfiguration { get; }
}
