/// <summary>
/// Port of codex-rs/core/src/config_types.rs History types (done).
/// </summary>
namespace CodexCli.Config;

public enum HistoryPersistence
{
    SaveAll,
    None,
}

public class History
{
    public HistoryPersistence Persistence { get; set; } = HistoryPersistence.SaveAll;
    public int? MaxBytes { get; set; }
}
