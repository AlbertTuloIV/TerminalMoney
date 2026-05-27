namespace TM.Data.Persistence;

public static class LocalDatabasepath
{
    public static string GetDatabasePath()
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var appFolder = Path.Combine(appData, "TerminalMoney");

        Directory.CreateDirectory(appFolder);

        return Path.Combine(appFolder, "terminalmoney.db");
    }
}