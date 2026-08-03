using System.Globalization;
using Z21Client.Resources.Localization;

namespace Z21ClientTest.Resources.Localization;

public class MessagesTests
{
    [Fact]
    public void Text0020_ReturnsLocalizedStringForEachLanguage()
    {
        // Arrange
        var originalCulture = Messages.Culture;

        try
        {
            // English (default)
            Messages.Culture = new CultureInfo("en");
            Assert.Equal("Disconnected.", Messages.Text0020);

            // Danish
            Messages.Culture = new CultureInfo("da");
            Assert.Equal("Frakoblet.", Messages.Text0020);

            // German
            Messages.Culture = new CultureInfo("de");
            Assert.Equal("Verbindung getrennt.", Messages.Text0020);
        }
        finally
        {
            Messages.Culture = originalCulture;
        }
    }
}
