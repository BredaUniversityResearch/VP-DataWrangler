using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlackmagicCameraControl
{
	public readonly struct CameraHandle
	{
		public static readonly CameraHandle Invalid = new CameraHandle(-1);

		public readonly int ConnectionId;

		public CameraHandle(int a_connectionId)
		{
			ConnectionId = a_connectionId;
		}

		public static bool operator ==(CameraHandle a_lhs, CameraHandle a_rhs)
		{
			return a_lhs.ConnectionId == a_rhs.ConnectionId;
		}

		public static bool operator !=(CameraHandle a_lhs, CameraHandle a_rhs)
		{
			return !(a_lhs == a_rhs);
		}

		public bool Equals(CameraHandle a_other)
		{
			return ConnectionId == a_other.ConnectionId;
		}

		public override bool Equals(object? a_obj)
		{
			return a_obj is CameraHandle other && Equals(other);
		}

		public override int GetHashCode()
		{
			return ConnectionId;
		}
	}
}
