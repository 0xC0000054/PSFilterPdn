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
	internal sealed class HandleSuite
	{
		private struct PSHandle
		{
			public IntPtr pointer;

			public static readonly int SizeOf = Marshal.SizeOf(typeof(PSHandle));
		}

		private sealed class HandleEntry : IDisposable
		{
			private IntPtr handle;
			private IntPtr pointer;
			private readonly int size;
			private bool disposed;

			public IntPtr Pointer
			{
				get
				{
					return this.pointer;
				}
			}

			public int Size
			{
				get
				{
					return this.size;
				}
			}

			public HandleEntry(IntPtr handle, IntPtr pointer, int size)
			{
				this.handle = handle;
				this.pointer = pointer;
				this.size = size;
				this.disposed = false;
			}

			public void Dispose()
			{
				Dispose(true);
				GC.SuppressFinalize(this);
			}

			~HandleEntry()
			{
				Dispose(false);
			}

			private void Dispose(bool disposing)
			{
				if (!disposed)
				{
					if (disposing)
					{
					}

					if (handle != IntPtr.Zero)
					{
						Memory.Free(handle);
						handle = IntPtr.Zero;
					}
					if (pointer != IntPtr.Zero)
					{
						Memory.Free(pointer);
						pointer = IntPtr.Zero;
					}
					disposed = true;
				}
			}
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

		private static readonly HandleSuite instance = new HandleSuite();

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
			this.handles = new Dictionary<IntPtr, HandleEntry>(IntPtrEqualityComparer.Instance);
		}

		public static HandleSuite Instance
		{
			get
			{
				return instance;
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
				item.Value.Dispose();
			}
			this.handles.Clear();
		}

		/// <summary>
		/// Determines whether the specified pointer is valid to read from.
		/// </summary>
		/// <param name="ptr">The pointer to check.</param>
		/// <returns>
		///   <c>true</c> if the pointer is valid; otherwise, <c>false</c>.
		/// </returns>
		private static bool IsValidReadPtr(IntPtr ptr)
		{
			NativeStructs.MEMORY_BASIC_INFORMATION mbi = new NativeStructs.MEMORY_BASIC_INFORMATION();
			int mbiSize = Marshal.SizeOf(typeof(NativeStructs.MEMORY_BASIC_INFORMATION));

			if (SafeNativeMethods.VirtualQuery(ptr, out mbi, new UIntPtr((ulong)mbiSize)) == UIntPtr.Zero)
			{
				return false;
			}

			if (mbi.State != NativeConstants.MEM_COMMIT ||
				(mbi.Protect & NativeConstants.PAGE_GUARD) != 0 ||
				(mbi.Protect & NativeConstants.PAGE_NOACCESS) != 0)
			{
				return false;
			}

			const int ReadProtect = NativeConstants.PAGE_READONLY |
									NativeConstants.PAGE_READWRITE |
									NativeConstants.PAGE_WRITECOPY |
									NativeConstants.PAGE_EXECUTE_READ |
									NativeConstants.PAGE_EXECUTE_READWRITE |
									NativeConstants.PAGE_EXECUTE_WRITECOPY;

			return ((mbi.Protect & ReadProtect) != 0);
		}

		/// <summary>
		/// Determines whether the specified pointer is valid to write to.
		/// </summary>
		/// <param name="ptr">The pointer to check.</param>
		/// <returns>
		///   <c>true</c> if the pointer is valid; otherwise, <c>false</c>.
		/// </returns>
		private static bool IsValidWritePtr(IntPtr ptr)
		{
			NativeStructs.MEMORY_BASIC_INFORMATION mbi = new NativeStructs.MEMORY_BASIC_INFORMATION();
			int mbiSize = Marshal.SizeOf(typeof(NativeStructs.MEMORY_BASIC_INFORMATION));

			if (SafeNativeMethods.VirtualQuery(ptr, out mbi, new UIntPtr((ulong)mbiSize)) == UIntPtr.Zero)
			{
				return false;
			}

			if (mbi.State != NativeConstants.MEM_COMMIT ||
				(mbi.Protect & NativeConstants.PAGE_GUARD) != 0 ||
				(mbi.Protect & NativeConstants.PAGE_NOACCESS) != 0)
			{
				return false;
			}

			const int WriteProtect = NativeConstants.PAGE_READWRITE |
									 NativeConstants.PAGE_WRITECOPY |
									 NativeConstants.PAGE_EXECUTE_READWRITE |
									 NativeConstants.PAGE_EXECUTE_WRITECOPY;

			return ((mbi.Protect & WriteProtect) != 0);
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

				this.handles.Add(handle, new HandleEntry(handle, hand->pointer, size));
#if DEBUG
				string message = string.Format("Handle: 0x{0}, pointer: 0x{1}, size: {2}", handle.ToHexString(), hand->pointer.ToHexString(), size);
				DebugUtils.Ping(DebugFlags.HandleSuite, message);
#endif
			}
			catch (OutOfMemoryException)
			{
				if (handle != IntPtr.Zero)
				{
					// Free the handle pointer if it has been allocated.
					// This would occur if the framework throws an OutOfMemoryException when adding to the handles dictionary.
					PSHandle* hand = (PSHandle*)handle.ToPointer();
					if (hand->pointer != IntPtr.Zero)
					{
						Memory.Free(hand->pointer);
						hand->pointer = IntPtr.Zero;
					}

					Memory.Free(handle);
					handle = IntPtr.Zero;
				}

				return IntPtr.Zero;
			}

			return handle;
		}

		internal unsafe void DisposeHandle(IntPtr h)
		{
			if (h != IntPtr.Zero && IsValidReadPtr(h))
			{
#if DEBUG
				DebugUtils.Ping(DebugFlags.HandleSuite, string.Format("Handle: 0x{0}", h.ToHexString()));
#endif
				HandleEntry item;
				if (this.handles.TryGetValue(h, out item))
				{
					item.Dispose();
					this.handles.Remove(h);
				}
				else
				{
					if (SafeNativeMethods.GlobalSize(h).ToInt64() > 0L)
					{
						IntPtr hPtr = Marshal.ReadIntPtr(h);

						if (IsValidReadPtr(hPtr) && SafeNativeMethods.GlobalSize(hPtr).ToInt64() > 0L)
						{
							SafeNativeMethods.GlobalFree(hPtr);
						}

						SafeNativeMethods.GlobalFree(h);
					}

					return;
				}
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

					if (IsValidReadPtr(hPtr) && SafeNativeMethods.GlobalSize(hPtr).ToInt64() > 0L)
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
			HandleEntry item;
			if (this.handles.TryGetValue(h, out item))
			{
				return item.Pointer;
			}
			else
			{
				if (SafeNativeMethods.GlobalSize(h).ToInt64() > 0L)
				{
					IntPtr hPtr = Marshal.ReadIntPtr(h);

					if (IsValidReadPtr(hPtr) && SafeNativeMethods.GlobalSize(hPtr).ToInt64() > 0L)
					{
						return SafeNativeMethods.GlobalLock(hPtr);
					}

					return SafeNativeMethods.GlobalLock(h);
				}
				if (IsValidReadPtr(h) && IsValidWritePtr(h)) // Pointer to a pointer?
				{
					return h;
				}
				return IntPtr.Zero;
			}
		}

		internal int GetHandleSize(IntPtr h)
		{
#if DEBUG
			DebugUtils.Ping(DebugFlags.HandleSuite, string.Format("Handle: 0x{0}", h.ToHexString()));
#endif
			HandleEntry item;
			if (this.handles.TryGetValue(h, out item))
			{
				return item.Size;
			}
			else
			{
				if (SafeNativeMethods.GlobalSize(h).ToInt64() > 0L)
				{
					IntPtr hPtr = Marshal.ReadIntPtr(h);

					if (IsValidReadPtr(hPtr))
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

					if (IsValidReadPtr(hPtr) && SafeNativeMethods.GlobalSize(hPtr).ToInt64() > 0L)
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

				this.handles.AddOrUpdate(h, new HandleEntry(h, ptr, newSize));
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

					if (IsValidReadPtr(hPtr) && SafeNativeMethods.GlobalSize(hPtr).ToInt64() > 0L)
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
