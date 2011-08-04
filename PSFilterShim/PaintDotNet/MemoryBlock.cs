/////////////////////////////////////////////////////////////////////////////////
// Paint.NET                                                                   //
// Copyright (C) dotPDN LLC, Rick Brewster, Tom Jackson, and contributors.     //
// Portions Copyright (C) Microsoft Corporation. All Rights Reserved.          //
// See src/Resources/Files/License.txt for full licensing and attribution      //
// details.                                                                    //
// .                                                                           //
/////////////////////////////////////////////////////////////////////////////////

using PaintDotNet.SystemLayer;
using System;
using System.Collections;
using System.Drawing;
using System.Globalization;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Security;
using System.Threading;

namespace PaintDotNet
{
    /// <summary>
    /// Manages an arbitrarily sized block of memory. You can also create child MemoryBlocks
    /// which reference a portion of the memory allocated by a parent MemoryBlock. If the parent
    /// is disposed, the children will not be valid.
    /// </summary>
    public unsafe sealed class MemoryBlock
        : IDisposable,
          ICloneable
    {
        // serialize 1MB at a time: this enables us to serialize very large blocks, and to conserve memory while doing so
        private const int serializationChunkSize = 1048576; 

        // blocks this size or larger are allocated with AllocateLarge (VirtualAlloc) instead of Allocate (HeapAlloc)
        private const long largeBlockThreshold = 65536;

        private long length;

        // if parentBlock == null, then we allocated the pointer and are responsible for deallocating it
        // if parentBlock != null, then the parentBlock allocated it, not us
        private void *voidStar;

        private bool valid; // if voidStar is null, and this is false, we know that it's null because allocation failed. otherwise we have a real error

        private MemoryBlock parentBlock = null;

        private IntPtr bitmapHandle = IntPtr.Zero; // if allocated using the "width, height" constructor, we keep track of a bitmap handle
        private int bitmapWidth;
        private int bitmapHeight;

        private bool disposed = false;

        public MemoryBlock Parent
        {
            get
            {
                return this.parentBlock;
            }
        }

        public long Length
        {
            get
            {
                if (disposed)
                {
                    throw new ObjectDisposedException("MemoryBlock");
                }

                return length;
            }
        }

        public IntPtr Pointer
        {
            get
            {
                if (disposed)
                {
                    throw new ObjectDisposedException("MemoryBlock");
                }

                return new IntPtr(voidStar);
            }
        }

        public IntPtr BitmapHandle
        {
            get
            {
                if (disposed)
                {
                    throw new ObjectDisposedException("MemoryBlock");
                }

                return this.bitmapHandle;
            }
        }

        public void *VoidStar
        {
            get
            {
                if (disposed)
                {
                    throw new ObjectDisposedException("MemoryBlock");
                }

                return voidStar;
            }
        }

        public byte this[long index]
        {
            get
            {
                if (disposed)
                {
                    throw new ObjectDisposedException("MemoryBlock");
                }

                if (index < 0 || index >= length)
                {
                    throw new ArgumentOutOfRangeException("index must be positive and less than Length");
                }

                unsafe
                {
                    return ((byte *)this.VoidStar)[index];
                }
            }

            set
            {
                if (disposed)
                {
                    throw new ObjectDisposedException("MemoryBlock");
                }

                if (index < 0 || index >= length)
                {
                    throw new ArgumentOutOfRangeException("index must be positive and less than Length");
                }

                unsafe
                {
                    ((byte *)this.VoidStar)[index] = value;
                }
            }
        }

        public bool MaySetAllowWrites
        {
            get
            {
                if (disposed)
                {
                    throw new ObjectDisposedException("MemoryBlock");
                }

                if (this.parentBlock != null)
                {
                    return this.parentBlock.MaySetAllowWrites;
                }
                else
                {
                    return (this.length >= largeBlockThreshold && this.bitmapHandle != IntPtr.Zero);
                }
            }
        }

        /// <summary>
        /// Sets a flag indicating whether the memory that this instance of MemoryBlock points to
        /// may be written to.
        /// </summary>
        /// <remarks>
        /// This flag is meant to be set to false for short periods of time. The value of this
        /// property is not persisted with serialization.
        /// </remarks>
        public bool AllowWrites
        {
            set
            {
                if (disposed)
                {
                    throw new ObjectDisposedException("MemoryBlock");
                }

                if (!MaySetAllowWrites)
                {
                    throw new InvalidOperationException("May not set write protection on this memory block");
                }

                Memory.ProtectBlockLarge(new IntPtr(this.voidStar), (ulong)this.length, true, value);
            }
        }

        /// <summary>
        /// Copies bytes from one area of memory to another. Since this function works
        /// with MemoryBlock instances, it does bounds checking.
        /// </summary>
        /// <param name="dst">The MemoryBlock to copy bytes to.</param>
        /// <param name="dstOffset">The offset within dst to copy bytes to.</param>
        /// <param name="src">The MemoryBlock to copy bytes from.</param>
        /// <param name="srcOffset">The offset within src to copy bytes from.</param>
        /// <param name="length">The number of bytes to copy.</param>
        public static void CopyBlock(MemoryBlock dst, long dstOffset, MemoryBlock src, long srcOffset, long length)
        {
            if ((dstOffset + length > dst.length) || (srcOffset + length > src.length))
            {
                throw new ArgumentOutOfRangeException("", "copy ranges were out of bounds");
            }

            if (dstOffset < 0)
            {
                throw new ArgumentOutOfRangeException("dstOffset", dstOffset, "must be >= 0");
            }
             
            if (srcOffset < 0)
            {
                throw new ArgumentOutOfRangeException("srcOffset", srcOffset, "must be >= 0");
            }

            if (length < 0)
            {
                throw new ArgumentOutOfRangeException("length", length, "must be >= 0");
            }

            void *dstPtr = (void *)((byte *)dst.VoidStar + dstOffset);
            void *srcPtr = (void *)((byte *)src.VoidStar + srcOffset);
            Memory.Copy(dstPtr, srcPtr, (ulong)length);
        }

        /// <summary>
        /// Creates a new parent MemoryBlock and copies our contents into it
        /// </summary>
        object ICloneable.Clone()
        {
            if (disposed)
            {
                throw new ObjectDisposedException("MemoryBlock");
            }

            return (object)Clone();
        }

        /// <summary>
        /// Creates a new parent MemoryBlock and copies our contents into it
        /// </summary>
        public MemoryBlock Clone()
        {
            if (disposed)
            {
                throw new ObjectDisposedException("MemoryBlock");
            }

            MemoryBlock dupe = new MemoryBlock(this.length);
            CopyBlock(dupe, 0, this, 0, length);
            return dupe;
        }

        /// <summary>
        /// Creates a new MemoryBlock instance and allocates the requested number of bytes.
        /// </summary>
        /// <param name="bytes"></param>
        public MemoryBlock(long bytes)
        {
            if (bytes <= 0)
            {
                throw new ArgumentOutOfRangeException("bytes", bytes, "Bytes must be greater than zero");
            }

            this.length = bytes;
            this.parentBlock = null;
            this.voidStar = Allocate(bytes).ToPointer();
            this.valid = true;
        }

        public MemoryBlock(int width, int height)
        {
            if (width < 0 && height < 0)
            {
                throw new ArgumentOutOfRangeException("width/height", new Size(width, height), "width and height must be >= 0");
            }
            else if (width < 0)
            {
                throw new ArgumentOutOfRangeException("width", width, "width must be >= 0");
            } 
            else if (height < 0)
            {
                throw new ArgumentOutOfRangeException("height", width, "height must be >= 0");
            }

            this.length = width * height * ColorBgra.SizeOf;
            this.parentBlock = null;
            this.voidStar = Allocate(width, height, out this.bitmapHandle).ToPointer();
            this.valid = true;
            this.bitmapWidth = width;
            this.bitmapHeight = height;
        }

        /// <summary>
        /// Creates a new MemoryBlock instance that refers to part of another MemoryBlock.
        /// The other MemoryBlock is the parent, and this new instance is the child.
        /// </summary>
        public unsafe MemoryBlock(MemoryBlock parentBlock, long offset, long length)
        {
            if (offset + length > parentBlock.length)
            {
                throw new ArgumentOutOfRangeException();
            }   

            this.parentBlock = parentBlock;
            byte *bytePointer = (byte *)parentBlock.VoidStar;
            bytePointer += offset;
            this.voidStar = (void *)bytePointer;
            this.valid = true;
            this.length = length;
        }

        ~MemoryBlock()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if (!disposed)
            {
                disposed = true;

                if (disposing)
                {
                }

                if (this.valid && parentBlock == null)
                {
                    if (this.bitmapHandle != IntPtr.Zero)
                    {
                        Memory.FreeBitmap(this.bitmapHandle, this.bitmapWidth, this.bitmapHeight);
                    }
                    else if (this.length >= largeBlockThreshold)
                    {
                        Memory.FreeLarge(new IntPtr(voidStar), (ulong)this.length);
                    }
                    else
                    {
                        Memory.Free(new IntPtr(voidStar));
                    }
                }

                parentBlock = null;
                voidStar = null;
                this.valid = false;
            }
        }

        private static IntPtr Allocate(int width, int height, out IntPtr handle)
        {
            return Allocate(width, height, out handle, true);
        }

        private static IntPtr Allocate(int width, int height, out IntPtr handle, bool allowRetry)
        {
            IntPtr block;

            try
            {
                block = Memory.AllocateBitmap(width, height, out handle);
            }

            catch (OutOfMemoryException)
            {
                if (allowRetry)
                {
                    Utility.GCFullCollect();
                    return Allocate(width, height, out handle, false);
                }
                else
                {
                    throw;
                }
            }

            return block;
        }

        private static IntPtr Allocate(long bytes)
        {
            return Allocate(bytes, true);
        }

        private static IntPtr Allocate(long bytes, bool allowRetry)
        {
            IntPtr block;

            try
            {
                if (bytes >= largeBlockThreshold)
                {
                    block = Memory.AllocateLarge((ulong)bytes);
                }
                else
                {
                    block = Memory.Allocate((ulong)bytes);
                }
            }

            catch (OutOfMemoryException)
            {
                if (allowRetry)
                {
                    Utility.GCFullCollect();
                    return Allocate(bytes, false);
                }
                else
                {
                    throw;
                }
            }

            return block;
        }

        public byte[] ToByteArray()
        {
            return ToByteArray(0, this.length);
        }

        public byte[] ToByteArray(long startOffset, long lengthDesired)
        {
            if (disposed)
            {
                throw new ObjectDisposedException("MemoryBlock");
            }

            if (startOffset < 0)
            {
                throw new ArgumentOutOfRangeException("startOffset", "must be greater than or equal to zero");
            }

            if (lengthDesired < 0)
            {
                throw new ArgumentOutOfRangeException("length", "must be greater than or equal to zero");
            }

            if (startOffset + lengthDesired > this.length)
            {
                throw new ArgumentOutOfRangeException("startOffset, length", "startOffset + length must be less than Length");
            }

            byte[] dstArray = new byte[lengthDesired];
            byte *pbSrcArray = (byte *)this.VoidStar;

            fixed (byte *pbDstArray = dstArray)
            {
                Memory.Copy(pbDstArray, pbSrcArray + startOffset, (ulong)lengthDesired);
            }

            return dstArray;
        }

       
    }
}
