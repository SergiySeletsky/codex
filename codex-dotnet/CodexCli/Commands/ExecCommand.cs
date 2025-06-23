using System.CommandLine;
using CodexCli.Config;
using CodexCli.Util;
// Partial port of codex-rs/exec/src/lib.rs Exec command
// CodexWrapper and safety checks integrated
using CodexCli.Protocol;
using System;
using CodexCli.ApplyPatch;
using CodexCli.Models;
using System.Linq;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Runtime.CompilerServices;

namespace CodexCli.Commands;

public static class ExecCommand
{
    public static Command Create(Option<string?> configOption, Option<string?> cdOption)
    {
        var promptArg = new Argument<string?>("prompt", description: "Prompt text");
        var imagesOpt = new Option<FileInfo[]>("--image", "Image attachments") { AllowMultipleArgumentsPerToken = true };
        var modelOpt = new Option<string?>("--model", "Model to use");
        var profileOpt = new Option<string?>("--profile", "Config profile");
        var providerOpt = new Option<string?>("--model-provider", "Model provider");
        var fullAutoOpt = new Option<bool>("--full-auto", () => false, "Run in full-auto mode");
        var approvalOpt = new Option<ApprovalMode?>("--ask-for-approval", "When to require approval");
        var sandboxOpt = new Option<string[]>("-s", description: "Sandbox permissions") { AllowMultipleArgumentsPerToken = true };
        var colorOpt = new Option<ColorMode>("--color", () => ColorMode.Auto, "Output color mode");
        var cwdOpt = new Option<string?>(new[] {"--cwd", "-C"}, "Working directory for Codex");
        var lastMsgOpt = new Option<string?>("--output-last-message", "File to write last agent message");
        var sessionOpt = new Option<string?>("--session", "Existing session id");
        var skipGitOpt = new Option<bool>("--skip-git-repo-check", () => false, "Allow running outside git repo");
        var notifyOpt = new Option<string[]>("--notify", description: "Notification command") { AllowMultipleArgumentsPerToken = true };
        var overridesOpt = new Option<string[]>("-c", description: "Config overrides") { AllowMultipleArgumentsPerToken = true };
        var effortOpt = new Option<ReasoningEffort?>("--reasoning-effort");
        var summaryOpt = new Option<ReasoningSummary?>("--reasoning-summary");
        var instrOpt = new Option<string?>("--instructions", "Path to instructions file");
        var hideReasonOpt = new Option<bool?>("--hide-agent-reasoning", "Hide reasoning events");
        var disableStorageOpt = new Option<bool?>("--disable-response-storage", "Disable response storage");
        var noProjDocOpt = new Option<bool>("--no-project-doc", () => false, "Disable AGENTS.md project doc");
        var jsonOpt = new Option<bool>("--json", () => false, "Output raw JSON events");
        var eventLogOpt = new Option<string?>("--event-log", "Path to save JSON event log");
        var envInheritOpt = new Option<ShellEnvironmentPolicyInherit?>("--env-inherit");
        var envIgnoreOpt = new Option<bool?>("--env-ignore-default-excludes");
        var envExcludeOpt = new Option<string[]>("--env-exclude") { AllowMultipleArgumentsPerToken = true };
        var envSetOpt = new Option<string[]>("--env-set") { AllowMultipleArgumentsPerToken = true };
        var envIncludeOpt = new Option<string[]>("--env-include-only") { AllowMultipleArgumentsPerToken = true };
        var docMaxOpt = new Option<int?>("--project-doc-max-bytes", "Limit size of AGENTS.md to read");
        var docPathOpt = new Option<string?>("--project-doc-path", "Explicit project doc path");
        var mcpServerOpt = new Option<string?>("--mcp-server", "Run via MCP server from config");
        var eventsUrlOpt = new Option<string?>("--events-url", description: "Stream events from MCP server");
        var watchEventsOpt = new Option<bool>("--watch-events", description: "Keep watching events after completion");

        var cmd = new Command("exec", "Run Codex non-interactively");
        cmd.AddArgument(promptArg);
        cmd.AddOption(imagesOpt);
        cmd.AddOption(modelOpt);
        cmd.AddOption(profileOpt);
        cmd.AddOption(providerOpt);
        cmd.AddOption(fullAutoOpt);
        cmd.AddOption(cwdOpt);
        cmd.AddOption(approvalOpt);
        cmd.AddOption(sandboxOpt);
        cmd.AddOption(colorOpt);
        cmd.AddOption(lastMsgOpt);
        cmd.AddOption(sessionOpt);
        cmd.AddOption(skipGitOpt);
        cmd.AddOption(notifyOpt);
        cmd.AddOption(overridesOpt);
        cmd.AddOption(effortOpt);
        cmd.AddOption(summaryOpt);
        cmd.AddOption(instrOpt);
        cmd.AddOption(hideReasonOpt);
        cmd.AddOption(disableStorageOpt);
        cmd.AddOption(noProjDocOpt);
        cmd.AddOption(jsonOpt);
        cmd.AddOption(eventLogOpt);
        cmd.AddOption(envInheritOpt);
        cmd.AddOption(envIgnoreOpt);
        cmd.AddOption(envExcludeOpt);
        cmd.AddOption(envSetOpt);
        cmd.AddOption(envIncludeOpt);
        cmd.AddOption(docMaxOpt);
        cmd.AddOption(docPathOpt);
        cmd.AddOption(mcpServerOpt);
        cmd.AddOption(eventsUrlOpt);
        cmd.AddOption(watchEventsOpt);

        var binder = new ExecBinder(promptArg, imagesOpt, modelOpt, profileOpt, providerOpt, fullAutoOpt,
            approvalOpt, sandboxOpt, colorOpt, cwdOpt, lastMsgOpt, sessionOpt, skipGitOpt, notifyOpt, overridesOpt,
            effortOpt, summaryOpt, instrOpt, hideReasonOpt, disableStorageOpt, noProjDocOpt, jsonOpt, eventLogOpt,
            envInheritOpt, envIgnoreOpt, envExcludeOpt, envSetOpt, envIncludeOpt, docMaxOpt, docPathOpt, mcpServerOpt, eventsUrlOpt, watchEventsOpt);

        cmd.SetHandler(async (ExecOptions opts, string? cfgPath, string? cd) =>
        {
            if (cd != null) Environment.CurrentDirectory = cd;
            AppConfig? cfg = null;
            if (!string.IsNullOrEmpty(cfgPath) && File.Exists(cfgPath))
                cfg = AppConfig.Load(cfgPath, opts.Profile);

            SessionManager.SetPersistence(cfg?.History.Persistence ?? HistoryPersistence.SaveAll);
            var sessionId = opts.SessionId ?? SessionManager.CreateSession();
            var history = new ConversationHistory();
            var approvedCommands = new HashSet<List<string>>(new SequenceEqualityComparer<string>());
            var approvalPolicy = opts.Approval ?? cfg?.ApprovalPolicy ?? ApprovalMode.OnFailure;
            RolloutRecorder? recorder = null;
            StreamWriter? logWriter = null;
            if (opts.EventLogFile != null)
            {
                // Resolve relative log paths using Codex.ResolvePath for parity with Rust
                var logPath = Codex.ResolvePath(Environment.CurrentDirectory, opts.EventLogFile);
                logWriter = new StreamWriter(logPath, append: false);
            }
            if (cfg != null)
                recorder = await RolloutRecorder.CreateAsync(cfg, sessionId, null);

            var policy = cfg?.ShellEnvironmentPolicy ?? new ShellEnvironmentPolicy();
            if (opts.EnvInherit != null) policy.Inherit = opts.EnvInherit.Value;
            if (opts.EnvIgnoreDefaultExcludes != null) policy.IgnoreDefaultExcludes = opts.EnvIgnoreDefaultExcludes.Value;
            if (opts.EnvExclude.Length > 0) policy.Exclude = opts.EnvExclude.Select(EnvironmentVariablePattern.CaseInsensitive).ToList();
            if (opts.EnvSet.Length > 0)
                policy.Set = opts.EnvSet.Select(s => s.Split('=', 2)).ToDictionary(p => p[0], p => p.Length > 1 ? p[1] : string.Empty);
            if (opts.EnvIncludeOnly.Length > 0) policy.IncludeOnly = opts.EnvIncludeOnly.Select(EnvironmentVariablePattern.CaseInsensitive).ToList();
            var envMap = ExecEnv.Create(policy);

            if (opts.NotifyCommand.Length > 0)
                NotifyUtils.RunNotify(opts.NotifyCommand, "session_started", envMap);

            if (!opts.SkipGitRepoCheck && !GitUtils.IsInsideGitRepo(Environment.CurrentDirectory))
            {
                Console.Error.WriteLine("Not inside a git repo. Use --skip-git-repo-check to override.");
                return;
            }

            if (opts.Cwd != null) Environment.CurrentDirectory = opts.Cwd;

            var prompt = opts.Prompt;
            if (string.IsNullOrEmpty(prompt) || prompt == "-")
            {
                if (!Console.IsInputRedirected)
                {
                    // Use Codex.ResolvePath (port of resolve_path helper) so relative paths
                    // match Rust CLI behavior.
                    var instPath = opts.InstructionsPath != null
                        ? Codex.ResolvePath(Environment.CurrentDirectory, opts.InstructionsPath)
                        : null;
                    var projDocPath = opts.ProjectDocPath != null
                        ? Codex.ResolvePath(Environment.CurrentDirectory, opts.ProjectDocPath)
                        : null;
                    var inst = instPath != null && File.Exists(instPath)
                        ? File.ReadAllText(instPath)
                        : cfg != null ? ProjectDoc.GetUserInstructions(cfg, Environment.CurrentDirectory, opts.NoProjectDoc, opts.ProjectDocMaxBytes, projDocPath) : null;
                    if (!string.IsNullOrWhiteSpace(inst))
                    {
                        prompt = inst;
                    }
                    else
                    {
                        Console.WriteLine("Reading prompt from stdin...");
                        prompt = await Console.In.ReadToEndAsync();
                    }
                }
                else
                {
                    prompt = await Console.In.ReadToEndAsync();
                }
            }
            SessionManager.AddEntry(sessionId, prompt ?? string.Empty);

            var ov = ConfigOverrides.Parse(opts.Overrides);
            if (ov.Overrides.Count > 0)
            {
                if (cfg == null) cfg = new AppConfig();
                ov.Apply(cfg);
                Console.Error.WriteLine($"{ov.Overrides.Count} override(s) parsed and applied");
            }

            var providerId = EnvUtils.GetModelProviderId(opts.ModelProvider) ?? cfg?.ModelProvider ?? "openai";
            var providerInfo = cfg?.GetProvider(providerId) ?? ModelProviderInfo.BuiltIns[providerId];
            var baseUrl = EnvUtils.GetProviderBaseUrl(null) ?? providerInfo.BaseUrl;
            var apiKey = ApiKeyManager.GetKey(providerInfo);
            var client = new OpenAIClient(apiKey, baseUrl);
            var execPolicy = ExecPolicy.LoadDefault();
            bool hideReason = opts.HideAgentReasoning ?? cfg?.HideAgentReasoning ?? false;
            bool disableStorage = opts.DisableResponseStorage ?? cfg?.DisableResponseStorage ?? false;
            bool withAnsi = opts.Color switch
            {
                ColorMode.Always => true,
                ColorMode.Never => false,
                _ => !Console.IsOutputRedirected
            };

            var sandboxList = opts.Sandbox.ToList();
            if (opts.FullAuto)
            {
                sandboxList.Clear();
                sandboxList.Add(new SandboxPermission(SandboxPermissionType.DiskWriteCwd));
                sandboxList.Add(new SandboxPermission(SandboxPermissionType.DiskWritePlatformUserTempFolder));
                sandboxList.Add(new SandboxPermission(SandboxPermissionType.DiskWritePlatformGlobalTempFolder));
            }

            var sandboxLabel = opts.FullAuto
                ? "full-auto"
                : (sandboxList.Count > 0 ? string.Join(',', sandboxList.Select(s => s.ToString())) : "default");
            var processor = new CodexCli.Protocol.EventProcessor(withAnsi, !hideReason, cfg?.FileOpener ?? UriBasedFileOpener.None, Environment.CurrentDirectory);
            var sandboxPolicy = new SandboxPolicy { Permissions = sandboxList };
            processor.PrintConfigSummary(
                opts.Model ?? cfg?.Model ?? "default",
                opts.ModelProvider ?? cfg?.ModelProvider ?? string.Empty,
                Environment.CurrentDirectory,
                sandboxLabel,
                prompt.Trim(),
                disableStorage,
                opts.ReasoningEffort ?? cfg?.ModelReasoningEffort,
                opts.ReasoningSummary ?? cfg?.ModelReasoningSummary,
                EnvUtils.GetLogLevel(null));

            var imagePaths = opts.Images.Select(i => i.FullName).ToArray();
            IAsyncEnumerable<Event> events;
            if (!string.IsNullOrEmpty(opts.McpServer))
            {
                var (mgr, _) = await McpConnectionManager.CreateAsync(cfg ?? new AppConfig());
                if (!mgr.HasServer(opts.McpServer))
                {
                    Console.Error.WriteLine($"Unknown MCP server '{opts.McpServer}'");
                    return;
                }
                var param = new CodexToolCallParam(prompt ?? string.Empty, opts.Model ?? cfg?.Model, opts.Profile, Environment.CurrentDirectory, null, sandboxList.Select(s => s.ToString()).ToList(), null, providerId);
                var paramJson = System.Text.Json.JsonSerializer.SerializeToElement(param);
                // CallToolAsync port from codex-rs core::codex call_tool
                var callTask = Codex.CallToolAsync(mgr, opts.McpServer!, "codex", paramJson);
                if (!string.IsNullOrEmpty(opts.EventsUrl))
                {
                    async IAsyncEnumerable<Event> Stream()
                    {
                        await foreach (var ev in McpEventStream.ReadEventsAsync(opts.EventsUrl))
                        {
                            yield return ev;
                            if (ev is TaskCompleteEvent && !opts.WatchEvents)
                                break;
                        }
                        await callTask;
                    }
                    events = Stream();
                }
                else
                {
                    var result = await callTask;
                    async IAsyncEnumerable<Event> Enumerate()
                    {
                        var msg = result.Content.FirstOrDefault().ToString();
                        yield return new AgentMessageEvent(Guid.NewGuid().ToString(), msg);
                        yield return new TaskCompleteEvent(Guid.NewGuid().ToString(), msg);
                    }
                    events = Enumerate();
                }
            }
            else
            {
                Func<string, OpenAIClient, string, CancellationToken, IAsyncEnumerable<Event>>? agent = null;
                if (providerId == "mock")
                    agent = (p, c, m, t) => MockCodexAgent.RunAsync(p, imagePaths, null, t);
                var sigint = SignalUtils.NotifyOnSigInt();
                var (stream, first, codexCts) = await CodexWrapper.InitCodexAsync(prompt, client, opts.Model ?? cfg?.Model ?? "default", agent, opts.NotifyCommand);
                sigint.Token.Register(() => codexCts.Cancel());
                async IAsyncEnumerable<Event> EnumerateInit()
                {
                    yield return first;
                    await foreach (var e in stream.WithCancellation(codexCts.Token))
                        yield return e;
                }
                events = EnumerateInit();
            }
            await foreach (var ev in events)
            {
                if (opts.Json)
                    Console.WriteLine(System.Text.Json.JsonSerializer.Serialize(ev));
                else
                    processor.ProcessEvent(ev);
                if (logWriter != null)
                    await logWriter.WriteLineAsync(System.Text.Json.JsonSerializer.Serialize(ev));
                if (ResponseItemFactory.FromEvent(ev) is { } ri)
                {
                    history.RecordItems(new[] { ri });
                    if (recorder != null) await recorder.RecordItemsAsync(new[] { ri });
                }
                switch (ev)
                {
                    case AgentMessageEvent am:
                        SessionManager.AddEntry(sessionId, am.Message);
                        if (cfg != null)
                            await MessageHistory.AppendEntryAsync(am.Message, sessionId, cfg);
                        break;
                    case AddToHistoryEvent ah:
                        SessionManager.AddEntry(sessionId, ah.Text);
                        if (cfg != null)
                            await MessageHistory.AppendEntryAsync(ah.Text, sessionId, cfg);
                        break;
                    case ExecApprovalRequestEvent ar:
                        var prog = ar.Command.First();
                        var args = ar.Command.Skip(1).ToArray();
                        if (execPolicy.IsForbidden(prog))
                        {
                            Console.WriteLine($"Denied '{string.Join(" ", ar.Command)}' ({execPolicy.GetReason(prog)})");
                            var deniedEv = Codex.NotifyBackgroundEvent(Guid.NewGuid().ToString(), "command denied");
                            if (opts.Json)
                                Console.WriteLine(System.Text.Json.JsonSerializer.Serialize(deniedEv));
                            else
                                processor.ProcessEvent(deniedEv);
                            break;
                        }
                        if (!execPolicy.VerifyCommand(prog, args))
                        {
                            Console.WriteLine($"Denied '{string.Join(" ", ar.Command)}' (unverified)");
                            var deniedEv = Codex.NotifyBackgroundEvent(Guid.NewGuid().ToString(), "command denied");
                            if (opts.Json)
                                Console.WriteLine(System.Text.Json.JsonSerializer.Serialize(deniedEv));
                            else
                                processor.ProcessEvent(deniedEv);
                            break;
                        }
                        var safety = Safety.AssessCommandSafety(ar.Command.ToList(), approvalPolicy, sandboxPolicy, approvedCommands);
                        if (safety == SafetyCheck.Reject)
                        {
                            Console.WriteLine("Denied");
                            var deniedEv = Codex.NotifyBackgroundEvent(Guid.NewGuid().ToString(), "command denied");
                            if (opts.Json)
                                Console.WriteLine(System.Text.Json.JsonSerializer.Serialize(deniedEv));
                            else
                                processor.ProcessEvent(deniedEv);
                            break;
                        }
                        if (safety == SafetyCheck.AskUser)
                        {
                            Console.Write($"Run '{string.Join(" ", ar.Command)}'? [y/a/N] ");
                            var resp = Console.ReadLine();
                            if (resp?.StartsWith("a", StringComparison.OrdinalIgnoreCase) == true)
                                approvedCommands.Add(ar.Command.ToList());
                            if (!(resp?.StartsWith("y", StringComparison.OrdinalIgnoreCase) == true ||
                                  resp?.StartsWith("a", StringComparison.OrdinalIgnoreCase) == true))
                                Console.WriteLine("Denied");
                        }
                        break;
                    case ExecCommandBeginEvent begin:
                        var argv = begin.Command.ToArray();
                        if (ApplyPatchCommandParser.MaybeParseApplyPatch(argv, out var patch) == MaybeApplyPatch.Body &&
                            patch != null &&
                            ApplyPatchCommandParser.MaybeParseApplyPatchVerified(argv, begin.Cwd, out var action) == MaybeApplyPatchVerified.Body &&
                            action != null)
                        {
                            var roots = sandboxPolicy.GetWritableRootsWithCwd(begin.Cwd);
                            var patchSafety = Safety.AssessPatchSafety(action, approvalPolicy, roots, begin.Cwd);
                            if (patchSafety == SafetyCheck.Reject)
                            {
                                Console.WriteLine("Patch denied");
                                var deniedEv = Codex.NotifyBackgroundEvent(Guid.NewGuid().ToString(), "patch denied");
                                if (opts.Json)
                                    Console.WriteLine(System.Text.Json.JsonSerializer.Serialize(deniedEv));
                                else
                                    processor.ProcessEvent(deniedEv);
                                break;
                            }
                            bool autoApproved = patchSafety == SafetyCheck.AutoApprove;
                            if (patchSafety == SafetyCheck.AskUser)
                            {
                                Console.Write("Apply patch? [y/N] ");
                                var respPatch = Console.ReadLine();
                                if (!respPatch?.StartsWith("y", StringComparison.OrdinalIgnoreCase) ?? true)
                                {
                                    Console.WriteLine("Patch denied");
                                    var deniedEv = Codex.NotifyBackgroundEvent(Guid.NewGuid().ToString(), "patch denied");
                                    if (opts.Json)
                                        Console.WriteLine(System.Text.Json.JsonSerializer.Serialize(deniedEv));
                                    else
                                        processor.ProcessEvent(deniedEv);
                                    break;
                                }
                            }
                            var changes = new Dictionary<string, FileChange>();
                            foreach (var kv in action.Changes)
                            {
                                var fc = kv.Value.Kind switch
                                {
                                    "add" => (FileChange)new AddFileChange(kv.Value.Content ?? string.Empty),
                                    "delete" => new DeleteFileChange(),
                                    "update" => new UpdateFileChange(kv.Value.UnifiedDiff!, kv.Value.MovePath),
                                    _ => throw new InvalidOperationException()
                                };
                                changes[kv.Key] = fc;
                            }
                            var pbEvent = new PatchApplyBeginEvent(Guid.NewGuid().ToString(), autoApproved, changes);
                            if (opts.Json)
                                Console.WriteLine(System.Text.Json.JsonSerializer.Serialize(pbEvent));
                            else
                                processor.ProcessEvent(pbEvent);
                            if (logWriter != null)
                                await logWriter.WriteLineAsync(System.Text.Json.JsonSerializer.Serialize(pbEvent));
                            try
                            {
                                var result = PatchApplier.ApplyWithSummary(patch, begin.Cwd);
                                var peEvent = new PatchApplyEndEvent(Guid.NewGuid().ToString(), result.Summary, string.Empty, true);
                                if (opts.Json)
                                    Console.WriteLine(System.Text.Json.JsonSerializer.Serialize(peEvent));
                                else
                                    processor.ProcessEvent(peEvent);
                                if (logWriter != null)
                                    await logWriter.WriteLineAsync(System.Text.Json.JsonSerializer.Serialize(peEvent));
                            }
                            catch (PatchParseException e)
                            {
                                var peEvent = new PatchApplyEndEvent(Guid.NewGuid().ToString(), string.Empty, e.Message, false);
                                if (opts.Json)
                                    Console.WriteLine(System.Text.Json.JsonSerializer.Serialize(peEvent));
                                else
                                    processor.ProcessEvent(peEvent);
                                if (logWriter != null)
                                    await logWriter.WriteLineAsync(System.Text.Json.JsonSerializer.Serialize(peEvent));
                            }
                        }
                        else
                        {
                            var execParams = new ExecParams(begin.Command.ToList(), begin.Cwd, null, envMap, null, null, sessionId);
                            var result = await ExecRunner.RunAsync(execParams, CancellationToken.None, sandboxPolicy);
                            var endEv = Codex.NotifyExecCommandEnd(Guid.NewGuid().ToString(), Guid.NewGuid().ToString(), result.Stdout, result.Stderr, result.ExitCode);
                            if (opts.Json)
                                Console.WriteLine(System.Text.Json.JsonSerializer.Serialize(endEv));
                            else
                                processor.ProcessEvent(endEv);
                            if (logWriter != null)
                                await logWriter.WriteLineAsync(System.Text.Json.JsonSerializer.Serialize(endEv));
                        }
                        break;
                    case PatchApplyApprovalRequestEvent pr:
                        if (approvalPolicy == ApprovalMode.Never)
                        {
                            Console.WriteLine("Patch denied");
                            var deniedEv = Codex.NotifyBackgroundEvent(Guid.NewGuid().ToString(), "patch denied");
                            if (opts.Json)
                                Console.WriteLine(System.Text.Json.JsonSerializer.Serialize(deniedEv));
                            else
                                processor.ProcessEvent(deniedEv);
                        }
                        else
                        {
                            Console.Write($"Apply patch? [y/N] ");
                            var r = Console.ReadLine();
                            if (!r?.StartsWith("y", StringComparison.OrdinalIgnoreCase) ?? true)
                            {
                                Console.WriteLine("Patch denied");
                                var deniedEv = Codex.NotifyBackgroundEvent(Guid.NewGuid().ToString(), "patch denied");
                                if (opts.Json)
                                    Console.WriteLine(System.Text.Json.JsonSerializer.Serialize(deniedEv));
                                else
                                    processor.ProcessEvent(deniedEv);
                            }
                        }
                        break;
                    case PatchApplyBeginEvent pb:
                        foreach (var kv in pb.Changes)
                        {
                            var path = kv.Key;
                            switch (kv.Value)
                            {
                                case AddFileChange add:
                                    Directory.CreateDirectory(Path.GetDirectoryName(path)!);
                                    File.WriteAllText(path, add.Content);
                                    break;
                                case DeleteFileChange:
                                    if (File.Exists(path)) File.Delete(path);
                                    break;
                               case UpdateFileChange upd:
                                    var full = Path.GetFullPath(path);
                                    var lines = File.Exists(full) ? File.ReadAllLines(full).ToList() : new List<string>();
                                    var difflines = PatchParser.ParseUnified(upd.UnifiedDiff);
                                    int idx2 = 0;
                                    foreach (var ln in difflines)
                                    {
                                        if (ln.StartsWith("+"))
                                        {
                                            lines.Insert(idx2, ln.Substring(1));
                                            idx2++;
                                        }
                                        else if (ln.StartsWith("-"))
                                        {
                                            if (idx2 < lines.Count && lines[idx2] == ln.Substring(1))
                                                lines.RemoveAt(idx2);
                                        }
                                        else
                                        {
                                            if (idx2 < lines.Count && lines[idx2] == ln.TrimStart(' '))
                                                idx2++;
                                        }
                                    }
                                    File.WriteAllLines(full, lines);
                                    break;
                                }
                        }
                        break;
                    case TaskStartedEvent tsEvent:
                        break;
                    case TaskCompleteEvent tc:
                    if (providerId == "mock")
                    {
                        var aiResp = await client.ChatAsync(prompt);
                        Console.WriteLine(aiResp);
                    }
                    if (opts.LastMessageFile != null)
                        await File.WriteAllTextAsync(opts.LastMessageFile, tc.LastAgentMessage ?? string.Empty);
                    if (opts.NotifyCommand.Length > 0)
                        Codex.MaybeNotify(opts.NotifyCommand.ToList(),
                            new AgentTurnCompleteNotification(tc.Id, Array.Empty<string>(), tc.LastAgentMessage));
                    break;
                }
            }

            if (logWriter != null)
            {
                await logWriter.FlushAsync();
                logWriter.Dispose();
            }

            if (SessionManager.GetHistoryFile(sessionId) is { } histPath)
                Console.WriteLine($"History saved to {histPath}");

            if (opts.NotifyCommand.Length > 0)
                NotifyUtils.RunNotify(opts.NotifyCommand, "session_complete", envMap);
        }, binder, configOption, cdOption);
        return cmd;
    }
}
