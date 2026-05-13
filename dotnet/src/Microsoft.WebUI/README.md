# Microsoft.WebUI

High-performance server-side rendering for .NET — no JavaScript runtime required.

WebUI separates static and dynamic content at build time into a binary protocol that enables fast rendering in any host language. This package provides .NET bindings to the WebUI native rendering engine.

## Quick Start

```csharp
using Microsoft.WebUI;

// One-shot render (parse + render in a single call)
var html = "<div>Hello, {{name}}!</div>";
var json = """{"name": "World"}""";
var result = WebUIRenderer.RenderHtml(html, json);
// result: "<div>Hello, World!</div>"
```

## Handler API (Request-Scoped Full SSR)

For full-page renders with pre-compiled protocol data, create a `WebUIHandler`
inside the request and dispose it after rendering. The compiled `protocol.bin`
bytes can be cached and reused across requests, but `WebUIHandler` instances
wrap native state and are not thread-safe.

```csharp
using Microsoft.WebUI;

// Load pre-compiled protocol binary once at startup (from `webui build`)
byte[] protocol = File.ReadAllBytes("dist/protocol.bin");

// Create a handler per request (optionally with a plugin name)
using var handler = new WebUIHandler("webui");

// Optional: emit a CSP nonce on inline script/style tags
handler.SetNonce(requestNonce);

// Render this request
var html = handler.Render(protocol, """{"user": "Alice"}""", "index.html", "/");
```

Refer to the WebUI documentation for the available plugin identifiers.

## Content Security Policy

When your server sends a `Content-Security-Policy` header, generate a nonce for
each full-page response. Use the same value in the CSP header and on the
request-local `WebUIHandler` before rendering.

```csharp
using System.Security.Cryptography;
using System.Text.Json;

app.MapGet("/{**path}", (HttpContext ctx) =>
{
    var nonce = Convert.ToBase64String(RandomNumberGenerator.GetBytes(16));
    var requestPath = ctx.Request.Path.Value ?? "/";
    var stateJson = JsonSerializer.Serialize(new { user = "Alice" });

    using var handler = new WebUIHandler("webui");
    handler.SetNonce(nonce);

    var html = handler.Render(protocol, stateJson, "index.html", requestPath);

    ctx.Response.Headers.ContentSecurityPolicy =
        $"default-src 'self'; script-src 'self' 'nonce-{nonce}'; style-src 'self' 'unsafe-inline'";

    return Results.Content(html, "text/html");
});
```

WebUI full SSR emits an inline `window.__webui` bootstrap script and, in module
CSS mode, inline `<style type="module">` tags. `SetNonce` adds `nonce="..."`
to the full-page inline WebUI tags and also exposes the value as
`window.__webui.nonce` so the client router can apply it to scripts injected
during partial navigation.

## Client-Side Navigation (Partial Responses)

When the client navigates via the WebUI Router, your server returns a JSON partial instead of full HTML. Use `WebUIRenderer.RenderPartial` — one call produces the complete response with state, templates, inventory, path, and matched route chain:

```csharp
app.MapGet("/users/{id}", (HttpContext ctx, string id) =>
{
    var requestPath = ctx.Request.Path.Value ?? "/";
    var state = new { name = GetUser(id).Name };
    var stateJson = JsonSerializer.Serialize(state);

    if (ctx.Request.Headers.Accept.Contains("application/json"))
    {
        // Client-side navigation — return JSON partial (no assembly required)
        var inventoryHex = ctx.Request.Headers["X-WebUI-Inventory"].FirstOrDefault() ?? "";
        var json = WebUIRenderer.RenderPartial(protocol, stateJson, "index.html", requestPath, inventoryHex);
        return Results.Content(json, "application/json");
    }

    // Full SSR — create a request-local handler and return the complete page
    using var handler = new WebUIHandler("webui");
    var html = handler.Render(protocol, stateJson, "index.html", requestPath);
    return Results.Content(html, "text/html");
});
```

The response is a JSON string — pipe it directly to the HTTP response. No deserialization needed.

## Protocol Utilities

Two helper APIs expose protocol-only operations without creating a handler:

```csharp
var componentPayload = WebUIRenderer.RenderComponentTemplates(protocol, "[\"user-card\"]", "");
var tokens = WebUIRenderer.GetProtocolTokens(protocol);
```

- `RenderComponentTemplates` returns the JSON payload used by the client router for lazily requested component templates.
- `GetProtocolTokens` returns the protocol token names without the `--` prefix.

## Installation

```bash
dotnet add package Microsoft.WebUI
```

The `Microsoft.WebUI` package carries staged native assets under `runtimes/<rid>/native/` for supported platforms. Release packaging also emits standalone runtime packages per platform when you need them separately.

### Supported Platforms

| Runtime | Package |
| ------- | ------- |
| Windows x64 | `Microsoft.WebUI.Runtime.win-x64` |
| Windows ARM64 | `Microsoft.WebUI.Runtime.win-arm64` |
| Linux x64 | `Microsoft.WebUI.Runtime.linux-x64` |
| Linux ARM64 | `Microsoft.WebUI.Runtime.linux-arm64` |
| macOS x64 | `Microsoft.WebUI.Runtime.osx-x64` |
| macOS ARM64 | `Microsoft.WebUI.Runtime.osx-arm64` |

### Manual Native Library Path

If you need to point to a custom build of the native library:

```bash
export WEBUI_LIB_PATH=/path/to/directory   # directory containing libwebui_ffi.*
# or
export WEBUI_LIB_PATH=/path/to/libwebui_ffi.so     # direct file path
```

## Building from Source

```bash
# Build the native FFI library
cargo build --release -p microsoft-webui-ffi

# Build and test the .NET package
cargo xtask dotnet
```

## License

MIT
