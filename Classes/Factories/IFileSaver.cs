using System.Text;

namespace liXMaiLips.Factories;

public interface IFileSaver
{
    bool Save(string content, string fileName);
}

public class XmlFileSaver : IFileSaver
{
    public bool Save(string content, string filePath)
    {
        try
        {
            string? directory = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(directory))
                Directory.CreateDirectory(directory);

            File.WriteAllText(filePath, content, Encoding.UTF8);
            return true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"XML save error: {ex.Message}");
            return false;
        }
    }
}

public class HtmlFileSaver : IFileSaver
{
    public bool Save(string content, string filePath)
    {
        try
        {
            string? directory = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(directory))
                Directory.CreateDirectory(directory);

            File.WriteAllText(filePath, content, Encoding.UTF8);
            return true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"HTML save error: {ex.Message}");
            return false;
        }
    }
}