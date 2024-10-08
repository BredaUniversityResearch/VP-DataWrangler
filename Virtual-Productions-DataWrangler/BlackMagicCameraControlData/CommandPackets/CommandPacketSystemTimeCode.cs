﻿using DataWranglerCommon;

namespace BlackmagicCameraControlData.CommandPackets
{
	[CommandPacketMeta(9, 4, 4, ECommandDataType.Int32)]
	public class CommandPacketSystemTimeCode : ICommandPacketBase
	{
		public uint BinaryCodedTimeCode = 0; //BCD
		public TimeCode TimeCode = new TimeCode(0, 0, 0, 0); // HH:MM:SS:FF

		public CommandPacketSystemTimeCode()
		{
		}

        public CommandPacketSystemTimeCode(TimeCode a_timeCode)
        {
            BinaryCodedTimeCode = a_timeCode.TimeCodeAsBinaryCodedDecimal;
            TimeCode = a_timeCode;
        }

        public CommandPacketSystemTimeCode(CommandReader a_reader)
		{
			BinaryCodedTimeCode = a_reader.ReadUInt32();
			TimeCode = TimeCode.FromBCD((uint)BinaryCodedTimeCode);
		}

		public override void WriteTo(CommandWriter a_writer)
		{
			a_writer.Write(BinaryCodedTimeCode);
		}

		public override bool Equals(ICommandPacketBase? a_other)
		{
			CommandPacketSystemTimeCode? other = (CommandPacketSystemTimeCode?)a_other;
			return other != null &&
			       other.BinaryCodedTimeCode == BinaryCodedTimeCode;
		}

		public override string ToString()
		{
			return $"{GetType().Name} [{BinaryCodedTimeCode:X}]";
		}
	}
}