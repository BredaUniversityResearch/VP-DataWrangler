using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlackmagicCameraControl
{
	public struct CameraHandle
	{
		public static readonly CameraHandle Invalid = new CameraHandle(-1);

		public readonly int ConnectionId;

		public CameraHandle(int a_connectionId)
		{
			ConnectionId = a_connectionId;
		}
	}
}
