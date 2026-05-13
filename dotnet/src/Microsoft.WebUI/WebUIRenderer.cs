// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;

namespace Microsoft.WebUI;

/// <summary>
/// Static helpers for one-shot WebUI rendering (parse + render in a single call).
/// </summary>
public static class WebUIRenderer
{
    /// <summary>
    /// Parses and renders an HTML template with the given JSON state in a single call.
    /// </summary>
    /// <param name="html">The HTML template string to render.</param>
    /// <param name="stateJson">JSON-encoded state for the render.</param>
    /// <returns>The rendered HTML string.</returns>
    /// <exception cref="WebUIException">Thrown when rendering fails.</exception>
    public static string RenderHtml(string html, string stateJson)
    {
        ArgumentNullException.ThrowIfNull(html);
        ArgumentNullException.ThrowIfNull(stateJson);
        return RenderHtmlNative(html, stateJson);
    }

    /// <summary>
    /// Produces a complete JSON partial response for client-side navigation.
    /// </summary>
    /// <param name="protocol">Pre-compiled protocol binary data.</param>
    /// <param name="stateJson">JSON-encoded application state.</param>
    /// <param name="entryId">The persistent entry identifier.</param>
    /// <param name="requestPath">The current route path.</param>
    /// <param name="inventoryHex">Hex-encoded inventory string.</param>
    /// <returns>A JSON string containing state, templates, inventory, path, and chain.</returns>
    /// <exception cref="WebUIException">Thrown when the operation fails.</exception>
    public static string RenderPartial(byte[] protocol, string stateJson, string entryId, string requestPath, string inventoryHex)
    {
        ArgumentNullException.ThrowIfNull(protocol);
        ArgumentNullException.ThrowIfNull(stateJson);
        ArgumentNullException.ThrowIfNull(entryId);
        ArgumentNullException.ThrowIfNull(requestPath);
        ArgumentNullException.ThrowIfNull(inventoryHex);
        return RenderPartialNative(protocol, stateJson, entryId, requestPath, inventoryHex);
    }

    /// <summary>
    /// Renders client template payloads for specific component tags.
    /// </summary>
    /// <param name="protocol">Pre-compiled protocol binary data.</param>
    /// <param name="componentTagsJson">JSON array of component tag names.</param>
    /// <param name="inventoryHex">Hex-encoded inventory string.</param>
    /// <returns>A JSON string containing templates, styles, hrefs, and inventory.</returns>
    /// <exception cref="WebUIException">Thrown when the operation fails.</exception>
    public static string RenderComponentTemplates(byte[] protocol, string componentTagsJson, string inventoryHex)
    {
        ArgumentNullException.ThrowIfNull(protocol);
        ArgumentNullException.ThrowIfNull(componentTagsJson);
        ArgumentNullException.ThrowIfNull(inventoryHex);
        return RenderComponentTemplatesNative(protocol, componentTagsJson, inventoryHex);
    }

    /// <summary>
    /// Extracts the CSS token names embedded in a serialized protocol.
    /// </summary>
    /// <param name="protocol">Pre-compiled protocol binary data.</param>
    /// <returns>The token names without the <c>--</c> prefix.</returns>
    /// <exception cref="WebUIException">Thrown when the operation fails.</exception>
    public static string[] GetProtocolTokens(byte[] protocol)
    {
        ArgumentNullException.ThrowIfNull(protocol);

        IntPtr resultPtr = NativeBindings.webui_protocol_tokens(protocol, (nuint)protocol.Length);

        if (resultPtr == IntPtr.Zero)
        {
            string error = NativeBindings.GetLastError() ?? "GetProtocolTokens failed.";
            throw new WebUIException(error);
        }

        string tokens = NativeBindings.ReadAndFreeString(resultPtr) ?? string.Empty;
        return tokens.Length == 0 ? [] : tokens.Split('\n');
    }

    /// <summary>
    /// Internal helper that performs the native WebUI render call and marshals the result.
    /// </summary>
    /// <param name="html">The HTML template string to render.</param>
    /// <param name="stateJson">JSON-encoded state for the render.</param>
    /// <returns>The rendered HTML string.</returns>
    /// <exception cref="WebUIException">Thrown when rendering fails.</exception>
    private static string RenderHtmlNative(string html, string stateJson)
    {
        IntPtr resultPtr = NativeBindings.webui_render(html, stateJson);

        if (resultPtr == IntPtr.Zero)
        {
            string error = NativeBindings.GetLastError() ?? "RenderHtml failed.";
            throw new WebUIException(error);
        }

        return NativeBindings.ReadAndFreeString(resultPtr)!;
    }

    private static string RenderPartialNative(
        byte[] protocol,
        string stateJson,
        string entryId,
        string requestPath,
        string inventoryHex)
    {
        IntPtr resultPtr = NativeBindings.webui_render_partial(
            protocol,
            (nuint)protocol.Length,
            stateJson,
            entryId,
            requestPath,
            inventoryHex);

        if (resultPtr == IntPtr.Zero)
        {
            string error = NativeBindings.GetLastError() ?? "RenderPartial failed.";
            throw new WebUIException(error);
        }

        return NativeBindings.ReadAndFreeString(resultPtr)!;
    }

    private static string RenderComponentTemplatesNative(
        byte[] protocol,
        string componentTagsJson,
        string inventoryHex)
    {
        IntPtr resultPtr = NativeBindings.webui_render_component_templates(
            protocol,
            (nuint)protocol.Length,
            componentTagsJson,
            inventoryHex);

        if (resultPtr == IntPtr.Zero)
        {
            string error = NativeBindings.GetLastError() ?? "RenderComponentTemplates failed.";
            throw new WebUIException(error);
        }

        return NativeBindings.ReadAndFreeString(resultPtr)!;
    }
}
