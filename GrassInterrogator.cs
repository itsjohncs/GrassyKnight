using System;
using System.Collections.Generic;
using UnityEngine;

namespace GrassyKnight
{
    // Are you grass? ARE YOU?
    class GrassInterrogator {
        // The game object we'll use to poke at grass.
        static private GameObject _probePrefab = null;

        // Path to the prefab we want to use as a probe. Needs to be returned
        // in the mod's GetPreloadNames.
        public static (string, string) ProbePrefabPath {
            get => ("Crossroads_ShamanTemple", "_Enemies/Buzzer");
        }

        static public void ReceiveProbePrefab(GameObject probePrefab) {
            if (_probePrefab != null) {
                throw new InvalidOperationException(
                    $"Prefab with name '{_probePrefab.name}' already saved");
            }

            // Make a copy of the prefab that'll stick around for us to clone
            // more in the future.
            GameObject clone = UnityEngine.Object.Instantiate(probePrefab);
            UnityEngine.Object.DontDestroyOnLoad(clone);
            clone.SetActive(false);

            _probePrefab = clone;
        }

        public Dictionary<GrassKey, List<int>> SusGrass =
            new Dictionary<GrassKey, List<int>>();

        public void LogResult(GrassKey k, int numHits) {
            if (SusGrass.TryGetValue(k, out List<int> lst)) {
                lst.Add(numHits);
            } else {
                SusGrass.Add(k, new List<int> { numHits });
            }
        }

        // This is meant to be attached to a Slash object
        public class GrassProbe : MonoBehaviour {
            // Any grass hit will be added to this set by our ShouldCutGrass
            // handler up topside
            public HashSet<GrassKey> GrassHit = new HashSet<GrassKey>();
            public GrassInterrogator ReportTo = null;
            public GrassKey? ProbeFor = null;

            // This is called once upon destruction
            void OnDisable() {
                if (ReportTo != null && ProbeFor != null) {
                    ReportTo.LogResult(ProbeFor.Value, GrassHit.Count);
                }
            }

            void Update() {
                // Destroy the object after just a frame
                UnityEngine.Object.Destroy(gameObject);
            }
        }

        public void ProbeSuspectGrass(GameObject gameObject) {
            Collider2D collider = gameObject.GetComponent<Collider2D>();
            if (collider == null) {
                return;
            }

            GameObject probeObject = UnityEngine.Object.Instantiate(
                _probePrefab);

            GrassProbe probeComponent = probeObject.AddComponent<GrassProbe>();
            probeComponent.ReportTo = this;
            probeComponent.ProbeFor = GrassKey.FromGameObject(gameObject);

            // Place the slash in a random spot within the object
            Bounds bounds = collider.bounds;
            probeObject.transform.position = new Vector2(
                UnityEngine.Random.Range(bounds.min.x, bounds.max.x),
                UnityEngine.Random.Range(bounds.min.y, bounds.max.y));

            // We use smol probes to reduce the chance of collision with
            // multiple grass at once.
            probeObject.transform.localScale *= 0.1f;

            probeObject.SetActive(true);
        }
    }
}
