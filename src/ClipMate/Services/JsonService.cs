using ClipMate.Models;
using System.Collections.ObjectModel;
using System.Text.Json;

namespace ClipMate.Services;

public class JsonService
{
    public void Save(object obj)
    {
        try
        {
            var filePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "clipmate.json");
            var jsonString = JsonSerializer.Serialize(obj);
            File.WriteAllText(filePath, jsonString);
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
            var filePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "clipmate.json");

            if (File.Exists(filePath))
            {
                var jsonString = await File.ReadAllTextAsync(filePath);
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