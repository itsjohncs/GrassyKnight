using System;
using UnityEngine;
using UnityEngine.UI;
using ModCommon;


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

        // Used to get the grass stats as needed
        public GrassDB AllGrass = null;

        private GameObject _count = null;

        public void Start() {
            try {
                _Start();
            } catch (System.Exception e) {
                GrassyKnight.Instance.LogException(
                    "Error in GrassCount.Start()", e);
            }
        }

        private void _Start() {
            var inventoryFSM = GameManager.instance.inventoryFSM;
            var geoCountPrefab = (
                inventoryFSM.gameObject.FindGameObjectInChildren("Geo"));
            GameObject hudCanvas = (
                GameObject.Find("_GameCameras")
                          .FindGameObjectInChildren("HudCamera")
                          .FindGameObjectInChildren("Hud Canvas"));

            _count = UnityEngine.Object.Instantiate(
                geoCountPrefab, hudCanvas.transform, true);
            _count.transform.localScale = Vector3.one * Scale;

            _count.GetComponent<DisplayItemAmount>().playerDataInt = "none";
            _count.GetComponent<DisplayItemAmount>().textObject.text = "...";
            // _count.GetComponent<SpriteRenderer>().sprite = sprite;
            _count.SetActive(true);
            _count.GetComponent<BoxCollider2D>().size = new Vector2(1.5f, 1f);
            _count.GetComponent<BoxCollider2D>().offset = new Vector2(0.5f, 0f);
            // _count.FindGameObjectInChildren("Geo Amount").transform.position -= new Vector3(0.3f, 0, 0);
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

            // _count.GetComponent<Renderer>().sortingOrder = 32767;
            // _count.GetComponent<Renderer>().enabled = true;
            // _count.transform.position = GetText().transform.position;
            // GrassyKnight.Instance.Log($"Position of geo counter {gameObject.transform.position}");
            // GrassyKnight.Instance.Log($"Position of geo counter text {GetText().transform.position}");
        }

        // Makes the counter smaller
        void MaybeResize() {
            if (gameObject.transform.localScale.x == Scale && 
                    gameObject.transform.localScale.y == Scale &&
                    gameObject.transform.localScale.z == Scale) {
                // Nothing to do!
                return;
            }

            // Scaling will end up moving the position of the children as well.
            // To correct this, we look at how much one child moved after
            // scaling, and then we move ourselves in the opposite direction
            // to correct all of our children at once.
            GameObject child = GetText();
            Vector3 startPosition = child.GetComponent<Renderer>().bounds.min;
            gameObject.transform.localScale = Vector3.one * Scale;
            gameObject.transform.position -=
                child.GetComponent<Renderer>().bounds.min - startPosition;
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

        private string PrettyStats(GrassStats stats)
        {
            int struck = stats[GrassState.Cut] + stats[GrassState.ShouldBeCut];
            string result =  $"{struck}/{stats.Total()}";
            return result;
        }

        public void UpdateStats(GrassStats scene, GrassStats global) {
            _count.GetComponent<DisplayItemAmount>().textObject.text =
                $"{PrettyStats(scene)} -- {PrettyStats(global)}";
        }
    }
}
