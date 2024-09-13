namespace BlackmagicCameraControlData.CommandPackets;

public class CommandWriter
{
	private Stream m_targetStream;

	public CommandWriter(Stream a_targetStream)
	{
		m_targetStream = a_targetStream;
	}

	public void Write(sbyte a_valueToWrite)
	{
		m_targetStream.WriteByte((byte)a_valueToWrite);
	}

	public void Write(byte a_valueToWrite)
	{
		m_targetStream.WriteByte(a_valueToWrite);
		//m_targetStream.Seek(1, SeekOrigin.Current);
	}

	public void Write(short a_valueToWrite)
	{
		m_targetStream.Write(BitConverter.GetBytes(a_valueToWrite), 0, sizeof(short));
		//m_targetStream.Seek(sizeof(short), SeekOrigin.Current);
	}

	public void Write(int a_valueToWrite)
	{
		m_targetStream.Write(BitConverter.GetBytes(a_valueToWrite), 0, sizeof(int));
		//m_targetStream.Seek(sizeof(int), SeekOrigin.Current);
	}

	public void Write(uint a_valueToWrite)
	{
		m_targetStream.Write(BitConverter.GetBytes(a_valueToWrite), 0, sizeof(uint));
	}

	public void Write(string a_valueToWrite)
	{
		byte[] stringAsBytes = System.Text.Encoding.UTF8.GetBytes(a_valueToWrite);
		m_targetStream.Write(stringAsBytes, 0, stringAsBytes.Length);
	}

    public void WriteCommand(ICommandPacketBase a_packet)
    {
        CommandMeta? meta = CommandPacketFactory.FindCommandMeta(a_packet.GetType());
        if (meta == null)
            throw new Exception($"Unknown command packet type {a_packet}");
        WritePacketHeader(new PacketHeader()
        {
            Command = EPacketCommand.ChangeConfig,
            PacketSize = (byte) (PacketHeader.ByteSize + meta.SerializedSizeBytes),
            TargetCamera = 1
        });
        WriteCommandHeader(new CommandHeader() {
            CommandIdentifier = meta.Identifier, 
            DataType = meta.DataType, 
            Operation = ECommandOperation.Assign}
        );
        a_packet.WriteTo(this);

        long requiredPadding = m_targetStream.Length % 4;
        for (int i = 0; i < requiredPadding; ++i)
        {
            m_targetStream.WriteByte(0);
        }
    }

    public void WriteCommandHeader(CommandHeader a_commandHeader)
    {
        WriteIdentifier(a_commandHeader.CommandIdentifier);
        Write((byte) a_commandHeader.DataType);
        Write((byte) a_commandHeader.Operation);
    }

    private void WriteIdentifier(CommandIdentifier a_commandHeaderCommandIdentifier)
    {
        Write(a_commandHeaderCommandIdentifier.Category);
        Write(a_commandHeaderCommandIdentifier.Parameter);
    }

    public void WritePacketHeader(PacketHeader a_packetHeader)
    {
        Write(a_packetHeader.TargetCamera);
        Write(a_packetHeader.PacketSize);
        Write((byte)a_packetHeader.Command);
        Write(a_packetHeader.Reserved);
    }

	public void Write(CommandIdentifier a_commandIdentifier)
	{
		Write(a_commandIdentifier.Category);
		Write(a_commandIdentifier.Parameter);
	}
}