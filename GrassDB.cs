using System;
using System.Collections.Generic;
using UnityEngine;


namespace GrassyKnight
{
    [Serializable()]
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
    }

    [Serializable()]
    enum GrassState {
        Uncut,
        Cut,

        // A special state that grass might enter if it is struck with the
        // nail but not actually cut in game.
        ShouldBeCut,
    }

    class GrassStats {
        // Maps from GrassState (ex: Cut) to number of grass in that state. I'm
        // curious if there's a way to create a mutable-tuple-of-sorts with the
        // correct size (the number of enum values in GrassState)... but I
        // don't think there is.
        private int[] GrassInState;

        public GrassStats() {
            GrassInState = new int[Enum.GetNames(typeof(GrassState)).Length];
        }

        public int Total() {
            int sum = 0;
            foreach (int numGrass in GrassInState) {
                sum += numGrass;
            }
            return sum;
        }

        public int GetNumGrassInState(GrassState state) {
            return GrassInState[(int)state];
        }

        public int this[GrassState state] {
            get => GetNumGrassInState(state);
        }

        public void HandleUpdate(GrassState? oldState, GrassState newState) {
            if (oldState is GrassState oldStateValue) {
                GrassInState[(int)oldStateValue] -= 1;
            }

            GrassInState[(int)newState] += 1;
        }

        public override string ToString() {
            string result = "GrassStats(";
            foreach (GrassState state in Enum.GetValues(typeof(GrassState))) {
                result += $"{Enum.GetName(typeof(GrassState), state)}=" +
                          $"{GrassInState[(int)state]}, ";
            }
            return result + ")";
        }
    }

    // Responsible for storing the status of grass and letting us run various
    // queries against all the grass.
    class GrassDB {
        // Maps from scene name to a dictionary mapping from grass key to
        // state. The seperation of grass by scene is done only for query
        // speed, since GrassKey has the scene name in it already.
        private Dictionary<string, Dictionary<GrassKey, GrassState>> GrassStates =
            new Dictionary<string, Dictionary<GrassKey, GrassState>>();

        private GrassStats GlobalStats = new GrassStats();
        private Dictionary<string, GrassStats> SceneStats = new Dictionary<string, GrassStats>();

        public event EventHandler OnStatsChanged;

        public List<(GrassKey, GrassState)> ToList() {
            var result = new List<(GrassKey, GrassState)>();
            foreach (Dictionary<GrassKey, GrassState> states in GrassStates.Values) {
                foreach (KeyValuePair<GrassKey, GrassState> kv in states) {
                    result.Add((kv.Key, kv.Value));
                }
            }

            return result;
        }

        private void TryAddScene(string sceneName) {
            // Try add isn't available in the stdlib we're building against :(
            // (I think... honestly I'm not convinced I'm not missing an
            // reference but I sure can't find where it is).
            if (!GrassStates.ContainsKey(sceneName)) {
                GrassStates.Add(sceneName,
                                new Dictionary<GrassKey, GrassState>());
            }

            if (!SceneStats.ContainsKey(sceneName)) {
                SceneStats.Add(sceneName, new GrassStats());
            }
        }

        public bool TrySet(GrassKey k, GrassState newState) {
            TryAddScene(k.SceneName);

            GrassState? oldState = null;
            if (GrassStates[k.SceneName].TryGetValue(k, out GrassState state)) {
                oldState = state;
            }

            if (oldState == null || (int)oldState < (int)newState) {
                GrassStates[k.SceneName][k] = newState;

                SceneStats[k.SceneName].HandleUpdate(oldState, newState);
                GlobalStats.HandleUpdate(oldState, newState);
                OnStatsChanged?.Invoke(this, EventArgs.Empty);

                return true;
            } else {
                return false;
            }
        }

        public GrassKey GetNearestUncutGrass(Vector2 origin, string sceneName) {
            Dictionary<GrassKey, GrassState> grassStatesForScene;
            if (!GrassStates.TryGetValue(sceneName, out grassStatesForScene)) {
                return null;
            }

            GrassKey closest = null;
            float closestDistance = float.PositiveInfinity;
            foreach (KeyValuePair<GrassKey, GrassState> kv in grassStatesForScene) {
                if (kv.Value != GrassState.Uncut) {
                    continue;
                }

                float currentDistance = Vector2.Distance(origin, kv.Key.Position);
                if (currentDistance < closestDistance) {
                    closest = kv.Key;
                    closestDistance = currentDistance;
                }
            }

            return closest;
        }

        public GrassStats GetStatsForScene(string sceneName) {
            if (SceneStats.TryGetValue(sceneName, out GrassStats stats)) {
                return stats;
            } else {
                return new GrassStats();
            }
        }

        public GrassStats GetGlobalStats() {
            return GlobalStats;
        }
    }
}
