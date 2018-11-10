﻿using System;
using System.Runtime.InteropServices;

namespace Microsoft.Diagnostics.Runtime
{
    internal sealed class LinuxFunctions : PlatformFunctions
    {
        public override bool GetFileVersion(string dll, out int major, out int minor, out int revision, out int patch)
        {
            //TODO

            major = minor = revision = patch = 0;
            return true;
        }

        public override bool TryGetWow64(IntPtr proc, out bool result)
        {
            result = false;
            return true;
        }

        public override IntPtr LoadLibrary(string filename) => dlopen(filename, RTLD_NOW);

        public override bool FreeLibrary(IntPtr module) => dlclose(module) == 0;

        public override IntPtr GetProcAddress(IntPtr module, string method) => dlsym(module, method);

        [DllImport("libdl.so")]
        static extern IntPtr dlopen(string filename, int flags);
        [DllImport("libdl.so")]
        static extern int dlclose(IntPtr module);

        [DllImport("libdl.so")]
        static extern IntPtr dlsym(IntPtr handle, string symbol);

        const int RTLD_NOW = 2;
    }
}
