using BlackmagicCameraControlData;
using System;
using System.Diagnostics;

namespace BlackmagicCameraControl.CommandPackets
{
	[CommandPacketMeta(9, 4, 4, ECommandDataType.Int32)]
	public class CommandPacketSystemReferenceTime : ICommandPacketBase
	{
		public int ReferenceTime = 0; //BCD

		public CommandPacketSystemReferenceTime()
		{
		}

		public CommandPacketSystemReferenceTime(CommandReader a_reader)
		{
			ReferenceTime = a_reader.ReadInt32();
		}

		public override void WriteTo(CommandWriter a_writer)
		{
			a_writer.Write(ReferenceTime);
		}

		public override bool Equals(ICommandPacketBase? a_other)
		{
			CommandPacketSystemReferenceTime? other = (CommandPacketSystemReferenceTime?)a_other;
			return other != null &&
			       other.ReferenceTime == ReferenceTime;
		}

		public override string ToString()
		{
			return $"{GetType().Name} [{ReferenceTime:X}]";
		}
	}
}