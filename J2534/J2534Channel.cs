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
using System.Linq;
using J2534.Common;
using J2534.DataClasses;
using J2534.Definitions;

namespace J2534
{
    public class J2534Channel : Disposable
    {
        private object sync { get; }
        private int channelId;
        private HeapJ2534MessageArray hJ2534MessageArray { get; }
        private List<PeriodicMessage> periodicMsgList { get; } = new List<PeriodicMessage>();
        private List<MessageFilter> filterList { get; } = new List<MessageFilter>();
        internal J2534API API { get; }
        internal J2534Device Device { get; }
        public J2534Protocol ProtocolID { get; }
        public J2534CONNECTFLAG ConnectFlags { get; }
        public IList<PeriodicMessage> PeriodicMsgList { get { return periodicMsgList.AsReadOnly(); } }
        public IList<MessageFilter> FilterList { get { return filterList.AsReadOnly(); } }
        public int DefaultTxTimeout { get; set; }
        public int DefaultRxTimeout { get; set; }
        public J2534TxFlag DefaultTxFlag { get; set; }

        internal J2534Channel(J2534Device Device, J2534Protocol ProtocolID, J2534Baud Baud, J2534CONNECTFLAG ConnectFlags, int ChannelID, object Sync)
        {
            sync = Sync;
            channelId = ChannelID;
            hJ2534MessageArray = new HeapJ2534MessageArray(ProtocolID, CONST.HEAPMESSAGEBUFFERSIZE);
            API = Device.API;
            this.Device = Device;
            this.ProtocolID = ProtocolID;
            this.ConnectFlags = ConnectFlags;
            DefaultTxTimeout = 450;
            DefaultRxTimeout = 450;
            DefaultTxFlag = J2534TxFlag.NONE;
        }
        /// <summary>
        /// Gets a single message using the DefaultRxTimeout
        /// </summary>
        /// <returns></returns>
        public GetMessageResults GetMessage()
        {
            return GetMessages(1, DefaultRxTimeout);
        }

        /// <summary>
        /// Reads 'NumMsgs' messages from the input buffer using the DefaultRxTimeout
        /// </summary>
        /// <param name="NumMsgs">The number of messages to return. Due to timeout, the number of messages returned may be less than the number requested.  Number must be less than or equal to J2534.CONST.HEAPMESSAGEBUFFERSIZE (default is 200)</param>
        public GetMessageResults GetMessages(int NumMsgs)
        {
            return GetMessages(NumMsgs, DefaultRxTimeout);
        }
        /// <summary>
        /// Reads 'NumMsgs' messages from the input buffer.
        /// </summary>
        /// <param name="NumMsgs">The number of messages to return. Due to timeout, the number of messages returned may be less than the number requested.  Number must be less than or equal to J2534.CONST.HEAPMESSAGEBUFFERSIZE (default is 200)</param>
        /// <param name="Timeout">Timeout (in milliseconds) for read completion. A value of zero reads buffered messages and returns immediately. A non-zero value blocks (does not return) until the specified number of messages have been read, or until the timeout expires.</param>
        public GetMessageResults GetMessages(int NumMsgs, int Timeout)
        {
            lock (sync)
            {
                hJ2534MessageArray.Length = NumMsgs;
                J2534Err Status = API.PTReadMsgs(channelId, (IntPtr)hJ2534MessageArray, hJ2534MessageArray.LengthPtr, Timeout);
                if(Status != J2534Err.TIMEOUT &&
                   Status != J2534Err.BUFFER_EMPTY)
                {
                    API.CheckStatus(Status);
                }
                return new GetMessageResults(hJ2534MessageArray.ToJ2534MessageArray(), Status);
            }
        }
        /// <summary>
        /// Sends a single message 'Message' created from raw bytes
        /// </summary>
        /// <param name="Message">Raw message bytes to send</param>
        public void SendMessage(IEnumerable<byte> Message)
        {
            lock (sync)
            {
                hJ2534MessageArray.FromDataBytes(Message, DefaultTxFlag);
                SendMessages(hJ2534MessageArray);
            }
        }
        /// <summary>
        /// Sends an array of messages created from raw bytes
        /// </summary>
        /// <param name="Messages">Array of raw message bytes</param>
        public void SendMessages(IEnumerable<byte>[] Messages)
        {
            lock (sync)
            {
                hJ2534MessageArray.FromDataBytesArray(Messages, DefaultTxFlag);
                SendMessages(hJ2534MessageArray);
            }
        }
        /// <summary>
        /// Sends a single J2534Message
        /// </summary>
        /// <param name="Message">J2534Message</param>
        public void SendMessage(J2534Message Message)
        {
            lock (sync)
            {
                hJ2534MessageArray.FromJ2534Message(Message);
                SendMessages(hJ2534MessageArray);
            }
        }
        /// <summary>
        /// Sends an array of J2534Messages
        /// </summary>
        /// <param name="Messages">J2534Message Array</param>
        public void SendMessages(J2534Message[] Messages)
        {
            lock (sync)
            {
                hJ2534MessageArray.FromJ2534MessageArray(Messages);
                SendMessages(hJ2534MessageArray);
            }
        }
        /// <summary>
        /// Sends the contents of a HeapMessageArray
        /// </summary>
        /// <param name="hJ2534MessageArray_Local">HeapMessageArray to send</param>
        public void SendMessages(HeapJ2534MessageArray hJ2534MessageArray_Local)
        {
            lock (sync)
                API.CheckStatus(API.PTWriteMsgs(channelId,
                                                (IntPtr)this.hJ2534MessageArray,
                                                hJ2534MessageArray_Local.LengthPtr,
                                                DefaultTxTimeout));
        }
        /// <summary>
        /// Starts automated periodic transmission of a message
        /// </summary>
        /// <param name="PeriodicMessage">Periodic message object</param>
        /// <returns>Message index</returns>
        public int StartPeriodicMessage(PeriodicMessage PeriodicMessage)
        {
            using(HeapInt hMessageID = new HeapInt())
            using(HeapJ2534Message hPeriodicMessage = new HeapJ2534Message(ProtocolID, PeriodicMessage))
            {
                lock (sync)
                {
                    API.CheckStatus(API.PTStartPeriodicMsg(channelId,
                                                           (IntPtr)hPeriodicMessage,
                                                           (IntPtr)hMessageID,
                                                           PeriodicMessage.Interval));
                    PeriodicMessage.MessageID = hMessageID.Value;
                    PeriodicMsgList.Add(PeriodicMessage);
                }
                return PeriodicMsgList.IndexOf(PeriodicMessage);
            }
        }

        /// <summary>
        /// Stops automated transmission of a periodic message.
        /// </summary>
        /// <param name="Index"Message index>Message index</param>
        public void StopPeriodicMsg(int Index)
        {
            lock (sync)
            {
                API.CheckStatus(API.PTStopPeriodicMsg(channelId, PeriodicMsgList[Index].MessageID));
            }
        }

        /// <summary>
        /// Starts a message filter
        /// </summary>
        /// <param name="Filter">Message filter object</param>
        /// <returns>Filter index</returns>
        public int StartMsgFilter(MessageFilter Filter)
        {
            using (HeapInt hFilterID = new HeapInt())
            using (HeapJ2534Message hMask = new HeapJ2534Message(ProtocolID, Filter.TxFlags, Filter.Mask))
            using (HeapJ2534Message hPattern = new HeapJ2534Message(ProtocolID, Filter.TxFlags, Filter.Pattern))
            using (HeapJ2534Message hFlowControl = new HeapJ2534Message(ProtocolID, Filter.TxFlags, Filter.FlowControl))
            {
                lock (sync)
                {
                    API.CheckStatus(API.PTStartMsgFilter(channelId,
                                                         (int)Filter.FilterType,
                                                         (IntPtr)hMask,
                                                         (IntPtr)hPattern,
                                                         Filter.FilterType == J2534Filter.FLOW_CONTROL_FILTER ? (IntPtr)hFlowControl : IntPtr.Zero,
                                                         (IntPtr)hFilterID));
                    Filter.FilterId = hFilterID.Value;
                    filterList.Add(Filter);
                }
                return filterList.IndexOf(Filter);
            }
        }
        /// <summary>
        /// Stops a message filter
        /// </summary>
        /// <param name="Index">Filter index</param>
        public void StopMsgFilter(int Index)
        {
            lock (sync)
            {
                API.CheckStatus(API.PTStopMsgFilter(channelId, filterList[Index].FilterId));
                filterList.RemoveAt(Index);
            }
        }
        /// <summary>
        /// Gets a configuration parameter for the channel
        /// </summary>
        /// <param name="Parameter">Parameter to return</param>
        /// <returns>Parameter value</returns>
        public int GetConfig(J2534Parameter Parameter)
        {
            using (HeapSConfigArray hSConfigArray = new HeapSConfigArray(new J2534SConfig(Parameter, 0)))
            {
                lock (sync)
                {
                    API.CheckStatus(API.PTIoctl(channelId, (int)J2534IOCTL.GET_CONFIG, (IntPtr)hSConfigArray, IntPtr.Zero));
                }
                return hSConfigArray[0].Value;
            }
        }
        /// <summary>
        /// Sets a configuration parameter for the channel
        /// </summary>
        /// <param name="Parameter">Parameter to set</param>
        /// <param name="Value">Parameter value</param>
        public void SetConfig(J2534Parameter Parameter, int Value)
        {
            using (HeapSConfigArray hSConfigList = new HeapSConfigArray(new J2534SConfig(Parameter, Value)))
            {
                lock (sync)
                {
                    API.CheckStatus(API.PTIoctl(channelId, (int)J2534IOCTL.SET_CONFIG, (IntPtr)hSConfigList, IntPtr.Zero));
                }
            }
        }
        /// <summary>
        /// Gets a list of configuration parameters for the channel
        /// </summary>
        /// <param name="Parameter">List of parameters to get</param>
        /// <returns>Parameter list</returns>
        public J2534SConfig[] GetConfig(J2534SConfig[] SConfig)
        {
            using (HeapSConfigArray hSConfigArray = new HeapSConfigArray(SConfig))
            {
                lock (sync)
                {
                    API.CheckStatus(API.PTIoctl(channelId, (int)J2534IOCTL.GET_CONFIG, (IntPtr)hSConfigArray, IntPtr.Zero));
                }
                return hSConfigArray.ToJ2534SConfigArray();
            }
        }
        /// <summary>
        /// Sets a list of configuration parameters for the channel
        /// </summary>
        /// <param name="Parameter">List of parameters to set</param>
        public void SetConfig(J2534SConfig[] SConfig)
        {
            using (HeapSConfigArray hSConfigList = new HeapSConfigArray(SConfig))
            {
                lock (sync)
                {
                    API.CheckStatus(API.PTIoctl(channelId, (int)J2534IOCTL.SET_CONFIG, (IntPtr)hSConfigList, IntPtr.Zero));
                }
            }
        }
        /// <summary>
        /// Empties the transmit buffer for this channel
        /// </summary>
        public void ClearTxBuffer()
        {
            lock (sync)
            {
                API.CheckStatus(API.PTIoctl(channelId, (int)J2534IOCTL.CLEAR_TX_BUFFER, IntPtr.Zero, IntPtr.Zero));
            }
        }
        /// <summary>
        /// Empties the receive buffer for this channel
        /// </summary>
        public void ClearRxBuffer()
        {
            lock (sync)
            {
                API.CheckStatus(API.PTIoctl(channelId, (int)J2534IOCTL.CLEAR_RX_BUFFER, IntPtr.Zero, IntPtr.Zero));
            }
        }
        /// <summary>
        /// Stops and clears any periodic messages that have been configured for this channel
        /// </summary>
        public void ClearPeriodicMsgs()
        {
            lock (sync)
            {
                API.CheckStatus(API.PTIoctl(channelId, (int)J2534IOCTL.CLEAR_PERIODIC_MSGS, IntPtr.Zero, IntPtr.Zero));
            }
        }
        /// <summary>
        /// Stops and clears any message filters that have been configured for this channel
        /// </summary>
        public void ClearMsgFilters()
        {
            lock (sync)
            {
                API.CheckStatus(API.PTIoctl(channelId, (int)J2534IOCTL.CLEAR_MSG_FILTERS, IntPtr.Zero, IntPtr.Zero));
            }
        }
        /// <summary>
        /// Stops and clears all functional message address filters configured for this channel
        /// </summary>
        public void ClearFunctMsgLookupTable()
        {
            lock (sync)
            {
                API.CheckStatus(API.PTIoctl(channelId, (int)J2534IOCTL.CLEAR_FUNCT_MSG_LOOKUP_TABLE, IntPtr.Zero, IntPtr.Zero));
            }
        }
        /// <summary>
        /// Starts a functional message address filter for this channel
        /// </summary>
        /// <param name="Addr">Address to pass</param>
        public void AddToFunctMsgLookupTable(byte Addr)
        {
            using (HeapSByteArray hSByteArray = new HeapSByteArray(Addr))
            {
                lock (sync)
                {
                    API.CheckStatus(API.PTIoctl(channelId, (int)J2534IOCTL.ADD_TO_FUNCT_MSG_LOOKUP_TABLE, (IntPtr)hSByteArray, IntPtr.Zero));
                }
            }
        }
        /// <summary>
        /// Starts a list of functional message address filters for this channel
        /// </summary>
        /// <param name="AddressList">Address list to pass</param>
        public void AddToFunctMsgLookupTable(List<byte> AddressList)
        {
            using (HeapSByteArray hSByteArray = new HeapSByteArray(AddressList.ToArray()))
            {
                lock (sync)
                {
                    API.CheckStatus(API.PTIoctl(channelId, (int)J2534IOCTL.ADD_TO_FUNCT_MSG_LOOKUP_TABLE, (IntPtr)hSByteArray, IntPtr.Zero));
                }
            }
        }
        /// <summary>
        /// Stops and clears a single functional address message filter for this channel
        /// </summary>
        /// <param name="Addr">Address to remove</param>
        public void DeleteFromFunctMsgLookupTable(byte Addr)
        {
            using (HeapSByteArray hSByteArray = new HeapSByteArray(Addr))
            {
                lock (sync)
                {
                    API.CheckStatus(API.PTIoctl(channelId, (int)J2534IOCTL.DELETE_FROM_FUNCT_MSG_LOOKUP_TABLE, (IntPtr)hSByteArray, IntPtr.Zero));
                }
            }
        }
        /// <summary>
        /// Stops and clears a list of functional address filters for this channel
        /// </summary>
        /// <param name="AddressList">Address list to stop</param>
        public void DeleteFromFunctMsgLookupTable(IEnumerable<byte> AddressList)
        {
            using (HeapSByteArray hSByteArray = new HeapSByteArray(AddressList.ToArray()))
            {
                lock (sync)
                {
                    API.CheckStatus(API.PTIoctl(channelId, (int)J2534IOCTL.DELETE_FROM_FUNCT_MSG_LOOKUP_TABLE, (IntPtr)hSByteArray, IntPtr.Zero));
                }
            }
        }
        /// <summary>
        /// Performs a 5 baud handshake for ISO9141 initialization
        /// </summary>
        /// <param name="TargetAddress">Address to handshake with</param>
        /// <returns>byte[2]</returns>
        public byte[] FiveBaudInit(byte TargetAddress)
        {
            using (HeapSByteArray hInput = new HeapSByteArray(new byte[] { TargetAddress }))
            using (HeapSByteArray hOutput = new HeapSByteArray(new byte[2]))
            {
                lock (sync)
                {
                    API.CheckStatus(API.PTIoctl(channelId, (int)J2534IOCTL.FIVE_BAUD_INIT, (IntPtr)hInput, (IntPtr)hOutput));
                }
                return hOutput.ToSByteArray();
            }
        }
        /// <summary>
        /// Performs a fast initialzation sequence
        /// </summary>
        /// <param name="TxMessage"></param>
        /// <returns></returns>
        public J2534Message FastInit(J2534Message TxMessage)
        {
            using (HeapJ2534Message hInput = new HeapJ2534Message(ProtocolID, TxMessage.TxFlags, TxMessage.Data))
            using (HeapJ2534Message hOutput = new HeapJ2534Message(ProtocolID))
            {
                lock (sync)
                {
                    API.CheckStatus(API.PTIoctl(channelId, (int)J2534IOCTL.FAST_INIT, (IntPtr)hInput, (IntPtr)hOutput));
                }
                return hOutput.ToJ2534Message();
            }
        }
        /// <summary>
        /// Turns on the programming voltage for the device
        /// </summary>
        /// <param name="PinNumber">Pin number</param>
        /// <param name="Voltage">voltage (mV)</param>
        public void SetProgrammingVoltage(J2534Pin PinNumber, int Voltage)
        {
            Device.SetProgrammingVoltage(PinNumber, Voltage);
        }
        /// <summary>
        /// Measures the delivered programming voltage
        /// </summary>
        /// <returns>Voltage (mV)</returns>
        public int MeasureProgrammingVoltage()
        {
            return Device.MeasureProgrammingVoltage();
        }
        /// <summary>
        /// Measures the vehicle supply voltage
        /// </summary>
        /// <returns>Voltage (mV)</returns>
        public int MeasureBatteryVoltage()
        {
            return Device.MeasureBatteryVoltage();
        }

        protected override void DisposeManaged()
        {
            API.PTDisconnect(channelId);
            hJ2534MessageArray?.Dispose();
        }
    }
}
