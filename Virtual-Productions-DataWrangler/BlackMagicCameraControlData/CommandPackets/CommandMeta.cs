using System;
using BlackmagicCameraControlData.CommandPackets;

namespace BlackmagicCameraControl.CommandPackets;

public class CommandMeta
{
	public Type CommandType;
	public CommandIdentifier Identifier;
	public int SerializedSizeBytes;
	public ECommandDataType DataType;

	public CommandMeta(Type a_implementedType, CommandIdentifier a_identifier, int a_serializedSizeBytes, ECommandDataType a_dataType)
	{
		CommandType = a_implementedType;
		Identifier = a_identifier;
		SerializedSizeBytes = a_serializedSizeBytes;
		DataType = a_dataType;
	}

};