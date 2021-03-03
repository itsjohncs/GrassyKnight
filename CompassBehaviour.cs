using System;
using UnityEngine;


namespace GrassyKnight
{
    // Will move its gameObject to circle around the hero such that it's always
    // pointing at a target (like if you draw a line from the center of the
    // origin object to the target). See CreateCompassGameObject().
    class CompassBehaviour : MonoBehaviour {
        // The target location in world space
        public Vector2? Target = null;

        // Maximum distance between hero and compass
        public float Radius = 1.5f;

        // Will be used to automatically update Target ever SearchInterval
        // seconds if non-null.
        public GrassDB AllGrass = null;
        public float SearchInterval = 0.5f;
        private float _lastSearchTime = 0;

        // Called every frame, best be quick
        public void Update() {
            try {
                _Update();
            } catch (System.Exception e) {
                ModMain.Instance.LogException(
                    "Error in CompassBehaviour.Update()", e);
            }
        }

        private void _Update() {
            // Try to find the hero game object... we can't do much without it
            GameObject hero = GameManager.instance?.hero_ctrl?.gameObject;

            // Since finding the nearest uncut grass is mildly expensive, we
            // don't do it _every_ frame (though I suspect we could get away
            // with it just fine).
            if (AllGrass != null && hero != null &&
                    _lastSearchTime + SearchInterval < Time.time) {
                string sceneName = GameManager.instance?.sceneName;
                if (sceneName != null) {
                    GrassKey nearestGrass = AllGrass.GetNearestUncutGrass(
                        hero.transform.position,
                        sceneName);
                    Target = nearestGrass?.Position;

                    _lastSearchTime = Time.time;
                }
            }

            bool shouldCompassBeVisible = hero != null && Target != null;

            if (shouldCompassBeVisible) {
                gameObject.transform.parent = hero.transform;
                gameObject.transform.localPosition =
                    Vector3.ClampMagnitude(
                        // Target and transform.position are in world space
                        // scale. Bring it into hero's local scale because
                        // that's what we'll be affected by.
                        hero.transform.InverseTransformVector(
                            (Vector3)Target.Value - hero.transform.position),
                        Radius);
            }

            // Hide/show the compass appropriately
            foreach (Renderer renderer in gameObject.GetComponents<Renderer>()) {
                renderer.enabled = shouldCompassBeVisible;
            }
        }

        public static GameObject CreateCompassGameObject(GrassDB allGrass = null) {
            GameObject compassGameObject = new GameObject(
                "Compass", typeof(SpriteRenderer), typeof(CompassBehaviour));
            UnityEngine.Object.DontDestroyOnLoad(compassGameObject);

            // Create a 1x1 green sprite. Maybe one day we can have a fancy
            // arrow sprite 😳
            Texture2D blackTexture = new Texture2D(1, 1);
            blackTexture.SetPixel(0, 0, Color.green);
            blackTexture.Apply();
            Sprite blackSprite = Sprite.Create(
                blackTexture,
                new Rect(0, 0, 1, 1),
                // This makes the pivot point the center of sprite
                new Vector2(0.5f, 0.5f),
                // And this makes the sprite 1 world unit by 1 world unit. The
                // default value makes it 1/100 world unit by 1/100...
                1);

            SpriteRenderer renderer =
                compassGameObject.GetComponent<SpriteRenderer>();
            renderer.sprite = blackSprite;
            // Keep us above everything on the default layer
            renderer.sortingOrder = 32767;

            compassGameObject.GetComponent<CompassBehaviour>().AllGrass = allGrass;

            // Size the sprite to be 0.25 of a game unit (though it'll be
            // affected by scaling of the knight).
            compassGameObject.transform.localScale =
                new Vector3(1, 1, 0) * 0.25f;

            return compassGameObject;
        }
    }
}
