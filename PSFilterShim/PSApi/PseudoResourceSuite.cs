/////////////////////////////////////////////////////////////////////////////////
//
// Photoshop-compatible filter host Effect plugin for Paint.NET
// https://github.com/0xC0000054/PSFilterPdn
//
// This software is provided under the Microsoft Public License:
//   Copyright (C) 2010-2017 Nicholas Hayes
//
// See LICENSE.txt for complete licensing and attribution information.
//
/////////////////////////////////////////////////////////////////////////////////

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace PSFilterLoad.PSApi
{
	internal sealed class PseudoResourceSuite
	{
		private CountPIResourcesProc countResourceProc;
		private GetPIResourceProc getResourceProc;
		private DeletePIResourceProc deleteResourceProc;
		private AddPIResourceProc addResourceProc;
		private List<PSResource> pseudoResources;

		public PseudoResourceSuite()
		{
			this.countResourceProc = new CountPIResourcesProc(CountResource);
			this.addResourceProc = new AddPIResourceProc(AddResource);
			this.deleteResourceProc = new DeletePIResourceProc(DeleteResource);
			this.getResourceProc = new GetPIResourceProc(GetResource);
			this.pseudoResources = new List<PSResource>();
		}

		public List<PSResource> PseudoResources
		{
			get
			{
				return this.pseudoResources;
			}
			set
			{
				if (value == null)
				{
					throw new ArgumentNullException("value");
				}

				this.pseudoResources = value;
			}
		}

		public IntPtr CreateResourceProcs()
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

		private short AddResource(uint ofType, IntPtr data)
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
				this.pseudoResources.Add(new PSResource(ofType, index, bytes));
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

			foreach (var item in this.pseudoResources)
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
			int resourceIndex = this.pseudoResources.FindIndex(delegate (PSResource r)
			{
				return r.Equals(ofType, index);
			});

			if (resourceIndex >= 0)
			{
				this.pseudoResources.RemoveAt(resourceIndex);

				int i = index + 1;

				while (true)
				{
					// Renumber the index of subsequent items.
					int next = this.pseudoResources.FindIndex(delegate (PSResource r)
					{
						return r.Equals(ofType, i);
					});

					if (next < 0) break;

					this.pseudoResources[next].Index = i - 1;

					i++;
				}
			}
		}

		private IntPtr GetResource(uint ofType, short index)
		{
#if DEBUG
			DebugUtils.Ping(DebugFlags.ResourceSuite, string.Format("{0}, {1}", DebugUtils.PropToString(ofType), index));
#endif
			PSResource res = this.pseudoResources.Find(delegate (PSResource r)
			{
				return r.Equals(ofType, index);
			});

			if (res != null)
			{
				byte[] data = res.GetData();

				IntPtr h = HandleSuite.Instance.NewHandle(data.Length);
				if (h != IntPtr.Zero)
				{
					Marshal.Copy(data, 0, HandleSuite.Instance.LockHandle(h, 0), data.Length);
					HandleSuite.Instance.UnlockHandle(h);
				}

				return h;
			}

			return IntPtr.Zero;
		}
	}
}
