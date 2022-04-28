using System;

namespace BlackmagicCameraControl.CommandPackets
{
	[Serializable]
	public class CommandHeader
	{
		public const long ByteSize = 4;

		public CommandIdentifier CommandIdentifier;
		public ECommandDataType DataType;
		public ECommandOperation Operation;
	}
}
