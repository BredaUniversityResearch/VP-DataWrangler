using System;
using System.Threading;
using BlackmagicDeckLinkControl;
using Xunit;

namespace BlackmagicCameraControlTest
{
	public class DeckLinkSetupTest
	{
		[Fact]
		public void ConnectToCamera()
		{
			BlackmagicDeckLinkController iface = new BlackmagicDeckLinkController();
		}
    }
}