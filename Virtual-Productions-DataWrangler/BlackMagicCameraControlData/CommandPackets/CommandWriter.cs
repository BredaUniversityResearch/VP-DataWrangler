using System;
using System.IO;
using BlackmagicCameraControlData.CommandPackets;

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

	public void Write(uint a_valueToWrite)
	{
		m_targetStream.Write(BitConverter.GetBytes(a_valueToWrite), 0, sizeof(uint));
	}

	public void Write(string a_valueToWrite)
	{
		byte[] stringAsBytes = System.Text.Encoding.UTF8.GetBytes(a_valueToWrite);
		m_targetStream.Write(stringAsBytes, 0, stringAsBytes.Length);
	}

	public void Write(CommandIdentifier a_commandIdentifier)
	{
		Write(a_commandIdentifier.Category);
		Write(a_commandIdentifier.Parameter);
	}
}