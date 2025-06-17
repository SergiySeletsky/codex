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

    [Fact]
    public void CalculatesHeightWithManyMatches()
    {
        var popup = new CommandPopup();
        popup.OnComposerTextChange("/");
        int h = popup.CalculateRequiredHeight(10);
        Assert.Equal(5, h); // 3 commands => 3 rows + border
    }
}
