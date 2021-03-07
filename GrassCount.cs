using System;
using UnityEngine;
using UnityEngine.UI;

namespace GrassyKnight
{
    // Attach to the Geo Count object (referenced by HeroController.geoCounter)
    // to get a fancy grass counter!
    //
    // Sid's Death Count was an invaluable resource for learning how to do
    // this. They figured out how all this pieced together (or learned it from
    // someone else) and now I'm learning from them. Check out
    // github.com/Sid-003/DeathCounter
    class GrassCount : MonoBehaviour {
        // The normal size of the geo count is rather large, such that adding
        // a bunch of grass stats next to it is overwhelmingly large. So we
        // scale it down by this factor.
        public float Scale = 0.5f;

        private GameObject _count = null;

        public void Start() {
            try {
                _Start();
            } catch (System.Exception e) {
                GrassyKnight.Instance.LogException(
                    "Error in GrassyCompass.Start()", e);
            }
        }

        private void _Start() {
            _count = new GameObject(
                "Grass Count",
                typeof(TextMesh),
                typeof(MeshRenderer));

            // We'll destroy it when we're destroyed... so hopefully this keeps
            // anything else from destroying it until that happens.
            UnityEngine.Object.DontDestroyOnLoad(_count);

            // This'll make sure we use the same canvas to render as the geo
            // counter.
            GameObject hudCanvas = GameObject.Find("_GameCameras").transform.Find("HudCamera").Find("Hud Canvas").gameObject;
            _count.transform.parent = hudCanvas.transform;
            _count.transform.position = GetText().transform.position;

            TextMesh text = _count.GetComponent<TextMesh>();
            text.characterSize = GetText().GetComponent<TextMesh>().characterSize;
            text.fontSize = GetText().GetComponent<TextMesh>().fontSize;
            text.anchor = TextAnchor.MiddleCenter;
            text.text = "HELLO";
        }

        public void Destroy() {
            try {
                UnityEngine.Object.Destroy(_count);
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

            _count.GetComponent<Renderer>().sortingOrder = 32767;
            _count.GetComponent<Renderer>().enabled = true;
            _count.transform.position = GetText().transform.position;
            GrassyKnight.Instance.Log($"Position of geo counter {gameObject.transform.position}");
            GrassyKnight.Instance.Log($"Position of geo counter text {GetText().transform.position}");
        }

        // Makes the counter smaller
        void MaybeResize() {
            // if (gameObject.transform.localScale.x == Scale && 
            //         gameObject.transform.localScale.y == Scale &&
            //         gameObject.transform.localScale.z == Scale) {
            //     // Nothing to do!
            //     return;
            // }

            // // Scaling will end up moving the position of the children as well.
            // // To correct this, we look at how much one child moved after
            // // scaling, and then we move ourselves in the opposite direction
            // // to correct all of our children at once.
            // GameObject child = GetText();
            // Vector3 startPosition = child.GetComponent<Renderer>().bounds.min;
            // gameObject.transform.localScale = Vector3.one * Scale;
            // gameObject.transform.position -=
            //     child.GetComponent<Renderer>().bounds.min - startPosition;
        }

        GameObject GetText() {
            GameObject result = gameObject.transform.Find("Geo Text").gameObject;
            if (result == null) {
                // Makes the code a bit easier if I can assume GetText() is
                // never null.
                throw new InvalidOperationException("Cannot find Geo Text.");
            }

            return result;
        }
    }
}
