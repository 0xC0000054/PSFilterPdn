﻿/////////////////////////////////////////////////////////////////////////////////
//
// Photoshop-compatible filter host Effect plugin for Paint.NET
// https://github.com/0xC0000054/PSFilterPdn
//
// This software is provided under the Microsoft Public License:
//   Copyright (C) 2010-2021 Nicholas Hayes
//
// See LICENSE.txt for complete licensing and attribution information.
//
/////////////////////////////////////////////////////////////////////////////////

using System;
using System.Runtime.InteropServices;

namespace PSFilterLoad.PSApi
{
    internal sealed class ResourceSuite
    {
        private CountPIResourcesProc countResourceProc;
        private GetPIResourceProc getResourceProc;
        private DeletePIResourceProc deleteResourceProc;
        private AddPIResourceProc addResourceProc;
        private PseudoResourceCollection pseudoResources;

        public ResourceSuite()
        {
            countResourceProc = new CountPIResourcesProc(CountResource);
            addResourceProc = new AddPIResourceProc(AddResource);
            deleteResourceProc = new DeletePIResourceProc(DeleteResource);
            getResourceProc = new GetPIResourceProc(GetResource);
            pseudoResources = new PseudoResourceCollection();
        }

        public PseudoResourceCollection PseudoResources
        {
            get => pseudoResources;
            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException("value");
                }

                pseudoResources = value;
            }
        }

        public IntPtr CreateResourceProcsPointer()
        {
            IntPtr resourceProcsPtr = Memory.Allocate(Marshal.SizeOf(typeof(ResourceProcs)), true);

            unsafe
            {
                ResourceProcs* resourceProcs = (ResourceProcs*)resourceProcsPtr.ToPointer();
                resourceProcs->resourceProcsVersion = PSConstants.kCurrentResourceProcsVersion;
                resourceProcs->numResourceProcs = PSConstants.kCurrentResourceProcsCount;
                resourceProcs->addProc = Marshal.GetFunctionPointerForDelegate(addResourceProc);
                resourceProcs->countProc = Marshal.GetFunctionPointerForDelegate(countResourceProc);
                resourceProcs->deleteProc = Marshal.GetFunctionPointerForDelegate(deleteResourceProc);
                resourceProcs->getProc = Marshal.GetFunctionPointerForDelegate(getResourceProc);
            }

            return resourceProcsPtr;
        }

        private short AddResource(uint ofType, Handle data)
        {
#if DEBUG
            DebugUtils.Ping(DebugFlags.ResourceSuite, DebugUtils.PropToString(ofType));
#endif
            int size = HandleSuite.Instance.GetHandleSize(data);
            try
            {
                byte[] bytes = new byte[size];

                if (size > 0)
                {
                    Marshal.Copy(HandleSuite.Instance.LockHandle(data, 0), bytes, 0, size);
                    HandleSuite.Instance.UnlockHandle(data);
                }

                int index = CountResource(ofType) + 1;
                pseudoResources.Add(new PseudoResource(ofType, index, bytes));
            }
            catch (OutOfMemoryException)
            {
                return PSError.memFullErr;
            }

            return PSError.noErr;
        }

        private short CountResource(uint ofType)
        {
#if DEBUG
            DebugUtils.Ping(DebugFlags.ResourceSuite, DebugUtils.PropToString(ofType));
#endif
            short count = 0;

            foreach (PseudoResource item in pseudoResources)
            {
                if (item.Equals(ofType))
                {
                    count++;
                }
            }

            return count;
        }

        private void DeleteResource(uint ofType, short index)
        {
#if DEBUG
            DebugUtils.Ping(DebugFlags.ResourceSuite, string.Format("{0}, {1}", DebugUtils.PropToString(ofType), index));
#endif
            int resourceIndex = pseudoResources.FindIndex(ofType, index);

            if (resourceIndex >= 0)
            {
                pseudoResources.RemoveAt(resourceIndex);

                int i = index + 1;

                while (true)
                {
                    // Renumber the index of subsequent items.
                    int next = pseudoResources.FindIndex(ofType, i);

                    if (next < 0)
                    {
                        break;
                    }

                    pseudoResources[next].Index = i - 1;

                    i++;
                }
            }
        }

        private Handle GetResource(uint ofType, short index)
        {
#if DEBUG
            DebugUtils.Ping(DebugFlags.ResourceSuite, string.Format("{0}, {1}", DebugUtils.PropToString(ofType), index));
#endif
            PseudoResource res = pseudoResources.Find(ofType, index);

            if (res != null)
            {
                byte[] data = res.GetData();

                Handle h = HandleSuite.Instance.NewHandle(data.Length);
                if (h != Handle.Null)
                {
                    Marshal.Copy(data, 0, HandleSuite.Instance.LockHandle(h, 0), data.Length);
                    HandleSuite.Instance.UnlockHandle(h);
                }

                return h;
            }

            return Handle.Null;
        }
    }
}
