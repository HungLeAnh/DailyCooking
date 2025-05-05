using System;
using System.Collections.Generic;
using System.IO;

public class CSVParser<T> where T : class, new()
{
    public static List<T> ParseCSV(string filePath, string delimiter = ",")
    {
        List<T> data = new List<T>();

        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException("File not found", filePath);
        }

        string[] lines = File.ReadAllLines(filePath);

        // Skip header if present
        if (lines.Length > 0)
        {
            string[] headers = lines[0].Split(delimiter.ToCharArray());

            // Parse data rows
            for (int i = 1; i < lines.Length; i++)
            {
                string[] values = lines[i].Split(delimiter.ToCharArray());

                T obj = new T();

                // Use reflection to set properties
                for (int j = 0; j < headers.Length; j++)
                {
                    string header = headers[j];
                    string value = values[j];

                    // Find property by name and set its value
                    var propertyInfo = typeof(T).GetProperty(header);
                    if (propertyInfo != null)
                    {
                        if (propertyInfo.PropertyType.IsEnum)
                        {
                            // Handle enum type
                            var enumType = propertyInfo.PropertyType;
                            var enumValue = Enum.Parse(enumType, value);
                            propertyInfo.SetValue(obj, enumValue);
                        }
                        else
                        {
                            // Handle non-enum types
                            propertyInfo.SetValue(obj, Convert.ChangeType(value, propertyInfo.PropertyType));
                        }
                    }
                }

                data.Add(obj);
            }
        }

        return data;
    }
}