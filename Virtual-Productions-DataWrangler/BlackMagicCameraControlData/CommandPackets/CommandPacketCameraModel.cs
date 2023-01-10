namespace BlackmagicCameraControl.CommandPackets
{
	public class CommandPacketCameraModel: ICommandPacketBase
	{
		public string CameraModel;

		public CommandPacketCameraModel(string a_cameraModel)
		{
			CameraModel = a_cameraModel;
		}

		public override void WriteTo(CommandWriter a_writer)
		{
			throw new System.NotImplementedException();
		}

		public override bool Equals(ICommandPacketBase? a_other)
		{
			CommandPacketCameraModel? other = (CommandPacketCameraModel?)a_other;
			return other != null && other.CameraModel == CameraModel;
		}

		public override string ToString()
		{
			return $"{GetType().Name} [{CameraModel}]";
		}
	}
}
