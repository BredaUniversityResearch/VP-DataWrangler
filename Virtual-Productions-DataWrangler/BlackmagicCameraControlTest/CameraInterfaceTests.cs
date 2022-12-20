using System;
using System.Threading;
using BlackmagicCameraControl;
using BlackmagicCameraControl.CommandPackets;
using BlackmagicCameraControlBluetooth;
using BlackmagicCameraControlData;
using Xunit;

namespace BlackmagicCameraControlTest
{
	public class CameraInterfaceTests
	{
		[Fact]
		public void ConnectToCamera()
		{
			using BlackmagicBluetoothCameraAPIController iface = new BlackmagicBluetoothCameraAPIController();
			CreateCameraConnection(iface);
		}

		[Fact]
		public void ReceiveBatteryPercentage()
		{
			using BlackmagicBluetoothCameraAPIController iface = new BlackmagicBluetoothCameraAPIController();
			CameraHandle handle = CreateCameraConnection(iface);
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

		private CameraHandle CreateCameraConnection(BlackmagicBluetoothCameraAPIController a_apiController)
		{
			CameraHandle cameraHandle = CameraHandle.Invalid;
			ManualResetEvent waitHandle = new ManualResetEvent(false);
			a_apiController.OnCameraConnected += (a_cameraHandle) =>
			{
				cameraHandle = a_cameraHandle;
				waitHandle.Set();
			};

			Assert.True( a_apiController.ConnectedCameraCount == 1 || waitHandle.WaitOne(15000), "Camera failed to connect");
			return a_apiController.GetConnectedCameraByIndex(0);
		}
	}
}