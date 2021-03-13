using System;
using System.Collections.Generic;
using UnityEngine;


namespace GrassyKnight
{
    // Attach to the Geo Count object (referenced by HeroController.geoCounter)
    // to get a fancy grass counter!
    class GrassCount : MonoBehaviour {
        private class RowLayoutObject {
            // The object will be considered to be at least this wide when
            // laying out items, even if its actual bounds are smaller.
            public float MinWidth = 0;

            // If the object's layout width needs to be increased, it will
            // increased in steps of this. So if the real width was 10, the min
            // width was 9, and the step size was 3, the computed width would
            // be 12.
            private float _widthStepSize = 1;
            public float WidthStepSize {
                get => _widthStepSize;
                set {
                    if (value < 0) {
                        throw new ArgumentOutOfRangeException(
                            $"WidthStepSize must not be negative, got {value}");
                    }

                    _widthStepSize = value;
                }
            }

            // This will be added to the real width of object as the first step
            // of calculating the computed width.
            public float PaddingRight = 0;

            public GameObject GameObject_ = null;

            public float GetRealWidth() {
                Renderer renderer = GameObject_?.GetComponent<Renderer>();
                if (renderer == null) {
                    throw new InvalidOperationException(
                        "GameObject_ must be non-null and have a renderer.");
                }

                Transform parentTransform = GameObject_.transform.parent;
                if (parentTransform == null) {
                    return renderer.bounds.size.x;
                } else {
                    Vector3 localSize =
                        parentTransform.InverseTransformVector(
                            renderer.bounds.size);
                    return localSize.x;
                }
            }

            public float GetComputedWidth() {
                float realWidth = GetRealWidth();
                float paddedRealWidth = realWidth + PaddingRight;
                float unroundedComputedWidth = Mathf.Max(
                    paddedRealWidth, MinWidth);
                if (unroundedComputedWidth <= MinWidth) {
                    return MinWidth;
                } else if (WidthStepSize <= 0) {
                    return unroundedComputedWidth;
                } else {
                    return MinWidth + WidthStepSize * Mathf.Ceil(
                        (unroundedComputedWidth - MinWidth) / WidthStepSize);
                }
            }
        }

        // The first object is the "anchor", it will not be moved but its
        // computed width will be used.
        private List<RowLayoutObject> _layout = new List<RowLayoutObject>();

        // The normal size of the geo count is rather large, such that adding
        // a bunch of grass stats next to it is overwhelmingly large. So we
        // scale it down by this factor.
        public float Scale = 0.6f;

        private GameObject _roomCount = null;
        private GameObject _globalCount = null;

        public void Start() {
            try {
                _Start();
            } catch (System.Exception e) {
                GrassyKnight.Instance.LogException(
                    "Error in GrassCount.Start()", e);
            }
        }

        private void _Start() {
            _layout.Add(new RowLayoutObject {
                MinWidth = 1.4f, // A bit wider than 3 digits
                WidthStepSize = 0.5f, // Roughly 1 digit
                PaddingRight = 0.7f,
                GameObject_ = GetGeoTextObject(),
            });
            _layout.Add(new RowLayoutObject {
                MinWidth = 0,
                WidthStepSize = 0,
                PaddingRight = 0.3f,
                GameObject_ = CreateSpriteObject(
                    "Grass Sprite", "grass-icon.png"),
            });
            _roomCount = CreateTextObject("Room Grass Count");
            _layout.Add(new RowLayoutObject {
                MinWidth = 1.4f,
                WidthStepSize = 0.5f,
                PaddingRight = 0.7f,
                GameObject_ = _roomCount,
            });
            _layout.Add(new RowLayoutObject {
                MinWidth = 0,
                WidthStepSize = 0,
                PaddingRight = 0.3f,
                GameObject_ = CreateSpriteObject(
                    "Grass Sprite", "global-grass-icon.png"),
            });
            _globalCount = CreateTextObject("Global Grass Count");
            _layout.Add(new RowLayoutObject {
                MinWidth = 0,
                WidthStepSize = 0,
                PaddingRight = 0,
                GameObject_ = _globalCount,
            });
        }

        private GameObject CreateTextObject(string name) {
            GameObject result = new GameObject(
                name, typeof(TextMesh), typeof(MeshRenderer));
            result.layer = gameObject.layer;
            UnityEngine.Object.DontDestroyOnLoad(result);

            GameObject geoTextObject = GetGeoTextObject();

            TextMesh geoTextMesh = geoTextObject.GetComponent<TextMesh>();
            TextMesh textMesh = result.GetComponent<TextMesh>();
            textMesh.alignment = TextAlignment.Left;
            textMesh.anchor = TextAnchor.MiddleLeft;
            textMesh.font = geoTextMesh.font;
            textMesh.fontSize = geoTextMesh.fontSize;
            textMesh.text = geoTextMesh.text;

            MeshRenderer meshRenderer = result.GetComponent<MeshRenderer>();
            meshRenderer.material = textMesh.font.material;
            meshRenderer.enabled = false;

            result.transform.parent = gameObject.transform;
            result.transform.localScale = geoTextObject.transform.localScale;
            result.transform.localPosition = geoTextObject.transform.localPosition;

            return result;
        }

        private Texture2D LoadPNG(string name) {
            System.IO.Stream png = 
                System.Reflection.Assembly
                    .GetExecutingAssembly()
                    .GetManifestResourceStream(name);
            try {
                byte[] buffer = new byte[png.Length];
                png.Read(buffer, 0, buffer.Length);

                Texture2D texture = new Texture2D(1, 1);
                texture.LoadImage(buffer, true);
                return texture;
            } finally {
                png.Dispose();
            }
        }

        private GameObject CreateSpriteObject(string name, string pngName) {
            GameObject result = new GameObject(name, typeof(SpriteRenderer));
            result.layer = gameObject.layer;
            UnityEngine.Object.DontDestroyOnLoad(result);

            Texture2D texture = LoadPNG(pngName);
            Sprite sprite = Sprite.Create(
                texture,
                new Rect(0, 0, texture.width, texture.height),
                new Vector2(0, 0.5f), // (0.5, 0.5) is the center of the sprite
                // Make the sprite 1 world unit tall
                texture.height);

            SpriteRenderer renderer = result.GetComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sortingOrder = 32767;
            renderer.enabled = false;

            GameObject geoTextObject = GetGeoTextObject();
            result.transform.parent = gameObject.transform;
            result.transform.localScale = geoTextObject.transform.localScale;
            result.transform.localPosition = geoTextObject.transform.localPosition;

            // Adjust the height to match the existing geo sprite object
            Bounds geoSpriteBounds =
                GetSpriteObject().GetComponent<Renderer>().bounds;
            float targetHeight = geoSpriteBounds.size.y;
            float currentHeight = renderer.bounds.size.y;
            result.transform.localScale *= targetHeight / currentHeight;

            // Adjust the bottom of the sprite so that it aligns with the
            // bottom of the existing geo sprite
            float targetBottom = geoSpriteBounds.min.y;
            float currentBottom = renderer.bounds.min.y;
            result.transform.localPosition +=
                Vector3.up * (targetBottom - currentBottom);

            return result;
        }

        public void Destroy() {
            try {
                UnityEngine.Object.Destroy(_roomCount);
            } catch (System.Exception e) {
                GrassyKnight.Instance.LogException(
                    "Error in GrassyCompass.Destroy()", e);
            }
        }

        void Update() {
            try {
                _Update();
            } catch (System.Exception e) {
                GrassyKnight.Instance.LogException(
                    "Error in GrassCount.Update()", e);
            }
        }

        void _Update() {
            MaybeResize();
            ReflowLayout();
        }

        static Vector3 WithX(Vector3 v, float newX) {
            return new Vector3(newX, v.y, v.z);
        }

        void ReflowLayout() {
            if (_layout.Count <= 0) {
                return;
            }

            // The first component (the anchor) isn't created by us, and its
            // position is the center of itself (I think), so we use the
            // Renderer's bounds to get its leftmost edge.
            Transform anchorParentTransform =
                _layout[0].GameObject_.transform.parent;
            float anchorLeft = anchorParentTransform.InverseTransformPoint(
                _layout[0].GameObject_.GetComponent<Renderer>().bounds.min).x;

            float currentX = anchorLeft + _layout[0].GetComputedWidth();
            for (int i = 1; i < _layout.Count; ++i) {
                Transform transform = _layout[i].GameObject_.transform;
                transform.localPosition = WithX(transform.localPosition,
                                                currentX);
                currentX += _layout[i].GetComputedWidth();
                _layout[i].GameObject_.GetComponent<Renderer>().enabled = true;
            }
        }

        // Makes the counter smaller
        void MaybeResize() {
            if (gameObject.transform.localScale.x == Scale && 
                    gameObject.transform.localScale.y == Scale &&
                    gameObject.transform.localScale.z == Scale) {
                // Nothing to do!
                return;
            }

            // Fix the top left of the Geo Sprite in place as we shrink it
            GameObject child = GetSpriteObject();
            Vector2 startPosition = TopLeftOfBounds(
                child.GetComponent<Renderer>().bounds);
            gameObject.transform.localScale = Vector3.one * Scale;
            gameObject.transform.position -= (Vector3)(
                TopLeftOfBounds(child.GetComponent<Renderer>().bounds) -
                startPosition);
        }

        Vector2 TopLeftOfBounds(Bounds bounds) {
            return new Vector2(bounds.min.x, bounds.max.y);
        }

        GameObject GetGeoTextObject() {
            GameObject result = gameObject.transform.Find("Geo Text").gameObject;
            if (result == null) {
                throw new InvalidOperationException("Cannot find Geo Text.");
            }

            return result;
        }

        GameObject GetSpriteObject() {
            GameObject result = gameObject.transform.Find("Geo Sprite").gameObject;
            if (result == null) {
                throw new InvalidOperationException("Cannot find Geo Sprite.");
            }

            return result;
        }

        private string PrettyStats(GrassStats stats)
        {
            int struck = stats[GrassState.Cut] + stats[GrassState.ShouldBeCut];
            string result =  $"{struck}|{stats.Total()}";
            return result;
        }

        public void UpdateStats(GrassStats scene, GrassStats global) {
            if (_roomCount == null || _globalCount == null) {
                return;
            }

            _roomCount.GetComponent<TextMesh>().text = PrettyStats(scene);
            _globalCount.GetComponent<TextMesh>().text = PrettyStats(global);
        }
    }
}
