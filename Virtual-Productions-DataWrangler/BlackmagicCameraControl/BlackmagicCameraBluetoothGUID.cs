using System;
using Windows.Devices.Bluetooth;

namespace BlackmagicCameraControl
{
	internal static class BlackmagicCameraBluetoothGUID
	{
		public static readonly Guid DeviceInformationServiceUUID = BluetoothUuidHelper.FromShortId(0x180A);
		public static readonly Guid DeviceInformationService_CameraManufacturer = BluetoothUuidHelper.FromShortId(0x2A29);
		public static readonly Guid DeviceInformationService_CameraModel = BluetoothUuidHelper.FromShortId(0x2A24);

		public static readonly Guid BlackmagicServiceUUID = Guid.Parse("291D567A-6D75-11E6-8B77-86F30CA893D3");
		public static readonly Guid BlackmagicService_OutgoingCameraControl = Guid.Parse("5DD3465F-1AEE-4299-8493-D2ECA2F8E1BB");
		public static readonly Guid BlackmagicService_IncomingCameraControl = Guid.Parse("B864E140-76A0-416A-BF30-5876504537D9");
		public static readonly Guid BlackmagicService_Timecode = Guid.Parse("6D8F2110-86F1-41BF-9AFB-451D87E976C8");
	}
}
