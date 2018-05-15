#region License
/*Copyright(c) 2018, Brian Humlicek
* https://github.com/BrianHumlicek
* 
*Permission is hereby granted, free of charge, to any person obtaining a copy
*of this software and associated documentation files (the "Software"), to deal
*in the Software without restriction, including without limitation the rights
*to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
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
using System.Collections.Generic;
using System.Runtime.InteropServices;
using J2534.Common;
using J2534.Definitions;

namespace J2534.DataClasses
{
    internal class HeapJ2534Message : Disposable
    {
        public IntPtr Ptr { get; } = Marshal.AllocHGlobal(CONST.J2534MESSAGESIZE);
        public HeapJ2534Message(J2534Protocol ProtocolID)
        {
            this.ProtocolID = ProtocolID;
        }
        public HeapJ2534Message(J2534Protocol ProtocolID, J2534Message Message)
        {
            this.ProtocolID = ProtocolID;
            TxFlags = Message.TxFlags;
            //this.ExtraDataIndex = Message.ExtraDataIndex;
            Data = Message.Data;
        }
        public HeapJ2534Message(J2534Protocol ProtocolID, J2534TxFlag TxFlags, IEnumerable<byte> Data)
        {
            this.ProtocolID = ProtocolID;
            this.TxFlags = TxFlags;
            this.Data = Data;
        }

        public J2534Message ToJ2534Message()
        {
            return new J2534Message((byte[])Data, RxStatus, Timestamp);
        }

        public J2534Protocol ProtocolID
        {
            get { return (J2534Protocol)Marshal.ReadInt32(Ptr); }
            set { Marshal.WriteInt32(Ptr, (int)value); }
        }
        public J2534RXFLAG RxStatus
        {
            get { return (J2534RXFLAG)Marshal.ReadInt32(Ptr, 4); }
            set { Marshal.WriteInt32(Ptr, 4, (int)value); }
        }
        public J2534TxFlag TxFlags
        {
            get { return (J2534TxFlag)Marshal.ReadInt32(Ptr, 8); }
            set { Marshal.WriteInt32(Ptr, 8, (int)value); }
        }
        public uint Timestamp
        {
            get { return (uint)Marshal.ReadInt32(Ptr, 12); }
            set { Marshal.WriteInt32(Ptr, 12, (int)value); }
        }
        public uint ExtraDataIndex
        {
            get { return (uint)Marshal.ReadInt32(Ptr, 20); }
            set { Marshal.WriteInt32(Ptr, 20, (int)value); }
        }
        public int Length
        {
            get { return Marshal.ReadInt32(Ptr, 16); }
            private set
            {
                if (value > CONST.J2534MESSAGESIZE) throw new ArgumentException("Message Data.Length is greator than fixed maximum");
                Marshal.WriteInt32(Ptr, 16, value);
            }
        }
        public IEnumerable<byte> Data
        {
            get
            {
                byte[] data = new byte[Marshal.ReadInt32(Ptr, 16)];
                Marshal.Copy(IntPtr.Add(Ptr, 24), data, 0, data.Length);
                return data;
            }
            set
            {
                IntPtr DataPtr = IntPtr.Add(Ptr, 24);  //Offset to data array
                if (value is byte[])  //Byte[] is fastest
                {
                    var ValueAsArray = (byte[])value;
                    Length = ValueAsArray.Length;
                    Marshal.Copy(ValueAsArray, 0, DataPtr, ValueAsArray.Length);
                }
                else if (value is IList<byte>)   //Collection with indexer is second best
                {
                    var ValueAsList = (IList<byte>)value;
                    int length = ValueAsList.Count;
                    Length = length;
                    for (int indexer = 0; indexer < length; indexer++)
                    {
                        Marshal.WriteByte(DataPtr, indexer, ValueAsList[indexer]);
                    }
                }
                else if (value is IEnumerable<byte>)    //Enumerator is third
                {
                    int index_count = 0;
                    foreach (byte b in value)
                    {
                        Marshal.WriteByte(DataPtr, index_count, b);
                        index_count++;
                    }
                    Length = index_count;  //Set length
                }
            }
        }
        public static explicit operator IntPtr(HeapJ2534Message HeapJ2534MessagePtr)
        {
            return HeapJ2534MessagePtr.Ptr;
        }
        protected override void DisposeUnmanaged()
        {
            Marshal.FreeHGlobal(Ptr);
            base.DisposeUnmanaged();
        }
    }
}
