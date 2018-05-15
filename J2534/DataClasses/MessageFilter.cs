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
using System.Linq;
using J2534.Definitions;

namespace J2534.DataClasses
{
    public class MessageFilter
    {
        public J2534Filter FilterType;
        public byte[] Mask;
        public byte[] Pattern;
        public byte[] FlowControl;
        public J2534TxFlag TxFlags;
        public int FilterId;

        public MessageFilter()
        {
            TxFlags = J2534TxFlag.NONE;
        }

        public MessageFilter(UserFilterType FilterType, byte[] Match)
        {
            TxFlags = J2534TxFlag.NONE;

            switch (FilterType)
            {
                case UserFilterType.PASSALL:
                    PassAll();
                    break;
                case UserFilterType.PASS:
                    Pass(Match);
                    break;
                case UserFilterType.BLOCK:
                    Block(Match);
                    break;
                case UserFilterType.STANDARDISO15765:
                    StandardISO15765(Match);
                    break;
                case UserFilterType.NONE:
                    break;
            }
        }

        private void Reset(int Length)
        {
            Mask = new byte[Length];
            Pattern = new byte[Length];
            FlowControl = new byte[Length];
        }

        public void PassAll()
        {
            Reset(1);
            Mask[0] = 0x00;
            Pattern[0] = 0x00;
            FilterType = J2534Filter.PASS_FILTER;
        }

        public void Pass(byte[] Match)
        {
            ExactMatch(Match);
            FilterType = J2534Filter.PASS_FILTER;
        }

        public void Block(byte[] Match)
        {
            ExactMatch(Match);
            FilterType = J2534Filter.BLOCK_FILTER;
        }

        private void ExactMatch(byte[] Match)
        {
            Reset(Match.Length);
            Mask = Enumerable.Repeat((byte)0xFF, Match.Length).ToArray();
            Pattern = Match;
        }
        public void StandardISO15765(byte[] SourceAddress)
        {
            //Should throw exception??
            if (SourceAddress.Length != 4)
                return;
            Reset(4);
            Mask[0] = 0xFF;
            Mask[1] = 0xFF;
            Mask[2] = 0xFF;
            Mask[3] = 0xFF;

            Pattern = SourceAddress;
            Pattern[3] += 0x08;

            FlowControl = SourceAddress;

            TxFlags = J2534TxFlag.ISO15765_FRAME_PAD;
            FilterType = J2534Filter.FLOW_CONTROL_FILTER;
        }
    }
}
