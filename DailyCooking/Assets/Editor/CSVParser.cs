using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using UnityEngine;

public class CSVParser<T> where T : class, new()
{
    private static readonly Dictionary<Type, Func<string, object>> s_customParsers = new()
    {
        { typeof(ShopReward), s => { var sep = s.Split('_'); return new ShopReward(sep[0], int.Parse(sep[1])); } }
    };
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
                            var enumType = propertyInfo.PropertyType;
                            var enumValue = Enum.Parse(enumType, value);
                            propertyInfo.SetValue(obj, enumValue);
                        }
                        else if (propertyInfo.PropertyType.IsGenericType && propertyInfo.PropertyType.GetGenericTypeDefinition() == typeof(List<>))
                        {
                            var elementType = propertyInfo.PropertyType.GetGenericArguments()[0];
                            var list = (IList)Activator.CreateInstance(propertyInfo.PropertyType);
                            if (!string.IsNullOrEmpty(value))
                            {
                                var parts = value.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
                                foreach (var p in parts)
                                {
                                    if (s_customParsers.TryGetValue(elementType, out var parser))
                                    {
                                        list.Add(parser(p));
                                    }
                                    else if (elementType.IsEnum)
                                    {
                                        list.Add(Enum.Parse(elementType, p));
                                    }
                                    else
                                    {
                                        list.Add(Convert.ChangeType(p, elementType));
                                    }
                                }
                            }
                            propertyInfo.SetValue(obj, list);
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
    public static List<T> ParseCSV(TextAsset textFile, string delimiter = ",")
    {
        List<T> data = new List<T>();

        string content = textFile.text;
        //Debug.Log($"Parsing CSV content: {content}");
        string[] lines = content.Split(new[] { '\n', '\r' }, System.StringSplitOptions.RemoveEmptyEntries);
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
                    //Debug.Log($"value : {value}");
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
                        else if (propertyInfo.PropertyType == typeof(Vector3))
                        {
                            propertyInfo.SetValue(obj, ParseVector3FromSpaceSeparated(value));
                            //Debug.Log($"Parsed Vector3: {ParseVector3FromSpaceSeparated(value)} from value: {value}");
                        }
                        else if(propertyInfo.PropertyType == typeof(Color))
                        {
                            propertyInfo.SetValue(obj, ParseColorFromString(value));
                            //Debug.Log($"Parsed Color: {obj} from value: {value}");
                        }
                        else if (propertyInfo.PropertyType.IsGenericType && propertyInfo.PropertyType.GetGenericTypeDefinition() == typeof(List<>))
                        {
                            var elementType = propertyInfo.PropertyType.GetGenericArguments()[0];
                            var list = (IList)Activator.CreateInstance(propertyInfo.PropertyType);
                            if (!string.IsNullOrEmpty(value))
                            {
                                var parts = value.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
                                foreach (var p in parts)
                                {
                                    if (s_customParsers.TryGetValue(elementType, out var parser))
                                    {
                                        list.Add(parser(p));
                                    }
                                    else if (elementType.IsEnum)
                                    {
                                        list.Add(Enum.Parse(elementType, p));
                                    }
                                    else
                                    {
                                        list.Add(Convert.ChangeType(p, elementType));
                                    }
                                }
                            }
                            propertyInfo.SetValue(obj, list);
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

    #region Vector3
    public static Vector3 ParseVector3FromString(string vectorStr)
    {
        // Remove parentheses and spaces
        vectorStr = vectorStr.Trim('(', ')').Replace(" ", "");

        // Split by comma
        string[] parts = vectorStr.Split(',');

        if (parts.Length != 3)
        {
            Debug.LogError("Invalid Vector3 format in CSV: " + vectorStr);
            return Vector3.zero;
        }

        float x = float.Parse(parts[0], CultureInfo.InvariantCulture);
        float y = float.Parse(parts[1], CultureInfo.InvariantCulture);
        float z = float.Parse(parts[2], CultureInfo.InvariantCulture);

        return new Vector3(x, y, z);
    }
    public static Vector3 ParseVector3FromSpaceSeparated(string vectorStr)
    {
        string[] parts = vectorStr.Trim('(', ')').Split(' ');

        if (parts.Length != 3)
        {
            Debug.LogError("Invalid Vector3 format in CSV: " + vectorStr);
            return Vector3.zero;
        }

        float x = float.Parse(parts[0], CultureInfo.InvariantCulture);
        float y = float.Parse(parts[1], CultureInfo.InvariantCulture);
        float z = float.Parse(parts[2], CultureInfo.InvariantCulture);

        return new Vector3(x, y, z);
    }
    public static string Vector3ToString(Vector3 vector)
    {
        return $"({vector.x} {vector.y} {vector.z})";
    }
    #endregion

    #region Color
    public static Color ParseColorFromString(string colorStr)
    {
        // Remove quotes and spaces
        string[] parts = colorStr.Trim('(', ')').Split(' ');

        if (parts.Length < 3)
        {
            Debug.LogError("Invalid color format: " + colorStr);
            return Color.white;
        }

        float r = float.Parse(parts[0]);
        float g = float.Parse(parts[1]);
        float b = float.Parse(parts[2]);
        float a = float.Parse(parts[3]);

        return new Color(r , g, b , a );
    }

    public static string ColorToString(Color backgroundColor)
    {
        return $"({backgroundColor.r} {backgroundColor.g} {backgroundColor.b} {backgroundColor.a})";

    }
    #endregion
}