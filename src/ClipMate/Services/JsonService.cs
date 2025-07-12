using ClipMate.Models;
using System.Collections.ObjectModel;
using System.Text.Json;

namespace ClipMate.Services;

public class JsonService
{
    private static string _filePath;

    public JsonService()
    {
        var folderPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "ManuHub", "ClipMate");

        if (!Directory.Exists(folderPath))
        {
            Directory.CreateDirectory(folderPath);
        }

        _filePath = Path.Combine(folderPath, "clipmate.json");
    }
    public void Save(object obj)
    {
        try
        {
            var jsonString = JsonSerializer.Serialize(obj);
            File.WriteAllText(_filePath, jsonString);
            Console.WriteLine(_filePath);
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
        }
    }

    public async Task<IEnumerable<DownloadJob>> GetAsync()
    {
        try
        {
            if (File.Exists(_filePath))
            {
                Console.WriteLine(_filePath);
                var jsonString = await File.ReadAllTextAsync(_filePath);
                return JsonSerializer.Deserialize<ObservableCollection<DownloadJob>>(jsonString)!;
            }

            return [];
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
            return [];            
        }
    }
}