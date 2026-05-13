// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Xunit;

namespace Microsoft.WebUI.Tests;

public class WebUIRendererTests
{
    private static readonly byte[] InvalidProtocol = [0x01, 0x02, 0x03];

    [Fact]
    public void RenderHtml_SimpleTemplate_ReturnsRenderedOutput()
    {
        // This test requires the native library to be built.
        // Run: cargo build --release -p webui-ffi
        // Then set WEBUI_LIB_PATH to the target/release directory.
        var html = "<div>Hello, {{name}}!</div>";
        var json = "{\"name\": \"World\"}";
        var result = WebUIRenderer.RenderHtml(html, json);
        Assert.Contains("Hello, World!", result);
    }

    [Fact]
    public void RenderHtml_EmptyState_ReturnsTemplate()
    {
        var html = "<p>Static content</p>";
        var result = WebUIRenderer.RenderHtml(html, "{}");
        Assert.Contains("Static content", result);
    }

    [Fact]
    public void RenderPartial_InvalidProtocol_ThrowsWebUIException()
    {
        Assert.Throws<WebUIException>(() =>
            WebUIRenderer.RenderPartial(InvalidProtocol, "{}", "index.html", "/", string.Empty));
    }

    [Fact]
    public void RenderComponentTemplates_InvalidProtocol_ThrowsWebUIException()
    {
        Assert.Throws<WebUIException>(() =>
            WebUIRenderer.RenderComponentTemplates(InvalidProtocol, "[]", string.Empty));
    }

    [Fact]
    public void GetProtocolTokens_InvalidProtocol_ThrowsWebUIException()
    {
        Assert.Throws<WebUIException>(() =>
            WebUIRenderer.GetProtocolTokens(InvalidProtocol));
    }
}
