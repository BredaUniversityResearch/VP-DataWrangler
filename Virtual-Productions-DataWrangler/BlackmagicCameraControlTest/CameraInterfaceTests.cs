using System;
using System.Threading;
using BlackmagicCameraControl;
using BlackmagicCameraControlBluetooth;
using BlackmagicCameraControlData;
using BlackmagicCameraControlData.CommandPackets;
using Xunit;

namespace BlackmagicCameraControlTest
{
	public class CameraInterfaceTests
	{
		[Fact]
		public void ConnectToCamera()
		{
			using BlackmagicBluetoothCameraAPIController iface = new BlackmagicBluetoothCameraAPIController();
			iface.Start();
			CreateCameraConnection(iface);
		}

		[Fact]
		public void ReceiveBatteryPercentage()
		{
			using BlackmagicBluetoothCameraAPIController iface = new BlackmagicBluetoothCameraAPIController();
			iface.Start();
			CameraDeviceHandle deviceHandle = CreateCameraConnection(iface);
			ManualResetEvent waitHandle = new ManualResetEvent(false);
			iface.OnCameraDataReceived += (a_source, a_timeReceived, a_packet) =>
			{
				if (a_packet is CommandPacketSystemBatteryInfo)
				{
					waitHandle.Set();
				}
			};
			Assert.True(waitHandle.WaitOne(15000));
		}

		private CameraDeviceHandle CreateCameraConnection(BlackmagicBluetoothCameraAPIController a_apiController)
		{
			CameraDeviceHandle? cameraDeviceHandle = null;
			ManualResetEvent waitHandle = new ManualResetEvent(false);
			a_apiController.OnCameraConnected += (a_cameraHandle) =>
			{
				cameraDeviceHandle = a_cameraHandle;
				waitHandle.Set();
			};

			Assert.True( a_apiController.ConnectedCameraCount == 1 || waitHandle.WaitOne(15000), "Camera failed to connect");
			return a_apiController.GetConnectedCameraByIndex(0);
		}
	}
}