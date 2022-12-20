using System;
using BlackmagicCameraControlData.CommandPackets;

namespace BlackmagicCameraControl.CommandPackets
{
	[Serializable]
	public class CommandHeader
	{
		public const long ByteSize = 4;

		public CommandIdentifier CommandIdentifier;
		public ECommandDataType DataType;
		public ECommandOperation Operation;

		public void WriteTo(CommandWriter a_writer)
		{
			a_writer.Write(CommandIdentifier);
			a_writer.Write((byte) DataType);
			a_writer.Write((byte) Operation);
		}
	}
}
