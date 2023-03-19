namespace CameraControlOverEthernet
{
	internal class CameraControlTransport
	{
		public static void Write(ICameraControlPacket a_packet, BinaryWriter a_writer)
		{
			CommandControlPacketMeta meta = CameraControlPacketFactory.GetMeta(a_packet);
			a_writer.Write(meta.Identifier);
			foreach (CommandControlPacketFieldMeta field in meta.Fields)
			{
				field.Write(a_writer, a_packet);
			}
		}

		public static ICameraControlPacket? TryRead(BinaryReader a_reader)
		{
			ICameraControlPacket? packet = null;
			
			long bytesRemaining = a_reader.BaseStream.Length - a_reader.BaseStream.Position;
			const int PacketIdentifierSize = sizeof(uint);
			if (bytesRemaining >= PacketIdentifierSize)
			{
				uint identifier = a_reader.ReadUInt32();
				CommandControlPacketMeta? meta = CameraControlPacketFactory.FindMeta(identifier);
				if (meta != null)
				{
					packet = meta.CreateDefaultedInstance();
					foreach (CommandControlPacketFieldMeta field in meta.Fields)
					{
						field.Read(a_reader, packet);
					}
				}
			}

			return packet;
		}
	}
}
