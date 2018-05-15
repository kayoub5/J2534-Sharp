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
using System.Runtime.InteropServices;
using J2534.Definitions;

namespace J2534
{
    public partial class J2534API
    {
        private string shim_DeviceName = String.Empty;
        private int shim_DeviceID = 0;
        private bool shim_IsOpen = false;
        private J2534Err Open_shim(IntPtr pDeviceName, IntPtr pDeviceID)
        {
            string DeviceName = pDeviceName == IntPtr.Zero ? String.Empty : Marshal.PtrToStringAnsi(pDeviceName);
            if (!shim_IsOpen)
            {
                shim_DeviceName = DeviceName;
                shim_IsOpen = true;
                return J2534Err.STATUS_NOERROR;
            }
            if (shim_IsOpen && (DeviceName == this.shim_DeviceName)) return J2534Err.DEVICE_IN_USE;

            return J2534Err.INVALID_DEVICE_ID;
        }

        private J2534Err Close_shim(int DeviceID)
        {
            if (!shim_IsOpen || DeviceID != shim_DeviceID) return J2534Err.INVALID_DEVICE_ID;
            return J2534Err.STATUS_NOERROR;
        }

        private J2534Err Connect_shim(int DeviceID, int ProtocolID, int ConnectFlags, int Baud, IntPtr ChannelID)
        {
            if (DeviceID != shim_DeviceID) return J2534Err.INVALID_DEVICE_ID;
            return PTConnectv202(ProtocolID, ConnectFlags, ChannelID);
        }

        private J2534Err SetVoltage_shim(int DeviceID, int Pin, int Voltage)
        {
            if (DeviceID != shim_DeviceID) return J2534Err.INVALID_DEVICE_ID;
            return PTSetProgrammingVoltagev202(Pin, Voltage);
        }

        private J2534Err ReadVersion_shim(int DeviceID, IntPtr pFirmwareVer, IntPtr pDllVer, IntPtr pAPIVer)
        {
            if (DeviceID != shim_DeviceID) return J2534Err.INVALID_DEVICE_ID;
            return PTReadVersionv202(pFirmwareVer, pDllVer, pAPIVer);
        }
    }
}
