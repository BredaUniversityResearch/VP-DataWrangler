using System;
using BlackmagicCameraControl.CommandPackets;

namespace BlackmagicCameraControl
{
	public interface IBlackmagicCameraConnection: IDisposable
	{
		public enum EConnectionState
		{
			QueryingProperties,
			Connected,
			Disconnected,
		};

		public CameraHandle CameraHandle { get; }
		public DateTimeOffset LastReceivedDataTime { get; }
		public EConnectionState ConnectionState { get; }
		public string DeviceId { get; }
		public string HumanReadableName { get; }

		public void AsyncSendCommand(ICommandPacketBase a_command);
	}
}
