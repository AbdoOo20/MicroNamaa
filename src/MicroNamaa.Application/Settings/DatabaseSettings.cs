namespace MicroNamaa.Application.Settings;

public class DatabaseSettings
{
    public const string SectionName = "Database";

    /// <summary>Applies pending EF migrations when the API starts.</summary>
    public bool MigrateOnStartup { get; set; }
}
