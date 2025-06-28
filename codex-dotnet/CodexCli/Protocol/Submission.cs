namespace CodexCli.Protocol;

using System.Collections.Generic;
using CodexCli.Config;
using System.Text.Json.Serialization;

/// <summary>
/// C# port of codex-rs/core/src/protocol.rs Submission and Op (partial). (done)
/// </summary>
public record Submission(string Id, SubmissionOp Op);

[JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[JsonDerivedType(typeof(ConfigureSessionOp), "configure_session")]
[JsonDerivedType(typeof(UserInputOp), "user_input")]
[JsonDerivedType(typeof(InterruptOp), "interrupt")]
public abstract record SubmissionOp;

public record ConfigureSessionOp(
    ModelProviderInfo Provider,
    string Model,
    string? Instructions,
    IReadOnlyList<string>? Notify,
    string Cwd) : SubmissionOp;

public record UserInputOp(IReadOnlyList<InputItem> Items) : SubmissionOp;

public record InterruptOp() : SubmissionOp;
