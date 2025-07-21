using ClipMate.Models;
using System.Collections.ObjectModel;
using System.Text.Json;

namespace ClipMate.Services;

public class JsonService
{
    private static string _filePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "clipmate.json");

    public void Save(object obj)
    {
        try
        {
            var jsonString = JsonSerializer.Serialize(obj);
            File.WriteAllText(_filePath, jsonString);
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