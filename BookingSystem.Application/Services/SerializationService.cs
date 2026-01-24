using BookingSystem.Application.Interfaces;
using Serializator.Extensions;

namespace BookingSystem.Application.Services
{
    public class SerializationService : ISerializationService
    {
        public string SerializeToJson<T>(T obj) where T : class
        {
            return obj.ToJson();
        }

        public string SerializeToXml<T>(T obj) where T : class
        {
            return obj.ToXml();
        }

        public T DeserializeFromJson<T>(string jsonString) where T : class
        {
            return jsonString.FromJson<T>();
        }

        public T DeserializeFromXml<T>(string xmlString) where T : class
        {
            return xmlString.FromXml<T>();
        }

        public bool SaveToJsonFile<T>(T obj, string filePath) where T : class
        {
            return obj.ToJsonFile(filePath);
        }

        public bool SaveToXmlFile<T>(T obj, string filePath) where T : class
        {
            return obj.ToXmlFile(filePath);
        }

        public T LoadFromJsonFile<T>(string filePath) where T : class
        {
            return filePath.FromJsonFile<T>();
        }

        public T LoadFromXmlFile<T>(string filePath) where T : class
        {
            return filePath.FromXmlFile<T>();
        }

        public async Task<bool> SaveToJsonFileAsync<T>(T obj, string filePath) where T : class
        {
            return await obj.ToJsonFileAsync(filePath);
        }

        public async Task<T> LoadFromJsonFileAsync<T>(string filePath) where T : class
        {
            return await filePath.FromJsonFileAsync<T>();
        }

    }
}
