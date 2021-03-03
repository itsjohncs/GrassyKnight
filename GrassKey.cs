using System;
using System.Text;
using UnityEngine;


namespace GrassyKnight
{
	class GrassKey {
        public readonly string SceneName;
        public readonly string ObjectName;
        public readonly Vector2 Position;

        public GrassKey(string sceneName, string objectName, Vector2 position) {
            SceneName = sceneName;
            ObjectName = objectName;
            Position = position;
        }

        public static GrassKey FromGameObject(GameObject gameObject) {
            return new GrassKey(
                gameObject.scene.name,
                gameObject.name,
                // gameObject.position.z is being discarded here. The explicit
                // cast is added for clarity but is unnecessary.
                (Vector2)gameObject.transform.position);
        }

        public override string ToString() {
            return $"{SceneName}/{ObjectName} ({Position.x}, {Position.y})";
        }

        private (string, string, Vector2) ToTuple() {
            return (SceneName, ObjectName, Position);
        }

        public override int GetHashCode() {
            return ToTuple().GetHashCode();
        }

        public override bool Equals(object other) {
            return ToTuple() == ((GrassKey)other).ToTuple();
        }

        // The size of the arrays Serialize returns and Deserialize expects
        public const int NumSerializationTokens = 4;

        // Encodes into UTF-16 (which should be a no-op since that's how
        // strings are backed) and then converts to Base64. In the Remarks
        // section of https://docs.microsoft.com/en-us/dotnet/api/system.convert.tobase64string?view=net-5.0
        // it describes the alphabet used. Notably does not include `;`.
        private static string ToBase64(string str) {
            return Convert.ToBase64String(
                // Read "Unicode" as UTF-16
                Encoding.Unicode.GetBytes(str));
        }

        // Decodes a base 64 string into what should be valid UTF-16 which we
        // then convert to a string (which should be a no-op for the same
        // reason as above).
        private static string FromBase64(string str) {
            // Read "Unicode" as UTF-16
            return Encoding.Unicode.GetString(
                Convert.FromBase64String(str));
        }

        public string[] Serialize() {
            return new string[] {
                ToBase64(SceneName),
                ToBase64(ObjectName),
                Position.x.ToString(),
                Position.y.ToString(),
            };
        }

        public static GrassKey Deserialize(string[] serialized) {
            if (serialized.Length != NumSerializationTokens) {
                throw new ArgumentException(
                    $"Got {serialized.Length} tokens for " +
                    $"GrassKey.Deserialize. Expected " +
                    $"{NumSerializationTokens}.");
            }

            return new GrassKey(
                FromBase64(serialized[0]),
                FromBase64(serialized[1]),
                new Vector2(
                    float.Parse(serialized[2]),
                    float.Parse(serialized[3])));
        }
    }
}
