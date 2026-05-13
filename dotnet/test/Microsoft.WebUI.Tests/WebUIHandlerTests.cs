// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using Xunit;

namespace Microsoft.WebUI.Tests;

public class WebUIHandlerTests
{
    [Fact]
    public void Handler_CreateAndDispose_DoesNotThrow()
    {
        using var handler = new WebUIHandler();
        // Handler created successfully — dispose should clean up
    }

    [Fact]
    public void Handler_CreateWithPlugin_DoesNotThrow()
    {
        using var handler = new WebUIHandler("fast");
    }

    [Fact]
    public void Handler_DoubleDispose_DoesNotThrow()
    {
        var handler = new WebUIHandler();
        handler.Dispose();
        handler.Dispose(); // Should not throw
    }

    [Fact]
    public void Handler_SetNonce_DoesNotThrow()
    {
        using var handler = new WebUIHandler();
        handler.SetNonce("nonce-value");
    }

    [Fact]
    public void Handler_ClearNonce_DoesNotThrow()
    {
        using var handler = new WebUIHandler();
        handler.SetNonce(null);
    }

    [Fact]
    public void Handler_RequestScopedNonceUsage_AllowsSeparateHandlers()
    {
        using var firstRequestHandler = new WebUIHandler();
        firstRequestHandler.SetNonce("first-request-nonce");

        using var secondRequestHandler = new WebUIHandler();
        secondRequestHandler.SetNonce("second-request-nonce");
    }

    [Fact]
    public void Handler_RenderAfterDispose_ThrowsObjectDisposedException()
    {
        var handler = new WebUIHandler();
        handler.Dispose();

        Assert.Throws<ObjectDisposedException>(() =>
            handler.Render(Array.Empty<byte>(), "{}", "index.html", "/"));
    }

    [Fact]
    public void Handler_SetNonceAfterDispose_ThrowsObjectDisposedException()
    {
        var handler = new WebUIHandler();
        handler.Dispose();

        Assert.Throws<ObjectDisposedException>(() => handler.SetNonce("nonce-value"));
    }
}
