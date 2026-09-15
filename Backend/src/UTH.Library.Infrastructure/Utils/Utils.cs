namespace UTH.Library.Infrastructure.Utils;

public sealed class Utils
{
    public string ReadPEMFile(string path)
    {
        if (!File.Exists(path))
            throw new FileNotFoundException($"PEM file not found: {path}");
        return File.ReadAllText(path);
    }

    public bool CheckIfFileExists(string path)
    {
        return File.Exists(path);
    }
}
