using System.Collections.Generic;
using UnityEngine;

namespace GrassPls
{
    class AutoMower : MonoBehaviour {
        // The last time we searched for grass
        private float _lastSearchedAt = float.NegativeInfinity;

        // The number of seconds to wait between searches
        public float SearchInterval = 0.5f;

        // The grass knower that will tell us what to try and cut. Must be set
        // to a non-null value or the AutoMower won't do anything.
        public GrassKnower SetOfAllGrass = null;

        // The GrassDB that will tell us what's been cut already. Must be set
        // to a non-null value or the AutoMower won't do anything.
        public GrassDB GrassStates = null;

        public void Update() {
            try {
                _Update();
            } catch (System.Exception e) {
                GrassPls.Instance.LogException(
                    "Error in AutoMower.Update()", e);
            }
        }

        private void _Update() {
            if (SetOfAllGrass == null || GrassStates == null) {
                return;
            }

            if (_lastSearchedAt + SearchInterval <= Time.time) {
                foreach (GameObject grass in GetGrassOnScreen()) {
                    if (!SlashGrass(grass)) {
                        GrassPls.Instance.LogError(
                            $"Failed to slash on screen grass.");
                        break;
                    }
                }

                _lastSearchedAt = Time.time;
            }
        }

        private static bool IsPointOnScreenWithPadding(Vector2 point, float padding = 0.1f) {
            Vector2 viewLocalPoint =
                UnityEngine.Camera.main.WorldToViewportPoint(point);
            return
                viewLocalPoint.x >= padding &&
                viewLocalPoint.x <= 1 - padding &&
                viewLocalPoint.y >= padding &&
                viewLocalPoint.y <= 1 - padding;
        }

        private List<GameObject> GetGrassOnScreen() {
            List<GameObject> result = new List<GameObject>();

            foreach (GameObject maybeGrass in
                     UnityEngine.Object.FindObjectsOfType<GameObject>())
            {
                GrassKey k = GrassKey.FromGameObject(maybeGrass);
                GrassState? state = GrassStates.TryGet(k);
                bool isUncut = state == GrassState.Uncut || state == null;
                if (isUncut &&
                        IsPointOnScreenWithPadding(maybeGrass.transform.position) &&
                        SetOfAllGrass.IsGrass(maybeGrass)) {
                    result.Add(maybeGrass);
                }
            }

            return result;
        }

        private bool SlashGrass(GameObject grass) {
            HeroController heroController = GameManager.instance?.hero_ctrl;
            if (heroController == null) {
                return false;
            }

            GameObject slashPrefab = heroController.slashPrefab;
            if (slashPrefab == null) {
                return false;
            }

            // We expect to be attached to the hero, so we could also just use
            // our gameObject here, but eh. Feels a bit better stylistically to
            // not assume what we're attached to 🤷‍♀️.
            Transform attacksParent =
                heroController.gameObject?.transform.Find("Attacks");
            if (attacksParent == null) {
                return false;
            }

            GameObject slash = Instantiate(slashPrefab, attacksParent);

            slash.transform.position = grass.transform.position;

            slash.GetComponent<NailSlash>().StartSlash();

            // I'm not positive, but I think the slashes just leak if we don't
            // do this...
            tk2dSpriteAnimator animator =
                slash.GetComponent<tk2dSpriteAnimator>();
            float lifetime =
                animator.DefaultClip.frames.Length / animator.ClipFps;
            Destroy(slash, lifetime);

            return true;
        }
    }
}
