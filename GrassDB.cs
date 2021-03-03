using System;
using System.Collections.Generic;
using UnityEngine;


namespace GrassyKnight
{
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

        public void Clear() {
            GrassStates = new Dictionary<string, Dictionary<GrassKey, GrassState>>();
            GlobalStats = new GrassStats();
            SceneStats = new Dictionary<string, GrassStats>();

            OnStatsChanged?.Invoke(this, EventArgs.Empty);
        }

        private const string _serializationVersion = "1";

        // Serializes the DB into a single string
        //
        // HollowKnight doesn't ship with
        // System.Runtime.Serialization.Formatters.dll so I don't think
        // it's safe to use a stdlib serializer... Thus we make our own.
        //
        // Format is simple. It's a series of strings seperated by semicolons.
        // First string is the version of the serialization formatter (in case
        // we need to change the format in a back-incompat way). Each N strings
        // after that correspond to a single GrassKey
        public string Serialize() {
            var parts = new List<string>();

            parts.Add(_serializationVersion);

            foreach (Dictionary<GrassKey, GrassState> states in GrassStates.Values) {
                foreach (KeyValuePair<GrassKey, GrassState> kv in states) {
                    parts.AddRange(kv.Key.Serialize());
                    parts.Add(((int)kv.Value).ToString());
                }
            }

            return String.Join(";", parts.ToArray());
        }

        // Adds all the data in serialized. Will not call Clear() first so you
        // may want to... NOTE: will invoke OnStatsChanged a bunch 🤷‍♀️
        public void AddSerializedData(string serialized) {
            if (serialized == null || serialized == "") {
                return;
            }

            string[] parts = serialized.Split(';');

            if (parts[0] != _serializationVersion) {
                throw new ArgumentException(
                    $"Unknown serialization version {parts[0]}. You may " +
                    $"a new version of the mod to load this save file.");
            } else if ((parts.Length - 1) % (GrassKey.NumSerializationTokens + 1) != 0) {
                throw new ArgumentException("GrassDB in save data is corrupt");
            }

            string[] grassKeyParts = new string[GrassKey.NumSerializationTokens];
            for (int i = 1; i < parts.Length; i += GrassKey.NumSerializationTokens + 1) {
                // Copy just the parts for a single grass key into
                // grassKeyParts.
                Array.Copy(
                    parts, i,
                    grassKeyParts, 0,
                    GrassKey.NumSerializationTokens);
                GrassKey k = GrassKey.Deserialize(grassKeyParts);

                // Conver the one GrassState part into a GrassState
                GrassState state = (GrassState)int.Parse(
                    parts[i + GrassKey.NumSerializationTokens]);

                TrySet(k, state);
            }
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
