using System.Globalization;
using System.Net;
using System.Runtime;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Server.Kestrel.Core;

// enable LOH compaction during GC gen2
GCSettings.LargeObjectHeapCompactionMode = GCLargeObjectHeapCompactionMode.CompactOnce;

WebApplicationBuilder builder = WebApplication.CreateEmptyBuilder(new() {
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

// cuz we don't use parameter/output binding (to be closer to go/gin) we don't need this:
// builder.Services.ConfigureHttpJsonOptions(options => {
//   options.SerializerOptions.TypeInfoResolverChain.Insert(0, JsonContext.Default);
// });

WebApplication app = builder.Build();

app.UseRouting();

/*
 * by [http spec](https://tools.ietf.org/html/rfc7230#section-3.3.3)
 * All HTTP/1.1 requests should have Transfer-Encoding or Content-Length.
 * If you don't specify Content-Length Kestrel will use `Transfer-Encoding: chunked`
 * which produces more bytes and might be less performant (vs gin's constant in Content-Length)
 * so we must workarounds like an additional middleware with something like:
 *   await next(context);
 *   context.Response.Headers.ContentLength = context.Response.Body.Length;
 * or do serialization/output binding by self. Go/gin can't do mapping so it will be
 * fair to do it manually too, so instead of this:
 *  app.MapPost("/{id}", ([FromRoute] int id, [FromBody] JsonBody body) => TypedResults.Ok(body.Value + id));
 * we will write a bit more code that looks like go approach (but we still use routing)
 */

app.MapPost("/{id}", async context => {
  context.Features.Get<IHttpResponseBodyFeature>()!.DisableBuffering();
  context.Response.ContentType = "application/json; charset=utf-8";

  if (!int.TryParse(
        (string)context.Request.RouteValues["id"]!,
        NumberStyles.None,
        CultureInfo.InvariantCulture,
        out int id)) {
    context.Response.StatusCode = 400;
    await context.Response.WriteAsync( /*lang=json,strict*/ """{"error":"Invalid id"}""",
      Encoding.UTF8,
      context.RequestAborted).ConfigureAwait(false);
    return;
  }

  string result;
  try {
    JsonBody json = await context.Request.ReadFromJsonAsync(JsonContext.Default.JsonBody, context.RequestAborted)
      .ConfigureAwait(false);
    result = (id + json.Value).ToString();
  }
  catch (JsonException) {
    context.Response.StatusCode = 400;
    await context.Response.WriteAsync( /*lang=json,strict*/ """{"error":"Invalid body"}""",
      Encoding.UTF8,
      context.RequestAborted).ConfigureAwait(false);
    return;
  }

  context.Response.StatusCode = 200;
  context.Response.Headers.ContentLength = result.Length;
  await context.Response.WriteAsync(result, Encoding.UTF8, context.RequestAborted).ConfigureAwait(false);
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
  DictionaryKeyPolicy = JsonKnownNamingPolicy.Unspecified,
  IncludeFields = false,
  PropertyNameCaseInsensitive = true,
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
