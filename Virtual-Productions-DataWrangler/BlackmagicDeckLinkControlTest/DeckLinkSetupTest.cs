using BlackmagicDeckLinkControl;
using Xunit;

namespace BlackmagicDeckLinkControlTest
{
	public class DeckLinkSetupTest
	{
		[Fact]
		public void ConnectToCamera()
		{
			BlackmagicDeckLinkController? iface = BlackmagicDeckLinkController.Create(out string? message);
			Assert.True(message == null);
			Assert.True(iface != null);
		}
    }
}