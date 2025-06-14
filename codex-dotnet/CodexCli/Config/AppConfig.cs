using CodexCli.Commands;
using Tomlyn;
using System.Linq;
using System.Collections.Generic;

namespace CodexCli.Config;

public class AppConfig
{
    public string[]? NotifyCommand { get; set; }
    public string? Model { get; set; }
    public string? ModelProvider { get; set; }
    public string? CodexHome { get; set; }
    public string? Instructions { get; set; }
    public bool HideAgentReasoning { get; set; }
    public bool DisableResponseStorage { get; set; }
    public ReasoningEffort? ModelReasoningEffort { get; set; }
    public ReasoningSummary? ModelReasoningSummary { get; set; }
    public ShellEnvironmentPolicy ShellEnvironmentPolicy { get; set; } = new();
    public History History { get; set; } = new();
    public UriBasedFileOpener FileOpener { get; set; } = UriBasedFileOpener.None;
    public int ProjectDocMaxBytes { get; set; } = 32 * 1024;
    public Tui Tui { get; set; } = new();
    public Dictionary<string, ModelProviderInfo> ModelProviders { get; set; } = new();
    public Dictionary<string, ConfigProfile> Profiles { get; set; } = new();

    public static AppConfig Load(string path, string? profile = null)
    {
        var text = File.ReadAllText(path);
        var model = Toml.ToModel(text) as IDictionary<string, object?> ?? new Dictionary<string, object?>();
        var cfg = new AppConfig();
        if (model.TryGetValue("notify", out var notify) && notify is object[] arr)
            cfg.NotifyCommand = arr.Select(o => o?.ToString() ?? string.Empty).ToArray();
        if (model.TryGetValue("model", out var m)) cfg.Model = m?.ToString();
        if (model.TryGetValue("model_provider", out var mp)) cfg.ModelProvider = mp?.ToString();
        if (model.TryGetValue("codex_home", out var h)) cfg.CodexHome = h?.ToString();
        if (model.TryGetValue("instructions", out var inst)) cfg.Instructions = inst?.ToString();
        if (model.TryGetValue("project_doc_max_bytes", out var pdm) && int.TryParse(pdm?.ToString(), out var pdmv))
            cfg.ProjectDocMaxBytes = pdmv;
        if (model.TryGetValue("hide_agent_reasoning", out var har))
            cfg.HideAgentReasoning = har is bool hb ? hb : bool.TryParse(har?.ToString(), out var b) && b;
        if (model.TryGetValue("disable_response_storage", out var drsVal))
            cfg.DisableResponseStorage = drsVal is bool db ? db : bool.TryParse(drsVal?.ToString(), out var b2) && b2;
        if (model.TryGetValue("approval_policy", out var apVal) && Enum.TryParse<ApprovalMode>(apVal?.ToString(), true, out var apParsed))
            cfg.ApprovalPolicy = apParsed;
        if (model.TryGetValue("model_reasoning_effort", out var mre) && Enum.TryParse<ReasoningEffort>(mre?.ToString(), true, out var mrev))
            cfg.ModelReasoningEffort = mrev;
        if (model.TryGetValue("model_reasoning_summary", out var mrs) && Enum.TryParse<ReasoningSummary>(mrs?.ToString(), true, out var mrsv))
            cfg.ModelReasoningSummary = mrsv;
        if (model.TryGetValue("shell_environment_policy", out var sep) && sep is IDictionary<string, object?> sepd)
        {
            if (sepd.TryGetValue("inherit", out var inh) && Enum.TryParse<ShellEnvironmentPolicyInherit>(inh?.ToString(), true, out var inv))
                cfg.ShellEnvironmentPolicy.Inherit = inv;
            if (sepd.TryGetValue("ignore_default_excludes", out var ide))
                cfg.ShellEnvironmentPolicy.IgnoreDefaultExcludes = ide is bool b ? b : bool.TryParse(ide?.ToString(), out var bb) && bb;
            if (sepd.TryGetValue("exclude", out var exc) && exc is object[] excArr)
                cfg.ShellEnvironmentPolicy.Exclude = excArr.Select(o => EnvironmentVariablePattern.CaseInsensitive(o?.ToString() ?? string.Empty)).ToList();
            if (sepd.TryGetValue("set", out var set) && set is IDictionary<string, object?> setd)
                cfg.ShellEnvironmentPolicy.Set = setd.ToDictionary(kv => kv.Key, kv => kv.Value?.ToString() ?? string.Empty);
            if (sepd.TryGetValue("include_only", out var inc) && inc is object[] incArr)
                cfg.ShellEnvironmentPolicy.IncludeOnly = incArr.Select(o => EnvironmentVariablePattern.CaseInsensitive(o?.ToString() ?? string.Empty)).ToList();
        }
        if (model.TryGetValue("history", out var hist) && hist is IDictionary<string, object?> hmap)
        {
            if (hmap.TryGetValue("persistence", out var pval) && Enum.TryParse<HistoryPersistence>(pval?.ToString(), true, out var pv))
                cfg.History.Persistence = pv;
            if (hmap.TryGetValue("max_bytes", out var mb) && int.TryParse(mb?.ToString(), out var mbv))
                cfg.History.MaxBytes = mbv;
        }
        if (model.TryGetValue("file_opener", out var fo) && Enum.TryParse<UriBasedFileOpener>(fo?.ToString(), true, out var fov))
            cfg.FileOpener = fov;
        if (model.TryGetValue("tui", out var tuiVal) && tuiVal is IDictionary<string, object?> tm)
        {
            if (tm.TryGetValue("disable_mouse_capture", out var dmc))
                cfg.Tui.DisableMouseCapture = dmc is bool b3 ? b3 : bool.TryParse(dmc?.ToString(), out var tb) && tb;
        }
        if (model.TryGetValue("model_providers", out var mpMap) && mpMap is IDictionary<string, object?> provMap)
        {
            foreach (var (k,v) in provMap)
            {
                if (v is IDictionary<string, object?> pv)
                {
                    var info = new ModelProviderInfo();
                    if (pv.TryGetValue("name", out var n)) info.Name = n?.ToString() ?? string.Empty;
                    if (pv.TryGetValue("base_url", out var b)) info.BaseUrl = b?.ToString() ?? string.Empty;
                    if (pv.TryGetValue("env_key", out var e)) info.EnvKey = e?.ToString();
                    if (pv.TryGetValue("env_key_instructions", out var ins)) info.EnvKeyInstructions = ins?.ToString();
                    if (pv.TryGetValue("wire_api", out var wa) && Enum.TryParse<WireApi>(wa?.ToString(), true, out var wv))
                        info.WireApi = wv;
                    cfg.ModelProviders[k] = info;
                }
            }
        }
        if (model.TryGetValue("profiles", out var profs) && profs is IDictionary<string, object?> pmap)
        {
            foreach (var (k, v) in pmap)
            {
                if (v is IDictionary<string, object?> pm)
                {
                    var cp = new ConfigProfile();
                    if (pm.TryGetValue("model", out var m2)) cp.Model = m2?.ToString();
                    if (pm.TryGetValue("model_provider", out var mp2)) cp.ModelProvider = mp2?.ToString();
                    if (pm.TryGetValue("approval_policy", out var ap) && Enum.TryParse<ApprovalMode>(ap?.ToString(), true, out var apv)) cp.ApprovalPolicy = apv;
                    if (pm.TryGetValue("disable_response_storage", out var drs2))
                        cp.DisableResponseStorage = drs2 is bool db ? db : bool.TryParse(drs2?.ToString(), out var b) ? b : (bool?)null;
                    if (pm.TryGetValue("model_reasoning_effort", out var mre2) && Enum.TryParse<ReasoningEffort>(mre2?.ToString(), true, out var mrev2))
                        cp.ModelReasoningEffort = mrev2;
                    if (pm.TryGetValue("model_reasoning_summary", out var mrs2) && Enum.TryParse<ReasoningSummary>(mrs2?.ToString(), true, out var mrsv2))
                        cp.ModelReasoningSummary = mrsv2;
                    if (pm.TryGetValue("project_doc_max_bytes", out var pdm2) && int.TryParse(pdm2?.ToString(), out var pdmv2))
                        cp.ProjectDocMaxBytes = pdmv2;
                    cfg.Profiles[k] = cp;
                }
            }
        }
        // Merge built-in provider definitions
        foreach (var (k,v) in ModelProviderInfo.BuiltIns)
        {
            cfg.ModelProviders.TryAdd(k, v);
        }
        cfg.CodexHome ??= Environment.GetEnvironmentVariable("CODEX_HOME") ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".codex");
        if (cfg.Instructions == null)
        {
            var instPath = Path.Combine(cfg.CodexHome, "instructions.md");
            if (File.Exists(instPath))
                cfg.Instructions = File.ReadAllText(instPath);
        }
        if (profile != null && cfg.Profiles.TryGetValue(profile, out var p))
        {
            if (p.Model != null) cfg.Model = p.Model;
            if (p.ModelProvider != null) cfg.ModelProvider = p.ModelProvider;
            if (p.ApprovalPolicy != null) cfg.ApprovalPolicy = p.ApprovalPolicy.Value;
            if (p.DisableResponseStorage != null) cfg.DisableResponseStorage = p.DisableResponseStorage.Value;
            if (p.ModelReasoningEffort != null) cfg.ModelReasoningEffort = p.ModelReasoningEffort;
            if (p.ModelReasoningSummary != null) cfg.ModelReasoningSummary = p.ModelReasoningSummary;
            if (p.ProjectDocMaxBytes != null) cfg.ProjectDocMaxBytes = p.ProjectDocMaxBytes.Value;
        }
        return cfg;
    }

    public ApprovalMode ApprovalPolicy { get; set; } = ApprovalMode.UnlessAllowListed;

    public ModelProviderInfo GetProvider(string id)
    {
        if (ModelProviders.TryGetValue(id, out var info)) return info;
        return ModelProviderInfo.BuiltIns.TryGetValue(id, out var d) ? d : ModelProviderInfo.BuiltIns["openai"];
    }
}
