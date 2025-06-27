using CodexCli.Util;
using System.Text.Json;
using Xunit;

public class CodexToolCallParamTests
{
    [Fact]
    public void SerializesToKebabCase()
    {
        var param = new CodexToolCallParam("hi", Model: "gpt", Profile: "p1", Cwd: "/tmp", ApprovalPolicy: "on-failure", SandboxPermissions: new[]{"disk-write-cwd"}, Config: new(), Provider: "mock");
        var opts = new JsonSerializerOptions { Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping };
        var json = JsonSerializer.Serialize(param, opts);
        Assert.Equal("{\"prompt\":\"hi\",\"model\":\"gpt\",\"profile\":\"p1\",\"cwd\":\"/tmp\",\"approval-policy\":\"on-failure\",\"sandbox-permissions\":[\"disk-write-cwd\"],\"config\":{},\"provider\":\"mock\"}", json);
    }
}

