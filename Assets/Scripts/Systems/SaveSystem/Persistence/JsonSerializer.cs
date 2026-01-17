using UnityEngine;

public class JsonSerializer : ISerializer
{
    public string Serialize<T>(T obj)
    {
        return JsonUtility.ToJson(obj);
    }

    public T Deserialize<T>(string json)
    {
        return JsonUtility.FromJson<T>(json);
    }
}