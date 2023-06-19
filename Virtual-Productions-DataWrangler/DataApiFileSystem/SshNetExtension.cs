using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Renci.SshNet;
using Renci.SshNet.Sftp;

namespace DataApiTests
{
	internal static class SshNetExtension
	{
		private static readonly Encoding Utf8NoBOM = new UTF8Encoding(false, true);

		public static void TruncateWriteAllText(this SftpClient a_this, string a_path, string a_contents)
		{
			TruncateWriteAllText(a_this, a_path, a_contents, Utf8NoBOM);
		}

		public static void TruncateWriteAllText(this SftpClient a_this, string a_path, string a_contents, Encoding a_encoding)
		{
			using (SftpFileStream fs = a_this.OpenWrite(a_path))
			{
				fs.SetLength(a_contents.Length);
				byte[] bytes = a_encoding.GetBytes(a_contents);
				fs.Write(bytes);
			}
		}
	}
}
