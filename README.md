### J2534-Sharp ###

J2534-Sharp handles all the details of operating with un unmanaged SAE J2534 spec library and lets you deal with the important stuff.

Available on Nuget! [NuGet Gallery: j2534-sharp]

## Features ##

- No 'Unsafe' code.  All unmanaged memory references are done using the marshaller
- Thread safe design.   Locking is done on API calls to allow concurrent access.
- Simplified API.  Most API calls have had redundant data removed and offer method signatures for common use cases
- Support for v2.02 and v4.04 J2534 standards.  v2.02 librarys are detected and 'shimmed' to a v4.04 interface seamlessly
- Support for v5.00.  v5 J2534 support has been started, but I need more info to complete it.
- Support for DrewTech API.  Support has been included for undocumented DrewTech API calls

## Usage ##

The traditional usage will use explicit filter definition and using disposables within using's'.

## Traditional usage ##

```csharp
using System;
using System.Linq;

using J2534;
using J2534.Definitions;
using J2534.DataClasses;

namespace J5234Examples
{
    class Program
    {
        static void Main(string[] args)
        {
            MessageFilter PassFilter = new MessageFilter()
            {
                FilterType = J2534Filter.PASS_FILTER,
                Mask = new byte[] { 0xFF, 0xFF, 0xFF, 0xFF },
                Pattern = new byte[] { 0x00, 0x00, 0x07, 0xE8 }
            };
            MessageFilter FlowControlFilter = new MessageFilter()
            {
                FilterType = J2534Filter.FLOW_CONTROL_FILTER,
                Mask = new byte[] { 0xFF, 0xFF, 0xFF, 0xFF },
                Pattern = new byte[] { 0x00, 0x00, 0x07, 0xE0 },
                FlowControl = new byte[] { 0x00, 0x00, 0x07, 0xE8 }
            };

            string DllFileName = APIFactory.GetAPIList().First().Filename;

            using (J2534API API = APIFactory.GetAPI(DllFileName))
            using (J2534Device Device = API.GetDevice())
            using (J2534Channel Channel = Device.GetChannel(J2534Protocol.ISO15765, J2534Baud.ISO15765, J2534CONNECTFLAG.NONE))
            {

                Channel.StartMsgFilter(PassFilter);
                Channel.StartMsgFilter(FlowControlFilter);
                Console.WriteLine($"Voltage is {Channel.MeasureBatteryVoltage() / 1000}");
                Channel.SendMessage(new byte[] { 0x00, 0x00, 0x07, 0xE0, 0x01, 0x00 });
                GetMessageResults Response = Channel.GetMessage();
            }
        }
    }
}
```
The simplified usage take advantage of built in 'templates' for creating the filters

## Simplified usage ##

```csharp
using System;
using System.Linq;

using J2534;
using J2534.Definitions;
using J2534.DataClasses;

namespace J5234Examples
{
    class Program
    {
        static void Main(string[] args)
        {
            string DllFileName = APIFactory.GetAPIList().First().Filename;

            using (J2534API API = APIFactory.GetAPI(DllFileName))
            using (J2534Device Device = API.GetDevice())
            using (J2534Channel Channel = Device.GetChannel(J2534Protocol.ISO15765, J2534Baud.ISO15765, J2534CONNECTFLAG.NONE))
            {
                Channel.StartMsgFilter(new MessageFilter(UserFilterType.PASS, new byte[] { 0x00, 0x00, 0x07, 0xE0}));
                Channel.StartMsgFilter(new MessageFilter(UserFilterType.STANDARDISO15765, new byte[] { 0x00, 0x00, 0x07, 0xE0}));
                Console.WriteLine($"Voltage is {Channel.MeasureBatteryVoltage() / 1000}");
                Channel.SendMessage(new byte[] { 0x00, 0x00, 0x07, 0xE0, 0x01, 0x00 });
                GetMessageResults Response = Channel.GetMessage();
            }
        }
    }
}
```

Alternately, the API factory can be instanciated as an instance, and when disposed, will dispose all children with it.  This negates the need for explicit using's'
except for the initial one for the APIFactory.  NOTE:  The APIFactory instance is only used to facilitate the disposal, and the instance does not need to be passed
around.

## Alternate usage of the APIFactory ##

```csharp
using System;
using System.Linq;

using J2534;
using J2534.Definitions;
using J2534.DataClasses;

namespace J5234Examples
{
    class Program
    {
        static void Main(string[] args)
        {
            using (var factory = new APIFactory())
            {
                Run();
            }
        }
        static void Run()
        {
            string DllFileName = APIFactory.GetAPIList().First().Filename;

            J2534API API = APIFactory.GetAPI(DllFileName);
            J2534Device Device = API.GetDevice();
            J2534Channel Channel = Device.GetChannel(J2534Protocol.ISO15765, J2534Baud.ISO15765, J2534CONNECTFLAG.NONE);

            Channel.StartMsgFilter(new MessageFilter(UserFilterType.PASS, new byte[] { 0x00, 0x00, 0x07, 0xE0}));
            Channel.StartMsgFilter(new MessageFilter(UserFilterType.STANDARDISO15765, new byte[] { 0x00, 0x00, 0x07, 0xE0 }));
            Console.WriteLine($"Voltage is {Channel.MeasureBatteryVoltage() / 1000}");
            Channel.SendMessage(new byte[] { 0x00, 0x00, 0x07, 0xE0, 0x01, 0x00 });
            GetMessageResults Response = Channel.GetMessage();
        }
    }
}
```