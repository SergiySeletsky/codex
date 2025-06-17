using CodexCli.Interactive;
using System.Linq;
using Xunit;

public class CommandPopupTests
{
    [Fact]
    public void FiltersAndSelectsCommands()
    {
        var popup = new CommandPopup();
        popup.OnComposerTextChange("/n");
        Assert.Contains(SlashCommand.New, popup.GetFilteredCommands());
        popup.MoveDown();
        Assert.Equal(SlashCommand.New, popup.SelectedCommand());
        popup.MoveUp();
        Assert.Equal(SlashCommand.New, popup.SelectedCommand());
    }
}
