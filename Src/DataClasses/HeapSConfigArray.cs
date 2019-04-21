#region License
/*Copyright(c) 2018, Brian Humlicek
* https://github.com/BrianHumlicek
* 
*Permission is hereby granted, free of charge, to any person obtaining a copy
*of this software and associated documentation files (the "Software"), to deal
*in the Software without restriction, including without limitation the rights
*to use, copy, modify, merge, publish, distribute, sub-license, and/or sell
*copies of the Software, and to permit persons to whom the Software is
*furnished to do so, subject to the following conditions:
*The above copyright notice and this permission notice shall be included in all
*copies or substantial portions of the Software.
*
*THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
*IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
*FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
*AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
*LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
*OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
*SOFTWARE.
*/
#endregion License
using System;
using System.Runtime.InteropServices;

namespace SAE.J2534
{
    internal class HeapSConfigArray : Common.UnmanagedDisposable
    {
        private IntPtr arrayPtr;
        public IntPtr Ptr { get; }
        public HeapSConfigArray(SConfig ConfigItem)
        {
            //Create a blob big enough for 'ConfigItems' and two longs (NumOfItems and pItems)
            Ptr = Marshal.AllocHGlobal(16);
            arrayPtr = IntPtr.Add(Ptr, 8);
            Length = 1;  //Set Number of items

            //Write pItems.  To save complexity, the array immediately follows SConfigArray.
            Marshal.WriteIntPtr(Ptr, 4, IntPtr.Add(Ptr, 8));

            //Write ConfigItem to the blob
            Marshal.StructureToPtr<SConfig>(ConfigItem, arrayPtr, false);
        }
        public HeapSConfigArray(SConfig[] ConfigItems)
        {
            //Create a blob big enough for 'ConfigItems' and two longs (NumOfItems and pItems)
            Ptr = Marshal.AllocHGlobal(ConfigItems.Length * 8 + 8);
            arrayPtr = IntPtr.Add(Ptr, 8);
            Length = ConfigItems.Length;

            //Write pItems.  To save complexity, the array immediately follows SConfigArray.
            Marshal.WriteIntPtr(Ptr, 4, arrayPtr);

            //Write the array to the blob
            for (int i = 0; i < ConfigItems.Length; i++)
                setIndex(i, ConfigItems[i]);
        }
        public int Length
        {
            get { return Marshal.ReadInt32(Ptr); }
            private set { Marshal.WriteInt32(Ptr, value); }
        }
        public SConfig this[int Index]
        {
            get
            {
                if (Index >= Length || Index < 0) throw new IndexOutOfRangeException("Index out of bounds HeapSConfigArrayPtr!");
                return getIndex(Index);
            }
            set
            {
                if (Index >= Length || Index < 0) throw new IndexOutOfRangeException("Index out of bounds HeapSConfigArrayPtr!");
                setIndex(Index, value);
            }
        }
        private SConfig getIndex(int Index)
        {
            return Marshal.PtrToStructure<SConfig>(IntPtr.Add(arrayPtr, Index * 8));
        }
        private void setIndex(int Index, SConfig SConfig)
        {
            Marshal.StructureToPtr<SConfig>(SConfig, IntPtr.Add(arrayPtr, Index * 8), false);
        }
        public SConfig[] ToSConfigArray()
        {
            SConfig[] result = new SConfig[Length];
            for(int i = 0;i < result.Length; i++)
                result[i] = getIndex(i);
            return result;
        }
        public static explicit operator IntPtr(HeapSConfigArray HeapSConfigArray)
        {
            return HeapSConfigArray.Ptr;
        }
        protected override void DisposeUnmanaged()
        {
            Marshal.FreeHGlobal(Ptr);
        }
    }
}
