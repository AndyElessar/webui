// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Threading;

namespace Microsoft.WebUI;

/// <summary>
/// Manages a WebUI handler instance for protocol-based rendering.
/// Use this for a single full-page render with pre-compiled protocol data.
/// <para>This type wraps a native handler instance and is not thread-safe.
/// Create and dispose a new instance per request, especially when setting a
/// CSP nonce for that response.</para>
/// </summary>
public sealed class WebUIHandler : IDisposable
{
    private readonly NativeBindings.WebUIHandlerSafeHandle _handle;
    private volatile int _disposed;

    /// <summary>
    /// Creates a new WebUI handler instance.
    /// </summary>
    /// <param name="plugin">Optional plugin identifier. See the WebUI documentation for available plugin identifiers.</param>
    /// <exception cref="WebUIException">Thrown when the native handler cannot be created.</exception>
    public WebUIHandler(string? plugin = null)
    {
        string? normalizedPlugin = string.IsNullOrWhiteSpace(plugin) ? null : plugin.Trim();

        _handle = NativeBindings.CreateHandler(normalizedPlugin);

        if (_handle.IsInvalid)
        {
            string error = NativeBindings.GetLastError() ?? "Failed to create WebUI handler.";
            _handle.Dispose();
            throw new WebUIException(error);
        }
    }

    /// <summary>
    /// Renders the given protocol data with the specified state, entry, and request path.
    /// </summary>
    /// <param name="protocol">Pre-compiled protocol binary data.</param>
    /// <param name="stateJson">JSON-encoded state for the render.</param>
    /// <param name="entryId">The entry identifier to render.</param>
    /// <param name="requestPath">The HTTP request path.</param>
    /// <returns>The rendered HTML string.</returns>
    /// <exception cref="ObjectDisposedException">Thrown when the handler has been disposed.</exception>
    /// <exception cref="WebUIException">Thrown when rendering fails.</exception>
    public string Render(byte[] protocol, string stateJson, string entryId, string requestPath)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(protocol);
        ArgumentNullException.ThrowIfNull(stateJson);
        ArgumentNullException.ThrowIfNull(entryId);
        ArgumentNullException.ThrowIfNull(requestPath);

        IntPtr resultPtr = NativeBindings.webui_handler_render(
            _handle,
            protocol,
            (nuint)protocol.Length,
            stateJson,
            entryId,
            requestPath);

        if (resultPtr == IntPtr.Zero)
        {
            string error = NativeBindings.GetLastError() ?? "Render failed.";
            throw new WebUIException(error);
        }

        return NativeBindings.ReadAndFreeString(resultPtr)!;
    }

    /// <summary>
    /// Sets or clears the CSP nonce used for inline tags emitted by this handler instance.
    /// </summary>
    /// <param name="nonce">The per-request nonce value to emit, or <c>null</c> to clear it.</param>
    /// <exception cref="ObjectDisposedException">Thrown when the handler has been disposed.</exception>
    /// <exception cref="WebUIException">Thrown when the operation fails.</exception>
    public void SetNonce(string? nonce)
    {
        ThrowIfDisposed();

        NativeBindings.webui_handler_set_nonce(_handle, nonce);

        string? error = NativeBindings.GetLastError();
        if (error is not null)
        {
            throw new WebUIException(error);
        }
    }

    /// <summary>
    /// Produces a complete JSON partial response for client-side navigation.
    /// Combines application state, route templates, inventory, request path, and
    /// matched route chain in a single call — no assembly required.
    /// </summary>
    /// <param name="protocol">Pre-compiled protocol binary data.</param>
    /// <param name="stateJson">JSON-encoded application state.</param>
    /// <param name="entryId">The persistent entry identifier.</param>
    /// <param name="requestPath">The current route path.</param>
    /// <param name="inventoryHex">Hex-encoded inventory string.</param>
    /// <returns>A JSON string containing state, templates, inventory, path, and chain.</returns>
    /// <exception cref="ObjectDisposedException">Thrown when the handler has been disposed.</exception>
    /// <exception cref="WebUIException">Thrown when the operation fails.</exception>
    public string RenderPartial(byte[] protocol, string stateJson, string entryId, string requestPath, string inventoryHex)
    {
        ThrowIfDisposed();
        return WebUIRenderer.RenderPartial(protocol, stateJson, entryId, requestPath, inventoryHex);
    }

    /// <summary>
    /// Releases the native handler resources.
    /// </summary>
    public void Dispose()
    {
        if (Interlocked.CompareExchange(ref _disposed, 1, 0) != 0)
        {
            return;
        }

        _handle.Dispose();
    }

    private void ThrowIfDisposed()
    {
        ObjectDisposedException.ThrowIf(_disposed != 0 || _handle.IsClosed || _handle.IsInvalid, this);
    }
}
