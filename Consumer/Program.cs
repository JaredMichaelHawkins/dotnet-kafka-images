using Consumer;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Shared.Extensions;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddOpenTelemetry()
    .WithTracing(traceProvider =>
{
    traceProvider
        .AddSource(OpenTelemetryExtensions.ServiceName)
        .SetResourceBuilder(
            ResourceBuilder.CreateDefault()
                .AddService(serviceName: OpenTelemetryExtensions.ServiceName,
                    serviceVersion: OpenTelemetryExtensions.ServiceVersion))
        .SetSampler(new AlwaysOnSampler())
        .AddAspNetCoreInstrumentation()
        .AddOtlpExporter();
})
    .WithMetrics(metricsProvider =>
{
    metricsProvider
        .AddMeter(OpenTelemetryExtensions.ServiceName)
        .SetExemplarFilter(ExemplarFilterType.AlwaysOn)
        .AddRuntimeInstrumentation()
        .AddAspNetCoreInstrumentation()
        .AddOtlpExporter();

});
builder.Services.AddHostedService<Worker>();
var host = builder.Build();
host.Run();
