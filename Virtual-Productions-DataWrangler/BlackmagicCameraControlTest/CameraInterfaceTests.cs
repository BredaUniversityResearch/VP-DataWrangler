using System;
using System.Threading;
using BlackmagicCameraControl;
using BlackmagicCameraControl.CommandPackets;
using Xunit;

namespace BlackmagicCameraControlTest
{
	public class CameraInterfaceTests
	{
		[Fact]
		public void ConnectToCamera()
		{
			using BlackmagicCameraController iface = new BlackmagicCameraController();
			CreateCameraConnection(iface);
		}

		[Fact]
		public void ReceiveBatteryPercentage()
		{
			using BlackmagicCameraController iface = new BlackmagicCameraController();
			CameraHandle handle = CreateCameraConnection(iface);
			ManualResetEvent waitHandle = new ManualResetEvent(false);
			iface.OnCameraDataReceived += (a_source, a_timeReceived, a_packet) =>
			{
				if (a_packet is CommandPacketBatteryInfo)
				{
					waitHandle.Set();
				}
			};
			Assert.True(waitHandle.WaitOne(15000));
		}

		private CameraHandle CreateCameraConnection(BlackmagicCameraController a_controller)
		{
			CameraHandle cameraHandle = CameraHandle.Invalid;
			ManualResetEvent waitHandle = new ManualResetEvent(false);
			a_controller.OnCameraConnected += (a_cameraHandle) =>
			{
				cameraHandle = a_cameraHandle;
				waitHandle.Set();
			};

			Assert.True( a_controller.ConnectedCameraCount == 1 || waitHandle.WaitOne(15000), "Camera failed to connect");
			return a_controller.GetConnectedCameraByIndex(0);
		}
	}
}