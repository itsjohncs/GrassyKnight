using System;
using UnityEngine;


namespace GrassyKnight
{
    // Attach to the hero to give them a grassy compass friend
    class GrassyCompass : MonoBehaviour {
        // The target location in world space that the compass will point to
        public Vector2? Target = null;

        // Maximum distance between game object and compass
        public float Radius = 1.5f;

        // Will be used to automatically update Target ever SearchInterval
        // seconds if non-null.
        public GrassDB AllGrass = null;
        public float SearchInterval = 0.5f;
        private float _lastSearchTime = 0;

        // The actual compass object
        private GameObject _compassGameObject = null;

        public void Start() {
            try {
                _Start();
            } catch (System.Exception e) {
                GrassyKnight.Instance.LogException(
                    "Error in GrassyCompass.Start()", e);
            }
        }

        private void _Start() {
            _compassGameObject = new GameObject(
                "Grassy Compass", typeof(SpriteRenderer));

            // We'll destroy it when we're destroyed... so hopefully this keeps
            // anything else from destroying it until that happens.
            UnityEngine.Object.DontDestroyOnLoad(_compassGameObject);

            // Create a 1x1 green sprite. Maybe one day we can have a fancy
            // arrow sprite 😳
            Texture2D texture = new Texture2D(1, 1);
            texture.SetPixel(0, 0, Color.green);
            texture.Apply();
            Sprite sprite = Sprite.Create(
                texture,
                new Rect(0, 0, 1, 1),
                // This makes the pivot point the center of sprite
                new Vector2(0.5f, 0.5f),
                // And this makes the sprite 1 world unit by 1 world unit. The
                // default value makes it 1/100 world unit by 1/100...
                1);

            SpriteRenderer renderer =
                _compassGameObject.GetComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            // Keep us above everything on the default layer
            renderer.sortingOrder = 32767;
            renderer.enabled = false;

            // Size the sprite to be X of a game unit (though it'll be
            // affected by scaling of the knight).
            _compassGameObject.transform.parent = gameObject.transform;
            _compassGameObject.transform.localScale =
                new Vector3(1, 1, 0) * 0.1f;
        }

        public void Destroy() {
            try {
                UnityEngine.Object.Destroy(_compassGameObject);
            } catch (System.Exception e) {
                GrassyKnight.Instance.LogException(
                    "Error in GrassyCompass.Destroy()", e);
            }

        }

        // Called every frame, best be quick
        public void Update() {
            try {
                _Update();
            } catch (System.Exception e) {
                GrassyKnight.Instance.LogException(
                    "Error in GrassyCompass.Update()", e);
            }
        }

        private void _Update() {
            // Since finding the nearest uncut grass is mildly expensive, we
            // don't do it _every_ frame (though I suspect we could get away
            // with it just fine).
            if (AllGrass != null &&
                    _lastSearchTime + SearchInterval < Time.time) {
                // Can't use the game object's .scene.name because the hero
                // (the main object we want to attach this to) and any other
                // object set to not get destroyed on load have a garbage
                // scene name.
                string sceneName = GameManager.instance?.sceneName;
                if (sceneName != null) {
                    GrassKey nearestGrass = AllGrass.GetNearestUncutGrass(
                        gameObject.transform.position,
                        sceneName);
                    Target = nearestGrass?.Position;

                    _lastSearchTime = Time.time;
                }
            }

            if (Target != null) {
                // Position of target relative to game object. This'll account
                // for rotation, scale, and world position of game object
                // properly
                Vector3 targetLocalPosition =
                    gameObject.transform.InverseTransformPoint(Target.Value);
                _compassGameObject.transform.localPosition =
                    Vector3.ClampMagnitude(targetLocalPosition, Radius);
            }

            // Hide/show the compass appropriately
            _compassGameObject.GetComponent<SpriteRenderer>().enabled =
                Target != null;
        }
    }
}
