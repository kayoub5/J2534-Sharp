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
using J2534.Definitions;

namespace J2534.DataClasses
{
    public class J2534Message
    {
        public int FlagsAsInt { get; }
        public uint Timestamp { get; }
        //public int ExtraData { get; }    //Not implemented.
        public byte[] Data { get; }
        public J2534Message()
        {
        }
        public J2534Message(byte[] Data, J2534TxFlag TxFlags = J2534TxFlag.NONE)
        {
            FlagsAsInt = (int)TxFlags;
            this.Data = Data;
        }
        public J2534Message(byte[] Data, J2534RXFLAG RxFlags, uint TimeStamp)
        {
            this.Data = Data;
            FlagsAsInt = (int)RxFlags;
            this.Timestamp = Timestamp;
        }
        public J2534RXFLAG RxStatus
        {
            get { return (J2534RXFLAG)FlagsAsInt; }
        }
        public J2534TxFlag TxFlags
        {
            get { return (J2534TxFlag)FlagsAsInt; }
        }
    }
}
