namespace BlackmagicCameraControlData.CommandPackets
{
	[CommandPacketMeta(12, 1, 3, ECommandDataType.Int8)]
	public class CommandPacketMetadataSceneTags : ICommandPacketBase
	{
		public enum ESceneTags : sbyte
		{
			None = -1,
			WS = 0,
			CU = 1,
			MS = 2,
			BCU = 3,
			MCU = 4,
			ECU = 5
		}

		public ESceneTags SceneTags;	
		public byte ExteriorOrInterior;	// 0: Exterior, 1: Interior
		public byte NightOrDay;	//0: Night, 1: Day

		public CommandPacketMetadataSceneTags(CommandReader a_reader)
		{
			SceneTags = (ESceneTags)a_reader.ReadSInt8();
			ExteriorOrInterior = a_reader.ReadInt8();
			NightOrDay = a_reader.ReadInt8();
		}

		public override void WriteTo(CommandWriter a_writer)
		{
			a_writer.Write((sbyte)SceneTags);
			a_writer.Write(ExteriorOrInterior);
			a_writer.Write(NightOrDay);
		}

		public override bool Equals(ICommandPacketBase? a_other)
		{
			CommandPacketMetadataSceneTags? other = (CommandPacketMetadataSceneTags?)a_other;
			return other != null &&
			       other.SceneTags == SceneTags &&
			       other.ExteriorOrInterior == ExteriorOrInterior &&
			       other.NightOrDay == NightOrDay;
		}

		public override string ToString()
		{
			return $"{GetType().Name} [{SceneTags}, {ExteriorOrInterior}, {NightOrDay}]";
		}
	}
}
