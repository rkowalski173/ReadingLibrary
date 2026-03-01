using Xunit.Abstractions;
using Xunit.Sdk;

namespace ReadingLibrary.Tests;

[TraitDiscoverer("ReadingLibrary.Tests.IntegrationTestDiscoverer", "ReadingLibrary.Tests")]
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class IntegrationTestAttribute : Attribute, ITraitAttribute { }

public class IntegrationTestDiscoverer : ITraitDiscoverer
{
    public IEnumerable<KeyValuePair<string, string>> GetTraits(IAttributeInfo traitAttribute)
    {
        yield return new KeyValuePair<string, string>("Category", "Integration");
    }
}
