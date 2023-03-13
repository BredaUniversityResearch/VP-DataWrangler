namespace BlackmagicCameraControlData.CommandPackets
{
	[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
	public class CommandPacketMetaAttribute: Attribute
	{
		public CommandIdentifier Identifier;
		public int SerializedSizeBytes;
		public ECommandDataType DataType;

		public CommandPacketMetaAttribute(byte a_commandCategory, byte a_commandParameter, int a_serializedSizeBytes, ECommandDataType a_dataType)
		{
			Identifier = new CommandIdentifier(a_commandCategory, a_commandParameter);
			SerializedSizeBytes = a_serializedSizeBytes;
			DataType = a_dataType;
		}
	}
}
