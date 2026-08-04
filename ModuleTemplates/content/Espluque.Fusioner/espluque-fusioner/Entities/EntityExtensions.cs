using System.Reflection;

namespace espluque-fusioner.Entities
{
    internal static class EntityExtensions
    {
        public static List<KeyValuePair<string, string>> GetTextProperties<TInstance>(this TInstance instance) where TInstance : class
        {
            if (instance is null) return [];

            List<KeyValuePair<string, string>> result = [];

            List<PropertyInfo> properties = typeof(TInstance).GetProperties()
                .Where(x => x.PropertyType == typeof(string))
                .ToList();

            foreach (PropertyInfo property in properties)
            {
                string? value = property.GetValue(instance) as string;

                if (value is null) continue;

                result.Add(new KeyValuePair<string, string>(property.Name, value));
            }

            return result;
        }
    }
}
