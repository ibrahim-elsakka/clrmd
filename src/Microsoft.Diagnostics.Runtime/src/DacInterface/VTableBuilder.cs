﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;

namespace Microsoft.Diagnostics.Runtime.DacInterface
{
    internal unsafe class VTableBuilder
    {
        private readonly Guid _guid;
        private readonly COMCallableIUnknown _wrapper;
        private readonly List<Delegate> _delegates = new List<Delegate>();

        public VTableBuilder(COMCallableIUnknown wrapper, Guid guid)
        {
            _guid = guid;
            _wrapper = wrapper;
        }

        public void AddMethod(Delegate func)
        {
#if DEBUG
            if (func.Method.GetParameters().First().ParameterType != typeof(IntPtr))
                throw new InvalidOperationException();

            object[] attrs = func.GetType().GetCustomAttributes(false);
            if (attrs.Count(c => c is UnmanagedFunctionPointerAttribute) != 1)
                throw new InvalidOperationException();

            if (func.Method.ReturnType != typeof(int))
                throw new InvalidOperationException();
#endif

            _delegates.Add(func);
        }

        internal IntPtr Complete()
        {
            IntPtr obj = Marshal.AllocHGlobal(IntPtr.Size);

            int vtablePartSize = _delegates.Count * IntPtr.Size;
            IntPtr* vtable = (IntPtr*)Marshal.AllocHGlobal(vtablePartSize + sizeof(IUnknownVTable));
            *(void**)obj = vtable;

            IUnknownVTable iunk = _wrapper.IUnknown;
            *vtable++ = iunk.QueryInterface;
            *vtable++ = iunk.AddRef;
            *vtable++ = iunk.Release;

            foreach (Delegate d in _delegates)
                *vtable++ = Marshal.GetFunctionPointerForDelegate(d);

            _wrapper.RegisterInterface(_guid, obj, _delegates);
            return obj;
        }
    }
}