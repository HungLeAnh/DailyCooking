using UnityEngine;
using Newtonsoft.Json;
using System;
using Newtonsoft.Json.Linq;

public class UniversalUnityConverter : JsonConverter
{
    public override bool CanConvert(Type objectType)
    {
        return objectType == typeof(Vector3) ||
               objectType == typeof(Vector3Int) ||
               objectType == typeof(Vector2) ||
               objectType == typeof(Vector2Int) ||
               objectType == typeof(Quaternion) ||
               objectType == typeof(Color);
        // Extend with other Unity types
    }

    public override void WriteJson(JsonWriter writer, object value, Newtonsoft.Json.JsonSerializer serializer)
    {
        JObject obj = new JObject();

        switch (value)
        {
            case Vector3 vector3:
                obj["x"] = vector3.x;
                obj["y"] = vector3.y;
                obj["z"] = vector3.z;
                break;            

            case Vector3Int  vector3Int:
                obj["x"] = vector3Int.x;
                obj["y"] = vector3Int.y;
                obj["z"] = vector3Int.z;
                break;

            case Vector2 vector2:
                obj["x"] = vector2.x;
                obj["y"] = vector2.y;
                break;

            case Vector2Int vector2Int:
                obj["x"] = vector2Int.x;
                obj["y"] = vector2Int.y;
                break;

            case Quaternion quaternion:
                obj["x"] = quaternion.x;
                obj["y"] = quaternion.y;
                obj["z"] = quaternion.z;
                obj["w"] = quaternion.w;
                break;

            case Color color:
                obj["r"] = color.r;
                obj["g"] = color.g;
                obj["b"] = color.b;
                obj["a"] = color.a;
                break;

            // Add more Unity types here (e.g., Rect, Bounds)
            default:
                throw new JsonSerializationException($"Unsupported Unity type: {value.GetType()}");
        }

        obj.WriteTo(writer);
    }

    public override object ReadJson(JsonReader reader, Type objectType, object existingValue, Newtonsoft.Json.JsonSerializer serializer)
    {
        JObject obj = JObject.Load(reader);

        if (objectType == typeof(Vector3))
            return new Vector3((float)obj["x"], (float)obj["y"], (float)obj["z"]);   
        
        if (objectType == typeof(Vector3Int))
            return new Vector3((int)obj["x"], (int)obj["y"], (int)obj["z"]);

        if (objectType == typeof(Vector2))
            return new Vector2((float)obj["x"], (float)obj["y"]);

        if(objectType == typeof(Vector2Int))
            return new Vector2Int((int)obj["x"], (int)obj["y"]);

        if (objectType == typeof(Quaternion))
            return new Quaternion((float)obj["x"], (float)obj["y"], (float)obj["z"], (float)obj["w"]);

        if (objectType == typeof(Color))
            return new Color((float)obj["r"], (float)obj["g"], (float)obj["b"], (float)obj["a"]);
        
        throw new JsonSerializationException($"Unsupported Unity type: {objectType}");
    }
}
