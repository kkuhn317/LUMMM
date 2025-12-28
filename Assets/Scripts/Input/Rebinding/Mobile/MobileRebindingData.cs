using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json;

public class MobileRebindingData
{
    public const float DefaultPressedOpacity = 0.38f;
    public const float DefaultUnpressedOpacity = 0.38f;
    public float buttonPressedOpacity = DefaultPressedOpacity;
    public float buttonUnpressedOpacity = DefaultUnpressedOpacity;

    public class MobileButtonData
    {
        // Newtonsoft.Json doesn't easily support Vector2
        // https://discussions.unity.com/t/jsonserializationexception-self-referencing-loop-detected/877513
        public float posX;
        public float posY;

        [JsonIgnore]
        public Vector2 position {
            get {
                return new Vector2(posX, posY);
            }
            set {
                posX = value.x;
                posY = value.y;
            }
        }
        public float scale;
    }

    public Dictionary<string, MobileButtonData> buttonData = new();
}