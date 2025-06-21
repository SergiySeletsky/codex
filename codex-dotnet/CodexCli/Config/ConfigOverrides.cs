using Tomlyn;
using System.Linq;
using System.Collections.Generic;
using CodexCli.Commands;

// Ported from codex-rs/common/src/config_override.rs

namespace CodexCli.Config;

public class ConfigOverrides
{
    public Dictionary<string, object?> Overrides { get; } = new();

    public static ConfigOverrides Parse(IEnumerable<string> pairs)
    {
        var overrides = new ConfigOverrides();
        foreach (var pair in pairs)
        {
            var idx = pair.IndexOf('=');
            if (idx <= 0) continue;
            var key = pair.Substring(0, idx).Trim();
            var valueStr = pair[(idx + 1)..].Trim();
            var wrapped = $"_x_ = {valueStr}";
            try
            {
                var model = Toml.ToModel(wrapped);
                if (model.TryGetValue("_x_", out var val))
                    overrides.Overrides[key] = val;
                else
                    overrides.Overrides[key] = valueStr;
            }
            catch
            {
                overrides.Overrides[key] = valueStr;
            }
        }
        return overrides;
    }

    public void Apply(AppConfig cfg)
    {
        foreach (var (key, value) in Overrides)
        {
            switch (key)
            {
                case "model":
                    cfg.Model = value?.ToString();
                    break;
                case "model_provider":
                    cfg.ModelProvider = value?.ToString();
                    break;
                case "codex_home":
                    cfg.CodexHome = value?.ToString();
                    break;
                case "instructions":
                    cfg.Instructions = value?.ToString();
                    break;
                case "notify":
                    if (value is IEnumerable<object?> seq)
                        cfg.NotifyCommand = seq.Select(o => o?.ToString() ?? string.Empty).ToArray();
                    break;
                case "hide_agent_reasoning":
                    if (value is bool b1) cfg.HideAgentReasoning = b1;
                    else if (bool.TryParse(value?.ToString(), out var b1p)) cfg.HideAgentReasoning = b1p;
                    break;
                case "disable_response_storage":
                    if (value is bool b2) cfg.DisableResponseStorage = b2;
                    else if (bool.TryParse(value?.ToString(), out var b2p)) cfg.DisableResponseStorage = b2p;
                    break;
                case "approval_policy":
                    if (Enum.TryParse<ApprovalMode>(value?.ToString(), true, out var ap))
                        cfg.ApprovalPolicy = ap;
                    break;
                case "model_reasoning_effort":
                    if (Enum.TryParse<ReasoningEffort>(value?.ToString(), true, out var mre))
                        cfg.ModelReasoningEffort = mre;
                    break;
                case "model_reasoning_summary":
                    if (Enum.TryParse<ReasoningSummary>(value?.ToString(), true, out var mrs))
                        cfg.ModelReasoningSummary = mrs;
                    break;
            }
        }
    }
}
