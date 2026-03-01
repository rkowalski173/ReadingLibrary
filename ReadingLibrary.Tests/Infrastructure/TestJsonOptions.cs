using System.Text.Json;

namespace ReadingLibrary.Tests.Infrastructure;

public static class TestJsonOptions
{
    public static readonly JsonSerializerOptions Default = new() { PropertyNameCaseInsensitive = true };
}