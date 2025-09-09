using System.Globalization;
using System.Net;
using System.Runtime;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Server.Kestrel.Core;

// enable LOH compaction during GC gen2
GCSettings.LargeObjectHeapCompactionMode = GCLargeObjectHeapCompactionMode.CompactOnce;

var builder = WebApplication.CreateEmptyBuilder(new() {
  Args = args,
  ApplicationName = "dotnet",
  ContentRootPath = AppContext.BaseDirectory,
  WebRootPath = null,
  EnvironmentName = "Production",
});

builder.WebHost.ConfigureKestrel(static opt => {
  int port = int.Parse(
    Environment.GetEnvironmentVariable("PORT") ?? "80",
    NumberStyles.None,
    CultureInfo.InvariantCulture
  );
  opt.Listen(IPAddress.Parse("0.0.0.0"), port, srv => {
    srv.Protocols = HttpProtocols.Http1;
    srv.DisableAltSvcHeader = true;
  });
});

builder.WebHost.UseKestrelCore();
builder.WebHost.ConfigureKestrel(static opt => {
  opt.AddServerHeader = false;
  opt.Limits.MinResponseDataRate = null;
  opt.Limits.MinRequestBodyDataRate = null;
  opt.Limits.MaxRequestBodySize = null;
  opt.Limits.MaxRequestBufferSize = null;
  opt.Limits.MaxRequestLineSize = 1024;
  opt.AllowSynchronousIO = true;
  opt.AllowHostHeaderOverride = true;
  opt.AllowResponseHeaderCompression = false;
  opt.DisableStringReuse = false;
});
builder.Services.AddRoutingCore();
builder.Services.Configure<RouteOptions>(opt => {
  opt.SuppressCheckForUnhandledSecurityMetadata = true;
});

builder.Services.ConfigureHttpJsonOptions(options => {
  options.SerializerOptions.TypeInfoResolverChain.Insert(0, JsonContext.Default);
});

var app = builder.Build();

app.UseRouting();

app.MapPost("/{id}", ([FromRoute] int id, [FromBody] JsonBody body) => {
  return TypedResults.Ok(body.Value + id);
});

await app.RunAsync().ConfigureAwait(false);

internal record struct JsonBody
{
  public int Value { get; set; }
}

[JsonSourceGenerationOptions(
  WriteIndented = false,
  NewLine = "\n",
  NumberHandling = JsonNumberHandling.Strict,
  DefaultBufferSize = 256,
  IndentSize = 2,
  DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
  RespectNullableAnnotations = true,
  RespectRequiredConstructorParameters = false,
  GenerationMode = JsonSourceGenerationMode.Default
)]
[JsonSerializable(typeof(JsonBody))]
internal sealed partial class JsonContext : JsonSerializerContext
{
}
