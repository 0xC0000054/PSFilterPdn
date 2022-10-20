/////////////////////////////////////////////////////////////////////////////////
//
// Photoshop-compatible filter host Effect plugin for Paint.NET
// https://github.com/0xC0000054/PSFilterPdn
//
// This software is provided under the Microsoft Public License:
//   Copyright (C) 2010-2022 Nicholas Hayes
//
// See LICENSE.txt for complete licensing and attribution information.
//
/////////////////////////////////////////////////////////////////////////////////

using PSFilterLoad.PSApi.Diagnostics;
using System;
using System.Runtime.InteropServices;

namespace PSFilterLoad.PSApi
{
    internal sealed class ResourceSuite : IResourceSuite
    {
        private readonly IHandleSuite handleSuite;
        private readonly IPluginApiLogger logger;
        private CountPIResourcesProc countResourceProc;
        private GetPIResourceProc getResourceProc;
        private DeletePIResourceProc deleteResourceProc;
        private AddPIResourceProc addResourceProc;
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
                    throw new ArgumentNullException("value");
                }

                pseudoResources = value;
            }
        }

        ResourceProcs IResourceSuite.CreateResourceProcs()
        {
            return new ResourceProcs
            {
                resourceProcsVersion = PSConstants.kCurrentResourceProcsVersion,
                numResourceProcs = PSConstants.kCurrentResourceProcsCount,
                addProc = Marshal.GetFunctionPointerForDelegate(addResourceProc),
                countProc = Marshal.GetFunctionPointerForDelegate(countResourceProc),
                deleteProc = Marshal.GetFunctionPointerForDelegate(deleteResourceProc),
                getProc = Marshal.GetFunctionPointerForDelegate(getResourceProc)
            };
        }

        public IntPtr CreateResourceProcsPointer()
        {
            IntPtr resourceProcsPtr = Memory.Allocate(Marshal.SizeOf<ResourceProcs>(), true);

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
            logger.Log(PluginApiLogCategory.ResourceSuite, new FourCCAsStringFormatter(ofType));

            int size = handleSuite.GetHandleSize(data);
            try
            {
                byte[] bytes = new byte[size];

                if (size > 0)
                {
                    Marshal.Copy(handleSuite.LockHandle(data), bytes, 0, size);
                    handleSuite.UnlockHandle(data);
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
                    Marshal.Copy(data, 0, handleSuite.LockHandle(h), data.Length);
                    handleSuite.UnlockHandle(h);
                }

                return h;
            }

            return Handle.Null;
        }
    }
}
