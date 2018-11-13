﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Diagnostics.Runtime.Desktop;
using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;

namespace Microsoft.Diagnostics.Runtime
{
    /// <summary>
    /// Represents information about a single Clr runtime in a process.
    /// </summary>
    [Serializable]
    public class ClrInfo : IComparable
    {
        /// <summary>
        /// The version number of this runtime.
        /// </summary>
        public VersionInfo Version { get { return ModuleInfo.Version; } }

        /// <summary>
        /// The type of CLR this module represents.
        /// </summary>
        public ClrFlavor Flavor { get; private set; }

        /// <summary>
        /// Returns module information about the Dac needed create a ClrRuntime instance for this runtime.
        /// </summary>
        public DacInfo DacInfo { get; private set; }

        /// <summary>
        /// Returns module information about the ClrInstance.
        /// </summary>
        public ModuleInfo ModuleInfo { get; private set; }

        /// <summary>
        /// Returns the location of the local dac on your machine which matches this version of Clr, or null
        /// if one could not be found.
        /// </summary>
        public string LocalMatchingDac { get { return _dacLocation; } }
        
        /// <summary>
        /// Creates a runtime from the given Dac file on disk.
        /// </summary>
        public ClrRuntime CreateRuntime()
        {
            string dac = _dacLocation;
            if (dac != null && !File.Exists(dac))
                dac = null;

            if (dac == null)
                dac = _dataTarget.SymbolLocator.FindBinary(DacInfo);

            if (!File.Exists(dac))
                throw new FileNotFoundException("Could not find matching DAC for this runtime.", DacInfo.FileName);

            if (IntPtr.Size != (int)_dataTarget.DataReader.GetPointerSize())
                throw new InvalidOperationException("Mismatched architecture between this process and the dac.");

            return ConstructRuntime(dac);
        }

        /// <summary>
        /// Creates a runtime from a given IXClrDataProcess interface.  Used for debugger plugins.
        /// </summary>
        public ClrRuntime CreateRuntime(object clrDataProcess)
        {
            DacLibrary lib = new DacLibrary(_dataTarget, DacLibrary.TryGetDacPtr(clrDataProcess));

            // Figure out what version we are on.
            if (lib.GetSOSInterfaceNoAddRef() != null)
            {
                return new V45Runtime(this, _dataTarget, lib);
            }
            else
            {
                byte[] buffer = new byte[Marshal.SizeOf(typeof(V2HeapDetails))];

                int val = lib.InternalDacPrivateInterface.Request(DacRequests.GCHEAPDETAILS_STATIC_DATA, 0, null, (uint)buffer.Length, buffer);
                if ((uint)val == 0x80070057)
                    return new LegacyRuntime(this, _dataTarget, lib, Desktop.DesktopVersion.v4, 10000);
                else
                    return new LegacyRuntime(this, _dataTarget, lib, Desktop.DesktopVersion.v2, 3054);
            }
        }

        /// <summary>
        /// Creates a runtime from the given Dac file on disk.
        /// </summary>
        /// <param name="dacFilename">A full path to the matching mscordacwks for this process.</param>
        /// <param name="ignoreMismatch">Whether or not to ignore mismatches between </param>
        /// <returns></returns>
        public ClrRuntime CreateRuntime(string dacFilename, bool ignoreMismatch = false)
        {
            if (string.IsNullOrEmpty(dacFilename))
                throw new ArgumentNullException("dacFilename");

            if (!File.Exists(dacFilename))
                throw new FileNotFoundException(dacFilename);

            if (!ignoreMismatch)
            {
                DataTarget.PlatformFunctions.GetFileVersion(dacFilename, out int major, out int minor, out int revision, out int patch);
                if (major != Version.Major || minor != Version.Minor || revision != Version.Revision || patch != Version.Patch)
                    throw new InvalidOperationException(string.Format("Mismatched dac. Version: {0}.{1}.{2}.{3}", major, minor, revision, patch));
            }

            return ConstructRuntime(dacFilename);
        }

#pragma warning disable 0618
        private ClrRuntime ConstructRuntime(string dac)
        {
            if (IntPtr.Size != (int)_dataTarget.DataReader.GetPointerSize())
                throw new InvalidOperationException("Mismatched architecture between this process and the dac.");

            if (_dataTarget.IsMinidump)
                _dataTarget.SymbolLocator.PrefetchBinary(ModuleInfo.FileName, (int)ModuleInfo.TimeStamp, (int)ModuleInfo.FileSize);

            DacLibrary lib = new DacLibrary(_dataTarget, dac);

            Desktop.DesktopVersion ver;
            if (Flavor == ClrFlavor.Core)
            {
                return new Desktop.V45Runtime(this, _dataTarget, lib);
            }
            else if (Flavor == ClrFlavor.Native)
            {
                return new Native.NativeRuntime(this, _dataTarget, lib);
            }
            else if (Version.Major == 2)
            {
                ver = Desktop.DesktopVersion.v2;
            }
            else if (Version.Major == 4 && Version.Minor == 0 && Version.Patch < 10000)
            {
                ver = Desktop.DesktopVersion.v4;
            }
            else
            {
                // Assume future versions will all work on the newest runtime version.
                return new Desktop.V45Runtime(this, _dataTarget, lib);
            }

            return new Desktop.LegacyRuntime(this, _dataTarget, lib, ver, Version.Patch);
        }

        /// <summary>
        /// To string.
        /// </summary>
        /// <returns>A version string for this Clr runtime.</returns>
        public override string ToString()
        {
            return Version.ToString();
        }

        internal ClrInfo(DataTargetImpl dt, ClrFlavor flavor, ModuleInfo module, DacInfo dacInfo, string dacLocation)
        {
            Debug.Assert(dacInfo != null);

            Flavor = flavor;
            DacInfo = dacInfo;
            ModuleInfo = module;
            module.IsRuntime = true;
            _dataTarget = dt;
            _dacLocation = dacLocation;
        }

        internal ClrInfo()
        {
        }

        private string _dacLocation;
        private DataTargetImpl _dataTarget;

        /// <summary>
        /// IComparable.  Sorts the object by version.
        /// </summary>
        /// <param name="obj">The object to compare to.</param>
        /// <returns>-1 if less, 0 if equal, 1 if greater.</returns>
        public int CompareTo(object obj)
        {
            if (obj == null)
                return 1;

            if (!(obj is ClrInfo))
                throw new InvalidOperationException("Object not ClrInfo.");

            ClrFlavor flv = ((ClrInfo)obj).Flavor;
            if (flv != Flavor)
                return flv.CompareTo(Flavor);  // Intentionally reversed.

            VersionInfo rhs = ((ClrInfo)obj).Version;
            if (Version.Major != rhs.Major)
                return Version.Major.CompareTo(rhs.Major);


            if (Version.Minor != rhs.Minor)
                return Version.Minor.CompareTo(rhs.Minor);


            if (Version.Revision != rhs.Revision)
                return Version.Revision.CompareTo(rhs.Revision);

            return Version.Patch.CompareTo(rhs.Patch);
        }
    }
}
