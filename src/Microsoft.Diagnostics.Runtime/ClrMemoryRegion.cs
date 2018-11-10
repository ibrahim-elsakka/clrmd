﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.Diagnostics.Runtime
{    /// <summary>
     /// Types of memory regions in a Clr process.
     /// </summary>
    public enum ClrMemoryRegionType
    {
        // Loader heaps
        /// <summary>
        /// Data on the loader heap.
        /// </summary>
        LowFrequencyLoaderHeap,

        /// <summary>
        /// Data on the loader heap.
        /// </summary>
        HighFrequencyLoaderHeap,

        /// <summary>
        /// Data on the stub heap.
        /// </summary>
        StubHeap,

        // Virtual Call Stub heaps
        /// <summary>
        /// Clr implementation detail (this is here to allow you to distinguish from other
        /// heap types).
        /// </summary>
        IndcellHeap,
        /// <summary>
        /// Clr implementation detail (this is here to allow you to distinguish from other
        /// heap types).
        /// </summary>
        LookupHeap,
        /// <summary>
        /// Clr implementation detail (this is here to allow you to distinguish from other
        /// heap types).
        /// </summary>
        ResolveHeap,
        /// <summary>
        /// Clr implementation detail (this is here to allow you to distinguish from other
        /// heap types).
        /// </summary>
        DispatchHeap,
        /// <summary>
        /// Clr implementation detail (this is here to allow you to distinguish from other
        /// heap types).
        /// </summary>
        CacheEntryHeap,

        // Other regions
        /// <summary>
        /// Heap for JIT code data.
        /// </summary>
        JitHostCodeHeap,
        /// <summary>
        /// Heap for JIT loader data.
        /// </summary>
        JitLoaderCodeHeap,
        /// <summary>
        /// Heap for module jump thunks.
        /// </summary>
        ModuleThunkHeap,
        /// <summary>
        /// Heap for module lookup tables.
        /// </summary>
        ModuleLookupTableHeap,

        /// <summary>
        /// A segment on the GC heap (committed memory).
        /// </summary>
        GCSegment,

        /// <summary>
        /// A segment on the GC heap (reserved, but not committed, memory).
        /// </summary>
        ReservedGCSegment,

        /// <summary>
        /// A portion of Clr's handle table.
        /// </summary>
        HandleTableChunk
    }
    
    /// <summary>
    /// Represents a region of memory in the process which Clr allocated and controls.
    /// </summary>
    public abstract class ClrMemoryRegion
    {
        /// <summary>
        /// The start address of the memory region.
        /// </summary>
        public ulong Address { get; set; }

        /// <summary>
        /// The size of the memory region in bytes.
        /// </summary>
        public ulong Size { get; set; }

        /// <summary>
        /// The type of heap/memory that the region contains.
        /// </summary>
        public ClrMemoryRegionType Type { get; set; }

        /// <summary>
        /// The AppDomain pointer that corresponds to this heap.  You can obtain the
        /// name of the AppDomain index or name by calling the appropriate function
        /// on RuntimeBase.
        /// Note:  HasAppDomainData must be true before getting this property.
        /// </summary>
        abstract public ClrAppDomain AppDomain { get; }

        /// <summary>
        /// The Module pointer that corresponds to this heap.  You can obtain the
        /// filename of the module with this property.
        /// Note:  HasModuleData must be true or this property will be null.
        /// </summary>
        abstract public string Module { get; }

        /// <summary>
        /// Returns the heap number associated with this data.  Returns -1 if no
        /// GC heap is associated with this memory region.
        /// </summary>
        abstract public int HeapNumber { get; set; }

        /// <summary>
        /// Returns the gc segment type associated with this data.  Only callable if
        /// HasGCHeapData is true.
        /// </summary>
        abstract public GCSegmentType GCSegmentType { get; set; }

        /// <summary>
        /// Returns a string describing the region of memory (for example "JIT Code Heap"
        /// or "GC Segment").
        /// </summary>
        /// <param name="detailed">Whether or not to include additional data such as the module,
        /// AppDomain, or GC Heap associaed with it.</param>
        abstract public string ToString(bool detailed);

        /// <summary>
        /// Equivalent to GetDisplayString(false).
        /// </summary>
        public override string ToString()
        {
            return ToString(false);
        }
    }
}
