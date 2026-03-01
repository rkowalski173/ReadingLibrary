using Xunit.Abstractions;
using Xunit.Sdk;

namespace ReadingLibrary.Tests;

[TraitDiscoverer("ReadingLibrary.Tests.UnitTestDiscoverer", "ReadingLibrary.Tests")]
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class UnitTestAttribute : Attribute, ITraitAttribute { }

public class UnitTestDiscoverer : ITraitDiscoverer
{
    public IEnumerable<KeyValuePair<string, string>> GetTraits(IAttributeInfo traitAttribute)
    {
        yield return new KeyValuePair<string, string>("Category", "Unit");
    }
}
