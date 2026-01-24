namespace BookingSystem.Application.Interfaces;

public interface ISerializationService
{
    string SerializeToJson<T>(T obj) where T : class;
    string SerializeToXml<T>(T obj) where T : class;
    T DeserializeFromJson<T>(string jsonString) where T : class;
    T DeserializeFromXml<T>(string xmlString) where T : class;
    bool SaveToJsonFile<T>(T obj, string filePath) where T : class;
    bool SaveToXmlFile<T>(T obj, string filePath) where T : class;
    T LoadFromJsonFile<T>(string filePath) where T : class;
    T LoadFromXmlFile<T>(string filePath) where T : class;
    Task<bool> SaveToJsonFileAsync<T>(T obj, string filePath) where T : class;
    Task<T> LoadFromJsonFileAsync<T>(string filePath) where T : class;
}
