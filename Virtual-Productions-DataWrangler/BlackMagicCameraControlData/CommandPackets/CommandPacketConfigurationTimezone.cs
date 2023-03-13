namespace BlackmagicCameraControlData.CommandPackets
{
	[CommandPacketMeta(7, 2, 4, ECommandDataType.Int32)]
	public class CommandPacketConfigurationTimezone: ICommandPacketBase
	{
		public int MinutesOffsetFromUTC = 0;

		public CommandPacketConfigurationTimezone(TimeZoneInfo a_timeZone)
		{
			MinutesOffsetFromUTC = (int)a_timeZone.BaseUtcOffset.TotalMinutes;
			if (a_timeZone.IsDaylightSavingTime(DateTime.UtcNow))
			{
				foreach (TimeZoneInfo.AdjustmentRule rule in a_timeZone.GetAdjustmentRules())
				{
					MinutesOffsetFromUTC += (int) rule.DaylightDelta.TotalMinutes;
				}
			}
		}

		public CommandPacketConfigurationTimezone(CommandReader a_reader)
		{
			MinutesOffsetFromUTC = a_reader.ReadInt32();
		}

		public override void WriteTo(CommandWriter a_writer)
		{
			a_writer.Write(MinutesOffsetFromUTC);
		}

		public override bool Equals(ICommandPacketBase? a_other)
		{
			CommandPacketConfigurationTimezone? other = (CommandPacketConfigurationTimezone?)a_other;
			return other != null &&
			       other.MinutesOffsetFromUTC == MinutesOffsetFromUTC;
		}

		public override string ToString()
		{
			return $"{GetType().Name} [{MinutesOffsetFromUTC}]";
		}
	}
}
