using System;
using UnityEngine;


namespace GrassPls
{
    // Attach to the hero to give them a grassy compass friend
    class GrassyCompass : MonoBehaviour {
        // The target location in world space that the compass will point to
        public Vector2? Target = null;

        // Maximum distance between game object and compass
        public float Radius = 1.5f;

        // A hotkey that will toggle the compass visible/invisible
        public KeyCode? ToggleHotkey = null;

        // Whether the compass is toggled on or off
        public bool ToggledOn { get; private set; } = true;

        // Will be used to automatically update Target every frame if non-null
        public GrassDB AllGrass = null;

        // The actual compass object
        private GameObject _compassGameObject = null;

        public void Start() {
            try {
                _Start();
            } catch (System.Exception e) {
                GrassPls.Instance.LogException(
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
                GrassPls.Instance.LogException(
                    "Error in GrassyCompass.Destroy()", e);
            }

        }

        // Called every frame, best be quick
        public void Update() {
            try {
                _Update();
            } catch (System.Exception e) {
                GrassPls.Instance.LogException(
                    "Error in GrassyCompass.Update()", e);
            }
        }

        private void _Update() {
            if (ToggleHotkey != null &&
                    Input.GetKeyDown(ToggleHotkey.Value)) {
                ToggledOn = !ToggledOn;

                string prettyValue = ToggledOn ? "on" : "off";
                GrassPls.Instance.LogDebug(
                    $"Toggling Grassy compass. It is now {prettyValue}.");
            }

            // It seems to be fine with doing it every frame, so we'll do that
            if (ToggledOn && AllGrass != null) {
                // Can't use the game object's .scene.name because the hero
                // (the main object we want to attach this to) and any other
                // object set to not get destroyed on load have a garbage
                // scene name.
                string sceneName = GameManager.instance?.sceneName;
                if (sceneName != null) {
                    GrassKey? nearestGrass = AllGrass.GetNearestUncutGrass(
                        gameObject.transform.position,
                        sceneName);
                    Vector2? newTarget = nearestGrass?.Position;

                    // Very handy debug message in all sorts of situations
                    if (newTarget != Target) {
                        GrassPls.Instance.LogDebug(
                            $"Nearest uncut grass is now '{nearestGrass}'");
                    }

                    Target = newTarget;
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
                ToggledOn && Target != null;
        }
    }
}
