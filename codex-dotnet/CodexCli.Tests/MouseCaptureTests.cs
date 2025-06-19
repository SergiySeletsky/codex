using CodexCli.Interactive;
using System;
using System.IO;
using Xunit;

public class MouseCaptureTests
{
    [Fact]
    public void ToggleWritesEscapeSequences()
    {
        var orig = Console.Out;
        var sw = new StringWriter();
        Console.SetOut(sw);
        try
        {
            var mc = new MouseCapture(false);
            Assert.Contains("\u001b[?1000l", sw.ToString());
            sw.GetStringBuilder().Clear();
            mc.Toggle();
            Assert.Contains("\u001b[?1000h", sw.ToString());
        }
        finally
        {
            Console.SetOut(orig);
        }
    }
}
