using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System.Reflection;

public class CustomContractResolver : DefaultContractResolver
{
    protected override JsonProperty CreateProperty(MemberInfo member, MemberSerialization memberSerialization)
    {
        JsonProperty property = base.CreateProperty(member, memberSerialization);

        // Exclude Unity-specific properties like "rigidbody"
        if (member.DeclaringType.Namespace == "UnityEngine")
        {
            property.ShouldSerialize = instance => false;
        }
        // Exclude properties like magnitude and normalized
        if (member.Name == "magnitude" || member.Name == "normalized")
            property.ShouldSerialize = instance => false;

        return property;
    }
}
