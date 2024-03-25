namespace CameraControlOverEthernet
{
	internal class NetworkApiTransport
	{
		public static void Write(INetworkAPIPacket a_packet, BinaryWriter a_writer)
		{
			NetworkAPIPacketMeta meta = NetworkAPIPacketFactory.GetMeta(a_packet);
			a_writer.Write(meta.Identifier);
			foreach (NetworkAPIPacketFieldMeta field in meta.Fields)
			{
				field.Write(a_writer, a_packet);
			}
		}

		public static INetworkAPIPacket? TryRead(BinaryReader a_reader)
		{
			INetworkAPIPacket? packet = null;
			
			long bytesRemaining = a_reader.BaseStream.Length - a_reader.BaseStream.Position;
			const int PacketIdentifierSize = sizeof(uint);
			if (bytesRemaining >= PacketIdentifierSize)
			{
				uint identifier = a_reader.ReadUInt32();
				NetworkAPIPacketMeta? meta = NetworkAPIPacketFactory.FindMeta(identifier);
				if (meta != null)
				{
					packet = meta.CreateDefaultedInstance();
					foreach (NetworkAPIPacketFieldMeta field in meta.Fields)
					{
						try
						{
							field.Read(a_reader, packet);
						}
						catch (EndOfStreamException)
						{
							return null;
						}
					}
				}
			}

			return packet;
		}
	}
}
