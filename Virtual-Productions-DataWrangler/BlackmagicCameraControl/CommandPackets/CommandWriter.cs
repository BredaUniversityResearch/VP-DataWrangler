using System;
using System.IO;

namespace BlackmagicCameraControl.CommandPackets;

public class CommandWriter
{
	private Stream m_targetStream;

	public CommandWriter(Stream a_targetStream)
	{
		m_targetStream = a_targetStream;
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

	public void Write(CommandIdentifier a_commandIdentifier)
	{
		Write(a_commandIdentifier.Category);
		Write(a_commandIdentifier.Parameter);
	}
}