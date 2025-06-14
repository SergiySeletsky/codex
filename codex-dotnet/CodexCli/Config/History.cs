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
