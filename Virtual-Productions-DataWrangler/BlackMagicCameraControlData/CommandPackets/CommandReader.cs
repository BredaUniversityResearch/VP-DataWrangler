using BlackmagicCameraControlData;
using System.Reflection.PortableExecutable;
using BlackmagicCameraControlData.CommandPackets;

namespace BlackmagicCameraControl.CommandPackets
{
	public class CommandReader
	{
		private struct StreamSpan
		{
			public long ByteStart;
			public long ByteLength;

			public StreamSpan(long a_byteStart, long a_byteLength)
			{
				ByteStart = a_byteStart;
				ByteLength = a_byteLength;
			}

			public long GetBytesRemaining(Stream a_stream)
			{
				return ByteLength - (a_stream.Position - ByteStart);
			}
		};

		private Stream m_targetStream;
		private byte[] m_smallReadBuffer = new byte[32];

		public long BytesRemaining => (m_targetStream.Length - m_targetStream.Position);
		public long BytesProcessed => m_targetStream.Position;

		public CommandReader(Stream a_stream)
		{
			m_targetStream = a_stream;
		}

        public static void DecodeStream(Stream a_stream, Action<ICommandPacketBase> a_onPacketDecoded)
        {
            CommandReader reader = new CommandReader(a_stream);
            while (reader.BytesRemaining >= PacketHeader.ByteSize)
            {
                PacketHeader packetHeader = reader.ReadPacketHeader();
                if (reader.BytesRemaining < packetHeader.PacketSize && reader.BytesRemaining < byte.MaxValue)
                {
                    throw new Exception();
                }

				StreamSpan commandSpan = new StreamSpan(reader.m_targetStream.Position, packetHeader.PacketSize);
				reader.DecodeCommandStream(commandSpan, a_onPacketDecoded);

				if (reader.m_targetStream.Position != commandSpan.ByteStart + commandSpan.ByteLength)
				{
					throw new Exception("Reader read too much or too little");
				}

				long packetStreamSize = ((packetHeader.PacketSize + 3) & ~3); //Packets are aligned on a 4 byte boundary, pad if needed.
				long padding = packetStreamSize - packetHeader.PacketSize;
				if (padding > 0)
				{
					reader.m_targetStream.Seek(padding, SeekOrigin.Current);
				}
            }
        }

        private void DecodeCommandStream(StreamSpan a_streamSection, Action<ICommandPacketBase> a_onPacketDecoded)
        {
			while (a_streamSection.GetBytesRemaining(m_targetStream) > CommandHeader.ByteSize)
			{
				CommandHeader header = ReadCommandHeader();

				CommandMeta? commandMeta = CommandPacketFactory.FindCommandMeta(header.CommandIdentifier);
				if (commandMeta == null)
				{
					BlackmagicCameraLogInterface.LogWarning(
						$"Received unknown packet with identifier {header.CommandIdentifier}. Size: {BytesRemaining}, Type: {header.DataType}");
					m_targetStream.Seek(a_streamSection.ByteLength - CommandHeader.ByteSize, SeekOrigin.Current);
					break;
				}

				if (header.DataType != commandMeta.DataType ||
				    ((a_streamSection.GetBytesRemaining(m_targetStream) - commandMeta.SerializedSizeBytes) != 0 &&
				     header.DataType != ECommandDataType.Utf8String))
				{
					throw new Exception(
						$"Command meta data wrong: Bytes (Expected / Got) {commandMeta.SerializedSizeBytes} / {a_streamSection.GetBytesRemaining(m_targetStream)}, DataType: {commandMeta.DataType} / {header.DataType}");
				}

				ICommandPacketBase? packetInstance = CommandPacketFactory.CreatePacket(header.CommandIdentifier, this);
				if (packetInstance != null)
				{
					BlackmagicCameraLogInterface.LogVerbose(
						$"Received Packet {header.CommandIdentifier}. {packetInstance}");
					a_onPacketDecoded.Invoke(packetInstance);
				}
				else
				{
					throw new Exception("Failed to deserialize command with known meta");
				}

			}
		}

        public CommandIdentifier ReadIdentifier()
		{
			int readCount = m_targetStream.Read(m_smallReadBuffer, 0, 2);
			if (readCount != 2)
			{
				throw new Exception();
			}

			return new CommandIdentifier(m_smallReadBuffer[0], m_smallReadBuffer[1]);
		}

		public CommandHeader ReadCommandHeader()
		{
			CommandIdentifier identifier = ReadIdentifier();
			ECommandDataType dataType = (ECommandDataType)m_targetStream.ReadByte();
			ECommandOperation operation = (ECommandOperation) m_targetStream.ReadByte();
			return new CommandHeader() {CommandIdentifier = identifier, DataType = dataType, Operation = operation};
		}

		public PacketHeader ReadPacketHeader()
		{
			byte targetCamera = (byte)m_targetStream.ReadByte();
			byte packetSize = (byte) m_targetStream.ReadByte();
			EPacketCommand command = (EPacketCommand) m_targetStream.ReadByte();
			byte reserved = (byte) m_targetStream.ReadByte();
			return new PacketHeader()
				{TargetCamera = targetCamera, PacketSize = packetSize, Command = command, Reserved = reserved};
		}

		public byte[] ReadBytes(int a_commandSize)
		{
			byte[] result = new byte[a_commandSize];
			int bytesRead = m_targetStream.Read(result, 0, a_commandSize);
			if (bytesRead != a_commandSize)
			{
				throw new Exception($"Failed to read required amount of bytes from stream, expected {a_commandSize} read {bytesRead}");
			}

			return result;
		}

		public byte ReadInt8()
		{
			int value = m_targetStream.ReadByte();
			if (value == -1)
			{
				throw new Exception("Failed to read 1 byte for int8");
			}

			return (byte)value;
		}

		public short ReadInt16()
		{
			int readCount = m_targetStream.Read(m_smallReadBuffer, 0, 2);
			if (readCount != 2)
			{
				throw new Exception("Failed to read 2 bytes for int16");
			}

			return BitConverter.ToInt16(m_smallReadBuffer, 0);
		}

		public int ReadInt32()
		{
			int readCount = m_targetStream.Read(m_smallReadBuffer, 0, 4);
			if (readCount != 4)
			{
				throw new Exception("Failed to read 4 bytes for int32");
			}

			return BitConverter.ToInt32(m_smallReadBuffer, 0);
		}

		public uint ReadUInt32()
		{
			int readCount = m_targetStream.Read(m_smallReadBuffer, 0, 4);
			if (readCount != 4)
			{
				throw new Exception("Failed to read 4 bytes for int32");
			}

			return BitConverter.ToUInt32(m_smallReadBuffer, 0);
		}

		public string ReadString()
		{
			byte[] stringData = new byte[BytesRemaining];
			int bytesRead = m_targetStream.Read(stringData, 0, stringData.Length);
			if (bytesRead != stringData.Length)
			{
				throw new Exception("Failed to read string");
			}

			return System.Text.Encoding.UTF8.GetString(stringData);
		}

	}
}
