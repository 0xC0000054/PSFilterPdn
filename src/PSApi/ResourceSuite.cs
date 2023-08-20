/////////////////////////////////////////////////////////////////////////////////
//
// Photoshop-compatible filter host Effect plugin for Paint.NET
// https://github.com/0xC0000054/PSFilterPdn
//
// This software is provided under the Microsoft Public License:
//   Copyright (C) 2010-2023 Nicholas Hayes
//
// See LICENSE.txt for complete licensing and attribution information.
//
/////////////////////////////////////////////////////////////////////////////////

using PSFilterLoad.PSApi.Diagnostics;
using System;
using System.Runtime.InteropServices;

namespace PSFilterLoad.PSApi
{
    internal sealed class ResourceSuite : IPICASuiteAllocator
    {
        private readonly IHandleSuite handleSuite;
        private readonly IPluginApiLogger logger;
        private readonly CountPIResourcesProc countResourceProc;
        private readonly GetPIResourceProc getResourceProc;
        private readonly DeletePIResourceProc deleteResourceProc;
        private readonly AddPIResourceProc addResourceProc;
        private PseudoResourceCollection pseudoResources;

        public ResourceSuite(IHandleSuite handleSuite, IPluginApiLogger logger)
        {
            ArgumentNullException.ThrowIfNull(handleSuite);
            ArgumentNullException.ThrowIfNull(logger);

            this.handleSuite = handleSuite;
            this.logger = logger;
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
                    throw new ArgumentNullException(nameof(value));
                }

                pseudoResources = value;
            }
        }

        IntPtr IPICASuiteAllocator.Allocate(int version)
        {
            if (version != PSConstants.kCurrentResourceProcsVersion)
            {
                throw new UnsupportedPICASuiteVersionException(PSConstants.PICA.ResourceSuite, version);
            }

            return CreateResourceProcsPointer();
        }

        bool IPICASuiteAllocator.IsSupportedVersion(int version) => version == PSConstants.kCurrentResourceProcsVersion;

        public IntPtr CreateResourceProcsPointer()
        {
            IntPtr resourceProcsPtr = Memory.Allocate(Marshal.SizeOf<ResourceProcs>(), MemoryAllocationOptions.ZeroFill);

            unsafe
            {
                ResourceProcs* resourceProcs = (ResourceProcs*)resourceProcsPtr.ToPointer();
                resourceProcs->resourceProcsVersion = PSConstants.kCurrentResourceProcsVersion;
                resourceProcs->numResourceProcs = PSConstants.kCurrentResourceProcsCount;
                resourceProcs->addProc = new UnmanagedFunctionPointer<AddPIResourceProc>(addResourceProc);
                resourceProcs->countProc = new UnmanagedFunctionPointer<CountPIResourcesProc>(countResourceProc);
                resourceProcs->deleteProc = new UnmanagedFunctionPointer<DeletePIResourceProc>(deleteResourceProc);
                resourceProcs->getProc = new UnmanagedFunctionPointer<GetPIResourceProc>(getResourceProc);
            }

            return resourceProcsPtr;
        }

        private short AddResource(uint ofType, Handle data)
        {
            logger.Log(PluginApiLogCategory.ResourceSuite, new FourCCAsStringFormatter(ofType));

            int size = handleSuite.GetHandleSize(data);
            try
            {
                byte[] bytes = new byte[size];

                if (size > 0)
                {
                    using (HandleSuiteLock handleSuiteLock = handleSuite.LockHandle(data))
                    {
                        handleSuiteLock.Data.CopyTo(bytes);
                    }
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
            logger.Log(PluginApiLogCategory.ResourceSuite, new FourCCAsStringFormatter(ofType));

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
            logger.Log(PluginApiLogCategory.ResourceSuite, "{0}, {1}", new FourCCAsStringFormatter(ofType), index);

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
            logger.Log(PluginApiLogCategory.ResourceSuite, "{0}, {1}", new FourCCAsStringFormatter(ofType), index);

            PseudoResource res = pseudoResources.Find(ofType, index);

            if (res != null)
            {
                byte[] data = res.GetData();

                Handle h = handleSuite.NewHandle(data.Length);
                if (h != Handle.Null)
                {
                    using (HandleSuiteLock handleSuiteLock = handleSuite.LockHandle(h))
                    {
                        data.CopyTo(handleSuiteLock.Data);
                    }
                }

                return h;
            }

            return Handle.Null;
        }
    }
}
