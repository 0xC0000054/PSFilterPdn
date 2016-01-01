/////////////////////////////////////////////////////////////////////////////////
//
// Photoshop-compatible filter host Effect plugin for Paint.NET
// http://psfilterpdn.codeplex.com/
//
// This software is provided under the Microsoft Public License:
//   Copyright (C) 2010-2016 Nicholas Hayes
// 
// See LICENSE.txt for complete licensing and attribution information.
//
/////////////////////////////////////////////////////////////////////////////////

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace PSFilterLoad.PSApi
{
	internal sealed class BufferSuite
	{
		private class BufferSuiteSingleton
		{
			// Explicit static constructor to tell C# compiler
			// not to mark type as beforefieldinit
			static BufferSuiteSingleton()
			{
			}

			private BufferSuiteSingleton()
			{
			}

			internal static readonly BufferSuite Instance = new BufferSuite();
		}

		private readonly AllocateBufferProc allocProc;
		private readonly FreeBufferProc freeProc;
		private readonly LockBufferProc lockProc;
		private readonly UnlockBufferProc unlockProc;
		private readonly BufferSpaceProc spaceProc;
		private readonly List<IntPtr> bufferIDs;

		private BufferSuite()
		{
			this.allocProc = new AllocateBufferProc(AllocateBufferProc);
			this.freeProc = new FreeBufferProc(BufferFreeProc);
			this.lockProc = new LockBufferProc(BufferLockProc);
			this.unlockProc = new UnlockBufferProc(BufferUnlockProc);
			this.spaceProc = new BufferSpaceProc(BufferSpaceProc);
			this.bufferIDs = new List<IntPtr>();
		}

		public static BufferSuite Instance
		{
			get
			{
				return BufferSuiteSingleton.Instance;
			}
		}

		public int AvailableSpace
		{
			get
			{
				return BufferSpaceProc();
			}
		}

		public bool AllocatedBySuite(IntPtr buffer)
		{
			return this.bufferIDs.Contains(buffer);
		}

		public IntPtr CreateBufferProcs()
		{
			IntPtr bufferProcsPtr = Memory.Allocate(Marshal.SizeOf(typeof(BufferProcs)), true);

			unsafe
			{
				BufferProcs* bufferProcs = (BufferProcs*)bufferProcsPtr.ToPointer();
				bufferProcs->bufferProcsVersion = PSConstants.kCurrentBufferProcsVersion;
				bufferProcs->numBufferProcs = PSConstants.kCurrentBufferProcsCount;
				bufferProcs->allocateProc = Marshal.GetFunctionPointerForDelegate(this.allocProc);
				bufferProcs->freeProc = Marshal.GetFunctionPointerForDelegate(this.freeProc);
				bufferProcs->lockProc = Marshal.GetFunctionPointerForDelegate(this.lockProc);
				bufferProcs->unlockProc = Marshal.GetFunctionPointerForDelegate(this.unlockProc);
				bufferProcs->spaceProc = Marshal.GetFunctionPointerForDelegate(this.spaceProc);
			}

			return bufferProcsPtr;
		}

		public void FreeBuffer(IntPtr bufferID)
		{
			BufferUnlockProc(bufferID);
			BufferFreeProc(bufferID);
		}

		public void FreeRemainingBuffers()
		{
			for (int i = 0; i < this.bufferIDs.Count; i++)
			{
				Memory.Free(this.bufferIDs[i]);
			}
			this.bufferIDs.Clear();
		}

		public long GetBufferSize(IntPtr bufferID)
		{
			return Memory.Size(bufferID);
		}

		private short AllocateBufferProc(int size, ref IntPtr bufferID)
		{
#if DEBUG
			DebugUtils.Ping(DebugFlags.BufferSuite, string.Format("Size: {0}", size));
#endif
			short err = PSError.noErr;
			try
			{
				bufferID = Memory.Allocate(size, false);

				this.bufferIDs.Add(bufferID);
			}
			catch (OutOfMemoryException)
			{
				err = PSError.memFullErr;
			}

			return err;
		}

		private void BufferFreeProc(IntPtr bufferID)
		{
#if DEBUG
			DebugUtils.Ping(DebugFlags.BufferSuite, string.Format("Buffer: 0x{0}, Size: {1}", bufferID.ToHexString(), Memory.Size(bufferID)));
#endif
			Memory.Free(bufferID);

			this.bufferIDs.Remove(bufferID);
		}

		private IntPtr BufferLockProc(IntPtr bufferID, byte moveHigh)
		{
#if DEBUG
			DebugUtils.Ping(DebugFlags.BufferSuite, string.Format("Buffer: 0x{0}", bufferID.ToHexString()));
#endif

			return bufferID;
		}

		private void BufferUnlockProc(IntPtr bufferID)
		{
#if DEBUG
			DebugUtils.Ping(DebugFlags.BufferSuite, string.Format("Buffer: 0x{0}", bufferID.ToHexString()));
#endif
		}

		private int BufferSpaceProc()
		{
			return 1000000000;
		}
	}
}
