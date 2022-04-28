using System;
using System.IO;
using Windows.Storage.Streams;

namespace BlackmagicCameraControl.CommandPackets
{
	public class CommandReader
	{
		private Stream m_targetStream;
		private byte[] m_smallReadBuffer = new byte[32];

		public long BytesRemaining => (m_targetStream.Length - m_targetStream.Position);
		public long BytesProcessed => m_targetStream.Position;

		public CommandReader(Stream a_stream)
		{
			m_targetStream = a_stream;
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

		public short ReadInt16()
		{
			int readCount = m_targetStream.Read(m_smallReadBuffer, 0, 2);
			if (readCount != 2)
			{
				throw new Exception("Failed to read 2 bytes for int16");
			}

			return BitConverter.ToInt16(m_smallReadBuffer, 0);
		}
	}
}
