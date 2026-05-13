// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Microsoft.WebUI;

/// <summary>
/// Internal P/Invoke bindings to the native <c>webui_ffi</c> library.
/// </summary>
internal static partial class NativeBindings
{
    private const string LibName = "webui_ffi";

    /// <summary>
    /// SafeHandle wrapper for a native <c>webui_handler</c> pointer.
    /// </summary>
    internal sealed class WebUIHandlerSafeHandle : SafeHandle
    {
        internal WebUIHandlerSafeHandle()
            : base(IntPtr.Zero, ownsHandle: true)
        {
        }

        internal WebUIHandlerSafeHandle(IntPtr handle)
            : this()
        {
            SetHandle(handle);
        }

        public override bool IsInvalid => handle == IntPtr.Zero;

        protected override bool ReleaseHandle()
        {
            webui_handler_destroy_raw(handle);
            return true;
        }
    }

    static NativeBindings()
    {
        NativeLibrary.SetDllImportResolver(
            typeof(NativeBindings).Assembly,
            ResolveNativeLibrary);
    }

    private static IntPtr ResolveNativeLibrary(
        string libraryName,
        Assembly assembly,
        DllImportSearchPath? searchPath)
    {
        if (libraryName != LibName)
        {
            return IntPtr.Zero;
        }

        IntPtr handle;

        // Allow overriding the native library path via environment variable.
        string? customPath = Environment.GetEnvironmentVariable("WEBUI_LIB_PATH");
        if (!string.IsNullOrEmpty(customPath))
        {
            if (NativeLibrary.TryLoad(customPath, out handle))
            {
                return handle;
            }

            if (Directory.Exists(customPath))
            {
                string candidatePath = Path.Combine(customPath, GetPlatformLibraryFileName());
                if (NativeLibrary.TryLoad(candidatePath, out handle))
                {
                    return handle;
                }
            }
        }

        // Fall back to default resolution.
        if (NativeLibrary.TryLoad(LibName, assembly, searchPath, out handle))
        {
            return handle;
        }

        return IntPtr.Zero;
    }

    private static string GetPlatformLibraryFileName()
    {
        if (OperatingSystem.IsWindows())
        {
            return "webui_ffi.dll";
        }

        if (OperatingSystem.IsMacOS())
        {
            return "libwebui_ffi.dylib";
        }

        return "libwebui_ffi.so";
    }

    [LibraryImport(LibName, EntryPoint = "webui_handler_create")]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    private static partial IntPtr webui_handler_create_raw();

    [LibraryImport(LibName, EntryPoint = "webui_handler_create_with_plugin", StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    private static partial IntPtr webui_handler_create_with_plugin_raw(string? pluginId);

    [LibraryImport(LibName, EntryPoint = "webui_handler_destroy")]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    private static partial void webui_handler_destroy_raw(IntPtr handlerPtr);

    [LibraryImport(LibName, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    internal static partial void webui_handler_set_nonce(
        WebUIHandlerSafeHandle handlerPtr,
        string? nonce);

    [LibraryImport(LibName, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    internal static partial IntPtr webui_handler_render(
        WebUIHandlerSafeHandle handlerPtr,
        [In] byte[] protocolData,
        nuint protocolLen,
        string dataJson,
        string entryId,
        string requestPath);

    [LibraryImport(LibName, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    internal static partial IntPtr webui_render(string html, string dataJson);

    [LibraryImport(LibName, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    internal static partial IntPtr webui_render_partial(
        [In] byte[] protocolData,
        nuint protocolLen,
        string stateJson,
        string entryId,
        string requestPath,
        string inventoryHex);

    [LibraryImport(LibName, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    internal static partial IntPtr webui_render_component_templates(
        [In] byte[] protocolData,
        nuint protocolLen,
        string componentTagsJson,
        string inventoryHex);

    internal static WebUIHandlerSafeHandle CreateHandler(string? pluginId)
    {
        IntPtr handle = pluginId is null
            ? webui_handler_create_raw()
            : webui_handler_create_with_plugin_raw(pluginId);
        return new WebUIHandlerSafeHandle(handle);
    }

    [LibraryImport(LibName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    internal static partial void webui_free(IntPtr stringPtr);

    [LibraryImport(LibName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    internal static partial IntPtr webui_last_error();

    [LibraryImport(LibName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    internal static partial IntPtr webui_protocol_tokens(
        [In] byte[] protocolData,
        nuint protocolLen);

    /// <summary>
    /// Reads a UTF-8 string from a native pointer and frees the native memory.
    /// Returns <c>null</c> if the pointer is <see cref="System.IntPtr.Zero"/>.
    /// </summary>
    internal static string? ReadAndFreeString(IntPtr ptr)
    {
        if (ptr == IntPtr.Zero)
        {
            return null;
        }

        try
        {
            return Marshal.PtrToStringUTF8(ptr);
        }
        finally
        {
            webui_free(ptr);
        }
    }

    /// <summary>
    /// Reads the last error message from the native library.
    /// Returns <c>null</c> if there is no error.
    /// </summary>
    internal static string? GetLastError()
    {
        IntPtr errorPtr = webui_last_error();
        if (errorPtr == IntPtr.Zero)
        {
            return null;
        }

        return Marshal.PtrToStringUTF8(errorPtr);
    }
}
