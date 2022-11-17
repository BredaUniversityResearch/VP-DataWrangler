using BlackmagicDeckLinkControl;
using Xunit;

namespace BlackmagicDeckLinkControlTest
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