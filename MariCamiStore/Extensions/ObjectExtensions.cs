using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace MariCamiStore.Extensions
{
    /// <summary>An object extensions.</summary>
    public static class ObjectExtensions
    {
        /// <summary>A T extension method that converts an obj to a JSON.</summary>
        /// <typeparam name="T">Generic type parameter.</typeparam>
        /// <param name="obj">The obj to act on.</param>
        /// <returns>Obj as a string.</returns>
        public static string ToJson<T>(this T obj)
        {
            return JsonConvert.SerializeObject(obj, new JsonSerializerSettings { ContractResolver = new CamelCasePropertyNamesContractResolver() });
        }
    }
}
