/////////////////////////////////////////////////////////////////////////////////
//
// Photoshop-compatible filter host Effect plugin for Paint.NET
// http://psfilterpdn.codeplex.com/
//
// This software is provided under the Microsoft Public License:
//   Copyright (C) 2010-2015 Nicholas Hayes
// 
// See LICENSE.txt for complete licensing and attribution information.
//
/////////////////////////////////////////////////////////////////////////////////

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace PSFilterLoad.PSApi
{
	internal sealed class HandleSuite
	{
		private struct PSHandle
		{
			public IntPtr pointer;

			public static readonly int SizeOf = Marshal.SizeOf(typeof(PSHandle));
		}

		private sealed class HandleEntry
		{
			public readonly IntPtr pointer;
			public readonly int size;

			public HandleEntry(IntPtr pointer, int size)
			{
				this.pointer = pointer;
				this.size = size;
			}
		}

		private class HandleSuiteSingleton
		{
			// Explicit static constructor to tell C# compiler
			// not to mark type as beforefieldinit
			static HandleSuiteSingleton()
			{
			}

			private HandleSuiteSingleton()
			{
			}

			internal static readonly HandleSuite Instance = new HandleSuite();
		}

		private readonly NewPIHandleProc handleNewProc;
		private readonly DisposePIHandleProc handleDisposeProc;
		private readonly GetPIHandleSizeProc handleGetSizeProc;
		private readonly SetPIHandleSizeProc handleSetSizeProc;
		private readonly LockPIHandleProc handleLockProc;
		private readonly UnlockPIHandleProc handleUnlockProc;
		private readonly RecoverSpaceProc handleRecoverSpaceProc;
		private readonly DisposeRegularPIHandleProc handleDisposeRegularProc;
		private readonly Dictionary<IntPtr, HandleEntry> handles;

		private HandleSuite()
		{
			this.handleNewProc = new NewPIHandleProc(NewHandle);
			this.handleDisposeProc = new DisposePIHandleProc(DisposeHandle);
			this.handleGetSizeProc = new GetPIHandleSizeProc(GetHandleSize);
			this.handleSetSizeProc = new SetPIHandleSizeProc(SetHandleSize);
			this.handleLockProc = new LockPIHandleProc(LockHandle);
			this.handleUnlockProc = new UnlockPIHandleProc(UnlockHandle);
			this.handleRecoverSpaceProc = new RecoverSpaceProc(RecoverHandleSpace);
			this.handleDisposeRegularProc = new DisposeRegularPIHandleProc(DisposeRegularHandle);
			this.handles = new Dictionary<IntPtr, HandleEntry>();
		}

		public static HandleSuite Instance
		{
			get
			{
				return HandleSuiteSingleton.Instance;
			}
		}

		public IntPtr CreateHandleProcs()
		{
			IntPtr handleProcsPtr = Memory.Allocate(Marshal.SizeOf(typeof(HandleProcs)), true);

			unsafe
			{
				HandleProcs* handleProcs = (HandleProcs*)handleProcsPtr.ToPointer();
				handleProcs->handleProcsVersion = PSConstants.kCurrentHandleProcsVersion;
				handleProcs->numHandleProcs = PSConstants.kCurrentHandleProcsCount;
				handleProcs->newProc = Marshal.GetFunctionPointerForDelegate(this.handleNewProc);
				handleProcs->disposeProc = Marshal.GetFunctionPointerForDelegate(this.handleDisposeProc);
				handleProcs->getSizeProc = Marshal.GetFunctionPointerForDelegate(this.handleGetSizeProc);
				handleProcs->setSizeProc = Marshal.GetFunctionPointerForDelegate(this.handleSetSizeProc);
				handleProcs->lockProc = Marshal.GetFunctionPointerForDelegate(this.handleLockProc);
				handleProcs->unlockProc = Marshal.GetFunctionPointerForDelegate(this.handleUnlockProc);
				handleProcs->recoverSpaceProc = Marshal.GetFunctionPointerForDelegate(this.handleRecoverSpaceProc);
				handleProcs->disposeRegularHandleProc = Marshal.GetFunctionPointerForDelegate(this.handleDisposeRegularProc);
			}

			return handleProcsPtr;
		}

		/// <summary>
		/// Determines whether the handle was allocated using the handle suite.
		/// </summary>
		/// <param name="handle">The handle to check.</param>
		/// <returns>
		///   <c>true</c> if the handle was allocated using the handle suite; otherwise, <c>false</c>.
		/// </returns>
		public bool AllocatedBySuite(IntPtr handle)
		{
			return this.handles.ContainsKey(handle);
		}

		public void FreeRemainingHandles()
		{
			foreach (var item in this.handles)
			{
				Memory.Free(item.Value.pointer);
				Memory.Free(item.Key);
			}
			this.handles.Clear();
		}

		/// <summary>
		/// Determines whether the specified pointer is not valid to read from.
		/// </summary>
		/// <param name="ptr">The pointer to check.</param>
		/// <returns>
		///   <c>true</c> if the pointer is invalid; otherwise, <c>false</c>.
		/// </returns>
		private static bool IsBadReadPtr(IntPtr ptr)
		{
			bool result = false;
			NativeStructs.MEMORY_BASIC_INFORMATION mbi = new NativeStructs.MEMORY_BASIC_INFORMATION();
			int mbiSize = Marshal.SizeOf(typeof(NativeStructs.MEMORY_BASIC_INFORMATION));

			if (SafeNativeMethods.VirtualQuery(ptr, ref mbi, new UIntPtr((ulong)mbiSize)) == UIntPtr.Zero)
			{
				return true;
			}

			result = ((mbi.Protect & NativeConstants.PAGE_READONLY) != 0 ||
					  (mbi.Protect & NativeConstants.PAGE_READWRITE) != 0 ||
					  (mbi.Protect & NativeConstants.PAGE_WRITECOPY) != 0 ||
					  (mbi.Protect & NativeConstants.PAGE_EXECUTE_READ) != 0 ||
					  (mbi.Protect & NativeConstants.PAGE_EXECUTE_READWRITE) != 0 ||
					  (mbi.Protect & NativeConstants.PAGE_EXECUTE_WRITECOPY) != 0);

			if ((mbi.Protect & NativeConstants.PAGE_GUARD) != 0 || (mbi.Protect & NativeConstants.PAGE_NOACCESS) != 0)
			{
				result = false;
			}

			return !result;
		}

		/// <summary>
		/// Determines whether the specified pointer is not valid to write to.
		/// </summary>
		/// <param name="ptr">The pointer to check.</param>
		/// <returns>
		///   <c>true</c> if the pointer is invalid; otherwise, <c>false</c>.
		/// </returns>
		private static bool IsBadWritePtr(IntPtr ptr)
		{
			bool result = false;
			NativeStructs.MEMORY_BASIC_INFORMATION mbi = new NativeStructs.MEMORY_BASIC_INFORMATION();
			int mbiSize = Marshal.SizeOf(typeof(NativeStructs.MEMORY_BASIC_INFORMATION));

			if (SafeNativeMethods.VirtualQuery(ptr, ref mbi, new UIntPtr((ulong)mbiSize)) == UIntPtr.Zero)
			{
				return true;
			}

			result = ((mbi.Protect & NativeConstants.PAGE_READWRITE) != 0 ||
					  (mbi.Protect & NativeConstants.PAGE_WRITECOPY) != 0 ||
					  (mbi.Protect & NativeConstants.PAGE_EXECUTE_READWRITE) != 0 ||
					  (mbi.Protect & NativeConstants.PAGE_EXECUTE_WRITECOPY) != 0);

			if ((mbi.Protect & NativeConstants.PAGE_GUARD) != 0 || (mbi.Protect & NativeConstants.PAGE_NOACCESS) != 0)
			{
				result = false;
			}

			return !result;
		}

		internal unsafe IntPtr NewHandle(int size)
		{
			IntPtr handle = IntPtr.Zero;
			try
			{
				// The Photoshop API 'Handle' is a double indirect pointer.
				// As some plug-ins may dereference the pointer instead of calling HandleLockProc we recreate that implementation.
				handle = Memory.Allocate(PSHandle.SizeOf, true);

				PSHandle* hand = (PSHandle*)handle.ToPointer();

				hand->pointer = Memory.Allocate(size, true);

				this.handles.Add(handle, new HandleEntry(hand->pointer, size));
#if DEBUG
				string message = string.Format("Handle: 0x{0}, pointer: 0x{1}, size: {2}", handle.ToHexString(), hand->pointer.ToHexString(), size);
				DebugUtils.Ping(DebugFlags.HandleSuite, message);
#endif
			}
			catch (OutOfMemoryException)
			{
				if (handle != IntPtr.Zero)
				{
					Memory.Free(handle);
					handle = IntPtr.Zero;
				}

				return IntPtr.Zero;
			}

			return handle;
		}

		internal unsafe void DisposeHandle(IntPtr h)
		{
			if (h != IntPtr.Zero && !IsBadReadPtr(h))
			{
#if DEBUG
				DebugUtils.Ping(DebugFlags.HandleSuite, string.Format("Handle: 0x{0}", h.ToHexString()));
#endif
				if (!AllocatedBySuite(h))
				{
					if (SafeNativeMethods.GlobalSize(h).ToInt64() > 0L)
					{
						IntPtr hPtr = Marshal.ReadIntPtr(h);

						if (!IsBadReadPtr(hPtr) && SafeNativeMethods.GlobalSize(hPtr).ToInt64() > 0L)
						{
							SafeNativeMethods.GlobalFree(hPtr);
						}

						SafeNativeMethods.GlobalFree(h);
					}

					return;
				}

				PSHandle* handle = (PSHandle*)h.ToPointer();

				Memory.Free(handle->pointer);
				Memory.Free(h);

				this.handles.Remove(h);
			}
		}

		private unsafe void DisposeRegularHandle(IntPtr h)
		{
#if DEBUG
			DebugUtils.Ping(DebugFlags.HandleSuite, string.Format("Handle: 0x{0}", h.ToHexString()));
#endif
			// What is this supposed to do?
			if (!AllocatedBySuite(h))
			{
				if (SafeNativeMethods.GlobalSize(h).ToInt64() > 0L)
				{
					IntPtr hPtr = Marshal.ReadIntPtr(h);

					if (!IsBadReadPtr(hPtr) && SafeNativeMethods.GlobalSize(hPtr).ToInt64() > 0L)
					{
						SafeNativeMethods.GlobalFree(hPtr);
					}

					SafeNativeMethods.GlobalFree(h);
				}
			}
		}

		internal IntPtr LockHandle(IntPtr h, byte moveHigh)
		{
#if DEBUG
			DebugUtils.Ping(DebugFlags.HandleSuite, string.Format("Handle: 0x{0}, moveHigh: {1}", h.ToHexString(), moveHigh));
#endif
			if (!AllocatedBySuite(h))
			{
				if (SafeNativeMethods.GlobalSize(h).ToInt64() > 0L)
				{
					IntPtr hPtr = Marshal.ReadIntPtr(h);

					if (!IsBadReadPtr(hPtr) && SafeNativeMethods.GlobalSize(hPtr).ToInt64() > 0L)
					{
						return SafeNativeMethods.GlobalLock(hPtr);
					}

					return SafeNativeMethods.GlobalLock(h);
				}
				if (!IsBadReadPtr(h) && !IsBadWritePtr(h)) // Pointer to a pointer?
				{
					return h;
				}
				return IntPtr.Zero;
			}

			return this.handles[h].pointer;
		}

		internal int GetHandleSize(IntPtr h)
		{
#if DEBUG
			DebugUtils.Ping(DebugFlags.HandleSuite, string.Format("Handle: 0x{0}", h.ToHexString()));
#endif
			if (!AllocatedBySuite(h))
			{
				if (SafeNativeMethods.GlobalSize(h).ToInt64() > 0L)
				{
					IntPtr hPtr = Marshal.ReadIntPtr(h);

					if (!IsBadReadPtr(hPtr))
					{
						return SafeNativeMethods.GlobalSize(hPtr).ToInt32();
					}
					else
					{
						return SafeNativeMethods.GlobalSize(h).ToInt32();
					}
				}
				return 0;
			}

			return this.handles[h].size;
		}

		private void RecoverHandleSpace(int size)
		{
#if DEBUG
			DebugUtils.Ping(DebugFlags.HandleSuite, string.Format("size: {0}", size));
#endif
		}

		private unsafe short SetHandleSize(IntPtr h, int newSize)
		{
#if DEBUG
			DebugUtils.Ping(DebugFlags.HandleSuite, string.Format("Handle: 0x{0}", h.ToHexString()));
#endif
			if (!AllocatedBySuite(h))
			{
				if (SafeNativeMethods.GlobalSize(h).ToInt64() > 0L)
				{
					IntPtr hPtr = Marshal.ReadIntPtr(h);

					if (!IsBadReadPtr(hPtr) && SafeNativeMethods.GlobalSize(hPtr).ToInt64() > 0L)
					{
						IntPtr hMem = SafeNativeMethods.GlobalReAlloc(hPtr, new UIntPtr((uint)newSize), NativeConstants.GPTR);
						if (hMem == IntPtr.Zero)
						{
							return PSError.memFullErr;
						}
						Marshal.WriteIntPtr(h, hMem);
					}
					else
					{
						if (SafeNativeMethods.GlobalReAlloc(h, new UIntPtr((uint)newSize), NativeConstants.GPTR) == IntPtr.Zero)
						{
							return PSError.memFullErr;
						}
					}

					return PSError.noErr;
				}
				return PSError.nilHandleErr;
			}

			try
			{
				PSHandle* handle = (PSHandle*)h.ToPointer();
				IntPtr ptr = Memory.ReAlloc(handle->pointer, newSize);

				handle->pointer = ptr;

				this.handles.AddOrUpdate(h, new HandleEntry(ptr, newSize));
			}
			catch (OutOfMemoryException)
			{
				return PSError.memFullErr;
			}

			return PSError.noErr;
		}

		internal void UnlockHandle(IntPtr h)
		{
#if DEBUG
			DebugUtils.Ping(DebugFlags.HandleSuite, string.Format("Handle: 0x{0}", h.ToHexString()));
#endif
			if (!AllocatedBySuite(h))
			{
				if (SafeNativeMethods.GlobalSize(h).ToInt64() > 0L)
				{
					IntPtr hPtr = Marshal.ReadIntPtr(h);

					if (!IsBadReadPtr(hPtr) && SafeNativeMethods.GlobalSize(hPtr).ToInt64() > 0L)
					{
						SafeNativeMethods.GlobalUnlock(hPtr);
					}
					else
					{
						SafeNativeMethods.GlobalUnlock(h);
					}
				}
			}
		}
	}
}
