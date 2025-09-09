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

builder.Configuration.AddEnvironmentVariables();

int port = int.Parse(
  builder.Configuration["PORT"] ?? "80",
  NumberStyles.None,
  CultureInfo.InvariantCulture
);

builder.WebHost.ConfigureKestrel(opt => {
  opt.Listen(IPAddress.Parse("0.0.0.0"), port, srv => {
    srv.Protocols = HttpProtocols.Http1;
    srv.DisableAltSvcHeader = true;
  });
});

bool waitForDataBeforeAllocatingBuffer = builder.Configuration.ReadBool("ASPNETCORE_WAIT_FOR_DATA") ?? false;
bool unsafePreferInlineScheduling = builder.Configuration.ReadBool("ASPNETCORE_INLINE_SCHEDULING") ?? true;

builder.WebHost.UseKestrelCore();
builder.WebHost.UseSockets(opt => {
  opt.WaitForDataBeforeAllocatingBuffer = waitForDataBeforeAllocatingBuffer;
  opt.UnsafePreferInlineScheduling = unsafePreferInlineScheduling;
});

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

internal static class Extensions
{
  public static bool? ReadBool(this IConfiguration self, string key)
  {
    string? val = self[key]?.Trim();
    if (string.IsNullOrWhiteSpace(val)) {
      return null;
    }

    if (bool.TryParse(val, out bool result)) {
      return result;
    }
    return val.Length == 1 ? val[0] switch {
      '1' => true,
      '0' => false,
      _ => null,
    } : null;
  }
}
