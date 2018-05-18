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
using J2534.Common;
using J2534.DataClasses;
using J2534.Definitions;

namespace J2534
{
    public partial class J2534Device : Disposable
    {
        private int deviceId;
        private object sync;
        internal J2534API API { get; }
        public string FirmwareVersion { get; }
        public string DriverVersion { get; }
        public string APIVersion { get; }
        public string DeviceName { get; }

        internal J2534Device(J2534API API, string DeviceName, int DeviceID, object Sync)
        {
            this.API = API;
            this.DeviceName = DeviceName;
            deviceId = DeviceID;
            sync = Sync;

            using (var hFirmwareVersion = new HeapString(80))
            using (var hDllVersion = new HeapString(80))
            using (var hApiVersion = new HeapString(80))
            {
                lock (Sync)
                {
                    API.CheckStatus(API.PTReadVersion(DeviceID, (IntPtr)hFirmwareVersion, (IntPtr)hDllVersion, (IntPtr)hApiVersion));
                }
                FirmwareVersion = hFirmwareVersion.ToString();
                DriverVersion = hDllVersion.ToString();
                APIVersion = hApiVersion.ToString();
            }
        }
        /// <summary>
        /// Turns on the programming voltage for the device
        /// </summary>
        /// <param name="PinNumber">Pin number</param>
        /// <param name="Voltage">voltage (mV)</param>
        public void SetProgrammingVoltage(J2534Pin PinNumber, int Voltage)
        {
            lock (sync)
            {
                API.CheckStatus(API.PTSetProgrammingVoltage(deviceId, (int)PinNumber, Voltage));
            }
        }
        /// <summary>
        /// Measures the vehicle supply voltage
        /// </summary>
        /// <returns>Voltage (mV)</returns>
        public int MeasureBatteryVoltage()
        {
            using (HeapInt hVoltage = new HeapInt())
            {
                lock (sync)
                {
                    API.CheckStatus(API.PTIoctl(deviceId, (int)J2534IOCTL.READ_VBATT, IntPtr.Zero, (IntPtr)hVoltage));
                }
                return hVoltage.Value;
            }
        }
        /// <summary>
        /// Measures the delivered programming voltage
        /// </summary>
        /// <returns>Voltage (mV)</returns>
        public int MeasureProgrammingVoltage()
        {
            using (HeapInt hVoltage = new HeapInt())
            {
                lock (sync)
                {
                    API.CheckStatus(API.PTIoctl(deviceId, (int)J2534IOCTL.READ_PROG_VOLTAGE, IntPtr.Zero, (IntPtr)hVoltage));
                }
                return hVoltage.Value;
            }
        }
        /// <summary>
        /// Opens a channel on the device using the specified parameters
        /// </summary>
        /// <param name="ProtocolID">Connection protocol</param>
        /// <param name="Baud">Connection baud-rate</param>
        /// <param name="ConnectFlags">Connection flags</param>
        /// <returns>A connected J2534Channel object</returns>
        public J2534Channel GetChannel(J2534Protocol ProtocolID, J2534Baud Baud, J2534CONNECTFLAG ConnectFlags, bool ChannelLevelSync = false)
        {
            using (HeapInt hChannelID = new HeapInt())
            {
                lock (sync)
                {
                    API.CheckStatus(API.PTConnect(deviceId, (int)ProtocolID, (int)ConnectFlags, (int)Baud, (IntPtr)hChannelID));
                }
                var NewChannel = new J2534Channel(this, ProtocolID, Baud, ConnectFlags, hChannelID.Value, ChannelLevelSync ? new object() : sync);
                OnDisposing += NewChannel.Dispose;
                return NewChannel;
            }
        }

        protected override void DisposeManaged()
        {
            API.PTClose(deviceId);
        }
    }
}
