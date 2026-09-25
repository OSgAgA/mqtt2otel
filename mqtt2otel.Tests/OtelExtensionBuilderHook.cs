using Microsoft.Testing.Extensions;
using Microsoft.Testing.Platform.Builder;

namespace mqtt2otel.Tests.Extensions;

/// <summary>
/// This class will be used to register a new extension, which will then manually register the open telemetry provider for mtp.
/// 
/// This is needed as the steps for registering the extensions as documented here: https://learn.microsoft.com/en-us/dotnet/core/testing/microsoft-testing-platform-open-telemetry 
/// are not working with xunit, as it ignores the <code><GenerateTestingPlatformEntryPoint>false</GenerateTestingPlatformEntryPoint></code> setting needed to create a new entry point.
/// 
/// This registration process foolows https://learn.microsoft.com/en-us/dotnet/core/testing/microsoft-testing-platform-architecture-extensions
/// 
/// To ensure that the entry point is called, you have to register it in your project file:
/// 
/// <code>
///   <ItemGroup>
///     <TestingPlatformBuilderHook Include = "036c3204-36ab-4d91-ad8a-27e8fe58e823" >
///       < DisplayName > Otel.Extension </ DisplayName >
///       < TypeFullName > mqtt2otel.Tests.Extensions.OtelExtensionBuilderHook </ TypeFullName >
///     </ TestingPlatformBuilderHook >
///   </ ItemGroup >
/// </code>
/// </summary>
public static class OtelExtensionBuilderHook
{
    /// <summary>
    /// Adds the open telemetry extension and configures it using the standard otel environment variables.
    /// </summary>
    /// <param name="builder">The test application builder to register the extension.</param>
    /// <param name="arguments">The command line arguments.</param>
    public static void AddExtensions(ITestApplicationBuilder builder, string[] arguments)
    {
        builder.AddOpenTelemetryProviderFromEnvironment();
    }
}
