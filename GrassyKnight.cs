using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace GrassyKnight
{
    public class GrassyKnight : Modding.Mod {
        // In a previous version we accessed ModSettings.BoolValues directly,
        // but it looks like the latest code in the Modding.API repo no longer
        // has BoolValues as a member at all. This way of using ModSettings is
        // more in line with other mod authors do so we should be somewhat
        // future-proof now.
        private class MySaveData : Modding.ModSettings {
            public string serializedGrassDB;
        }

        public override Modding.ModSettings SaveSettings
        {
            get {
                return new MySaveData {
                    serializedGrassDB = GrassStates.Serialize(),
                };
            }

            set {
                GrassStates.Clear();
                GrassStates.AddSerializedData(
                    ((MySaveData)value).serializedGrassDB);
            }
        }

        private class MyGlobalSettings : Modding.ModSettings {
            public bool UseHeuristicGrassKnower = false;
        }

        private MyGlobalSettings Settings = new MyGlobalSettings();
        public override Modding.ModSettings GlobalSettings {
            get => Settings;
            set => Settings = (MyGlobalSettings)value;
        }

        // Will be set to the exactly one ModMain in existance... Trusting
        // Modding.Mod to ensure that ModMain is only ever instantiated once...
        public static GrassyKnight Instance = null;

        // Stores which grass is cut and allows queries (like "where's the
        // nearest uncut grass?")
        GrassDB GrassStates = new GrassDB();

        // Knows if an object is grass. Very wise. Uwu. Which knower we use
        // depends on configuration
        GrassKnower SetOfAllGrass = null;

        // The status bar that shows the player the number of grass cut
        StatusBar Status = null;

        // Usually Unity code is contained in MonoBehaviour classes, so Unity
        // has lots of very useful functionality in them (ex: access to the
        // coroutine scheduler). This is forever-living MonoBehaviour object we
        // use to give us that funcionality despite our non-MonoBheaviour
        // status.
        Behaviour UtilityBehaviour = null;

        public override string GetVersion() => "0.1.0";

        public GrassyKnight() : base("Grassy Knight") {
            GrassyKnight.Instance = this;
        }

        public override void Initialize() {
            base.Initialize();

            // We wait to create these until now because they all create game
            // objects. I found that game objects created in field initializers
            // are unreliable (and I assume the same is true for in the
            // constructor).
            Status = new StatusBar();
            UtilityBehaviour = Behaviour.CreateBehaviour();

            if (Settings.UseHeuristicGrassKnower) {
                SetOfAllGrass = new HeuristicGrassKnower();
                Log("Using HeuristicGrassKnower");
            } else {
                SetOfAllGrass = new CuratedGrassKnower();

                int totalGrass = ((CuratedGrassKnower)SetOfAllGrass).TotalGrass();
                Status.GlobalTotalOverride = totalGrass;

                Log($"Using CuratedGrassKnower, {totalGrass} known");
            }

            // Find all the grass in the room. Once CuratedGrassKnower has
            // a list of actual GrassKeys I'll only have to do this while using
            // the HeuristicGrassKnower.
            UnityEngine.SceneManagement.SceneManager.sceneLoaded +=
                (_, _1) => UtilityBehaviour.StartCoroutine(
                    WaitThenFindGrass());

            // Triggered when real grass is being cut for real
            On.GrassCut.ShouldCut += HandleShouldCut;

            // Backup we use to make sure we notice uncuttable grass getting
            // swung at. This is the detector of shameful grass.
            Modding.ModHooks.Instance.SlashHitHook += HandleSlashHit;

            // Update the stats in the status bar whenever we change scenes or
            // if they change.
            GrassStates.OnStatsChanged += (_, _1) => UpdateStatus();
            UnityEngine.SceneManagement.SceneManager.sceneLoaded +=
                (scene, _) => UpdateStatus(scene.name);

            // It's not what it looks like
            UnityEngine.SceneManagement.SceneManager.sceneLoaded +=
                (scene, _) => UtilityBehaviour.StartCoroutine(
                    ProbeGrassForAwhile());

            // Hides/shows the status bar depending on UI state
            UtilityBehaviour.OnUpdate += HandleCheckStatusBarVisibility;

            // Make sure the hero always has the grassy compass component
            // attached. We could probably hook the hero object's creation to
            // be more efficient, but it's a cheap operation so imma not worry
            // about it.
            Modding.ModHooks.Instance.HeroUpdateHook +=
                HandleCheckGrassyCompass;

        }

        private void HandleCheckStatusBarVisibility(object _, EventArgs _1) {
            try {
                GlobalEnums.UIState? state = UIManager.instance?.uiState;
                Status.Visible =
                    state == GlobalEnums.UIState.PLAYING ||
                    state == GlobalEnums.UIState.PAUSED;
            } catch (System.Exception e) {
                LogException("Error in HandleCheckStatusBarVisibility", e);
            }
        }

        private void HandleCheckGrassyCompass() {
            try {
                // Ensure the hero has their grassy compass friend
                GameObject hero = GameManager.instance?.hero_ctrl?.gameObject;
                if (hero != null) {
                    GrassyCompass compass = hero.GetComponent<GrassyCompass>();
                    if (compass == null) {
                        hero.AddComponent<GrassyCompass>().AllGrass = GrassStates;
                    }
                }
            } catch (System.Exception e) {
                LogException("Error in HandleCheckGrassyCompass", e);
            }
        }

        private IEnumerator ProbeGrassForAwhile() {
            yield return new WaitForSeconds(0.5f);

            GrassInterrogator interrogator = new GrassInterrogator();

            while (true) {
                try {
                    foreach (GameObject maybeGrass in
                             UnityEngine.Object.FindObjectsOfType<GameObject>())
                    {
                        GameObject hero = GameManager.instance?.hero_ctrl?.gameObject;
                        if (hero == null) {
                            continue;
                        }

                        if (Vector3.Magnitude(maybeGrass.transform.position - hero.transform.position) > 30) {
                            continue;
                        }

                        GrassKey k = GrassKey.FromGameObject(maybeGrass);
                        if (GrassStates.Contains(k) ||
                                SetOfAllGrass.IsGrass(maybeGrass)) {
                            interrogator.ProbeSuspectGrass(maybeGrass);
                        }
                    }

                    Log("Interrogator result");
                    foreach (var kv in interrogator.SusGrass) {
                        var foo = new System.Collections.Generic.List<string>();
                        foreach (int i in kv.Value) {
                            foo.Add(i.ToString());
                        }
                        Log($"... {kv.Key} -> {string.Join(", ", foo.ToArray())}");
                    }
                } catch (System.Exception e) {
                    LogException("Error in ProbeGrassForAwhile", e);
                }

                yield return new WaitForSeconds(1);
            }
        }

        // Meant to be called when a new scene is entered
        private IEnumerator WaitThenFindGrass() {
            // The docs suggest waiting a frame after scene loads before we
            // consider the scene fully instantiated. We've got time, so wait
            // even longer.
            yield return new WaitForSeconds(0.5f);

            try {
                foreach (GameObject maybeGrass in
                         UnityEngine.Object.FindObjectsOfType<GameObject>())
                {
                    GrassKey k = GrassKey.FromGameObject(maybeGrass);
                    if (GrassStates.Contains(k) ||
                            SetOfAllGrass.IsGrass(maybeGrass)) {
                        GrassStates.TrySet(k, GrassState.Uncut);
                    }
                }
            } catch (System.Exception e) {
                LogException("Error in WaitThenFindGrass", e);
            }
        }

        private void UpdateStatus(string sceneName = null) {
            try {
                if (sceneName == null) {
                    sceneName = GameManager.instance?.sceneName;
                }
                
                if (sceneName != null) {
                    Status.Update(
                        GrassStates.GetStatsForScene(sceneName),
                        GrassStates.GetGlobalStats());
                    Status.Visible = true;
                }
            } catch (System.Exception e) {
                LogException("Error in UpdateStatus", e);
            }
        }


        public void LogException(string heading, System.Exception error) {
            const string indent = "... ";
            string indentedError =
                indent + error.ToString().Replace("\n", "\n" + indent);
            LogError($"{heading}\n{indentedError}");
        }

        // If C# has local statics, this'd be scoped to OnShouldCut. This is
        // the buffer we'll receive the results of our "what's colliding with
        // the object that's colliding with us". 50 is a _way_ more than we
        // ever expect, so hopefully it's never too few.
        private Collider2D[] _OnShouldCutColliders = new Collider2D[50];

        private bool HandleShouldCut(On.GrassCut.orig_ShouldCut orig, Collider2D collision) {
            // Find out whether the original game code thinks this should be
            // cut
            bool shouldCut = orig(collision);

            try {
                if (shouldCut) {
                    // If this is a probe, we'll tell it that we hit grass
                    GrassInterrogator.GrassProbe probeComponent =
                        collision?
                        .gameObject?
                        .GetComponent<GrassInterrogator.GrassProbe>();

                    // Hackily figure out which grass the game is asking about
                    // by finding ourselves in the list of objects that
                    // collision is colliding with. This might also find other
                    // grass but that's fine.
                    int numFound = collision.GetContacts(_OnShouldCutColliders);
                    for (int i = 0; i < numFound && i < _OnShouldCutColliders.Length; ++i) {
                        GameObject maybeGrass = _OnShouldCutColliders[i].gameObject;

                        GrassKey k = GrassKey.FromGameObject(maybeGrass);
                        if (GrassStates.Contains(k) ||
                                SetOfAllGrass.IsGrass(maybeGrass)) {
                            GrassStates.TrySet(k, GrassState.Cut);

                            if (probeComponent != null) {
                                probeComponent.GrassHit.Add(k);
                            }
                        }
                    }
                }
            } catch (System.Exception e) {
                LogException("Error in HandleShouldCut", e);
            }

            return shouldCut;
        }

        private void HandleSlashHit(Collider2D otherCollider, GameObject _) {
            try {
                GameObject maybeGrass = otherCollider.gameObject;
                GrassKey k = GrassKey.FromGameObject(maybeGrass);
                if (GrassStates.Contains(k) ||
                        SetOfAllGrass.IsGrass(maybeGrass)) {
                    GrassStates.TrySet(k, GrassState.ShouldBeCut);
                }
            } catch(System.Exception e) {
                LogException("Error in HandleSlashHit", e);
            }
        }
    }
}
