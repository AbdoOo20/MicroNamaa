namespace MicroNamaa.Application.Settings;

public class StorageSettings
{
    public const string SectionName = "Storage";

    /// <summary>Folder for uploaded images, relative to the app's content root or absolute.</summary>
    public string RootPath { get; set; } = "App_Data/uploads";
}
