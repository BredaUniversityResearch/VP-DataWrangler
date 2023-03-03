using BlackmagicCameraControlData;

namespace BlackmagicCameraControl.CommandPackets
{
	[CommandPacketMeta(12, 11, 0, ECommandDataType.Utf8String)]
	public class CommandPacketVendorLensZoom: ICommandPacketBase
	{
		public string ZoomAmount;	//

		public CommandPacketVendorLensZoom(CommandReader a_reader)
		{
			ZoomAmount = a_reader.ReadString();
		}

		public override void WriteTo(CommandWriter a_writer)
		{
			a_writer.Write(ZoomAmount);
		}

		public override bool Equals(ICommandPacketBase? a_other)
		{
			CommandPacketVendorLensZoom? other = (CommandPacketVendorLensZoom?)a_other;
			return other != null &&
			       other.ZoomAmount == ZoomAmount;
		}

		public override string ToString()
		{
			return $"{GetType().Name} [{ZoomAmount}]";
		}
	}
}
