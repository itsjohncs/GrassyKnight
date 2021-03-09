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
            public bool AutomaticallyCutGrass = false;
            public string ToggleCompassHotkey = "Space";
            public string StatusBarMode = "under-soul";
            public bool DisableCompass = false;
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

        public override string GetVersion() => "1.2.0";

        public GrassyKnight() : base("Grassy Knight") {
            GrassyKnight.Instance = this;
        }

        public override void Initialize() {
            base.Initialize();

            // I tried creating this in the field initializer but it failed...
            // I think construction is too early to make game objects, though I
            // don't now why.
            UtilityBehaviour = Behaviour.CreateBehaviour();

            if (Settings.StatusBarMode == "top-middle") {
                Status = new TopMiddleStatusBar(false);
                Log("Using TopMiddleStatusBar without shameful grass count");
            } else if (Settings.StatusBarMode == "top-middle-with-shame") {
                Status = new TopMiddleStatusBar(true);
                Log("Using TopMiddleStatusBar with shameful grass count");
            } else {
                if (Settings.StatusBarMode != "under-soul") {
                    LogError($"Unrecognized StatusBarMode ${Settings.StatusBarMode}");
                }

                Status = new UnderSoulStatusBar();
                Log("Using UnderSoulStatusBar");
            }

            if (Settings.UseHeuristicGrassKnower) {
                SetOfAllGrass = new HeuristicGrassKnower();
                Log("Using HeuristicGrassKnower");

                // Because the heuristic grass knower doesn't know about grass
                // until it sees it for the first time, we need to constantly
                // look for grass each time we enter a scene.
                UnityEngine.SceneManagement.SceneManager.sceneLoaded +=
                    (_, _1) => UtilityBehaviour.StartCoroutine(
                        WaitThenFindGrass());


            } else {
                CuratedGrassKnower curatedKnower = new CuratedGrassKnower();
                SetOfAllGrass = curatedKnower;
                Log($"Using CuratedGrassKnower");

                Modding.ModHooks.Instance.SavegameLoadHook +=
                    _ => HandleFileEntered();
                Modding.ModHooks.Instance.NewGameHook +=
                    () => HandleFileEntered();

                foreach ((GrassKey, GrassKey) alias in curatedKnower.GetAliases()) {
                    GrassStates.AddAlias(alias.Item1, alias.Item2);
                }
            }

            // Triggered when real grass is being cut for real
            On.GrassCut.ShouldCut += HandleShouldCut;

            // Lots of various callbacks all doing the same thing: making sure
            // our grassy box is full when HandleShouldCut is called.
            On.GrassBehaviour.OnTriggerEnter2D += HandleGrassCollisionEnter;
            On.GrassCut.OnTriggerEnter2D += HandleGrassCollisionEnter;
            On.TownGrass.OnTriggerEnter2D += HandleGrassCollisionEnter;
            On.GrassSpriteBehaviour.OnTriggerEnter2D += HandleGrassCollisionEnter;

            // Backup we use to make sure we notice uncuttable grass getting
            // swung at. This is the detector of shameful grass.
            Modding.ModHooks.Instance.SlashHitHook += HandleSlashHit;

            // Update the stats in the status bar whenever we change scenes or
            // if they change.
            GrassStates.OnStatsChanged += (_, _1) => UpdateStatus();
            UnityEngine.SceneManagement.SceneManager.sceneLoaded +=
                (_, _1) => UtilityBehaviour.StartCoroutine(
                    WaitThenUpdateStatus());

            // Hides/shows the status bar depending on UI state
            UtilityBehaviour.OnUpdate += HandleCheckStatusBarVisibility;

            // Make sure the hero always has the grassy compass component
            // attached. We could probably hook the hero object's creation to
            // be more efficient, but it's a cheap operation so imma not worry
            // about it.
            if (!Settings.DisableCompass) {
                Modding.ModHooks.Instance.HeroUpdateHook +=
                    HandleCheckGrassyCompass;
            }

            // It's dangerous out there, make sure to bring your lawnmower!
            // This'll make sure the hero has their lawnmower handy at all
            // times.
            if (Settings.AutomaticallyCutGrass) {
                Modding.ModHooks.Instance.HeroUpdateHook +=
                    HandleCheckAutoMower;
            }
        }

        // Triggered anytime the user loads a save file or starts a new game
        private void HandleFileEntered() {
            try {
                CuratedGrassKnower knower = (CuratedGrassKnower)SetOfAllGrass;
                foreach (GrassKey k in knower.GetAllGrassKeys()) {
                    GrassStates.TrySet(k, GrassState.Uncut);
                }
            } catch (System.Exception e) {
                LogException("Error in HandleFileEntered", e);
            }
        }

        // We'll hook this into a bunch of Grass components' OnTriggerEnter2D
        // methods. It's only responsibility is to store the game object that
        // the component is attached to for a moment in case ShouldCut is
        // called in the original function.
        private void HandleGrassCollisionEnter<OrigFunc, Component>(
            OrigFunc orig,
            Component self,
            Collider2D collision)
        where Component : MonoBehaviour
        where OrigFunc : MulticastDelegate
        {
            var context = new GrassyBox(self.gameObject);
            try {
                orig.DynamicInvoke(new object[] { self, collision });
            } finally {
                context.Dispose();
            }
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
                if (hero != null &&
                        hero.GetComponent<GrassyCompass>() == null) {
                    GrassyCompass compassComponent =
                        hero.AddComponent<GrassyCompass>();
                    compassComponent.AllGrass = GrassStates;

                    if (Settings.ToggleCompassHotkey != null) {
                        try {
                            KeyCode hotkey = (KeyCode)Enum.Parse(
                                typeof(KeyCode),
                                Settings.ToggleCompassHotkey);
                            compassComponent.ToggleHotkey = hotkey;
                            Log($"Hotkey for toggling the Grassy Compass " +
                                $"set to {hotkey}");
                        } catch (ArgumentException) {
                            LogError(
                                $"Unrecognized key name for " +
                                $"ToggleCompassHotkey " +
                                $"{Settings.ToggleCompassHotkey}. See the " +
                                $"README.md file for a list of all valid " +
                                $"key names.");
                        }
                    }
                }
            } catch (System.Exception e) {
                LogException("Error in HandleCheckGrassyCompass", e);
            }
        }

        private void HandleCheckAutoMower() {
            try {
                // Ensure the hero has their lawnmower
                GameObject hero = GameManager.instance?.hero_ctrl?.gameObject;
                if (hero != null && hero.GetComponent<AutoMower>() == null) {
                    AutoMower autoMower = hero.AddComponent<AutoMower>();
                    autoMower.SetOfAllGrass = SetOfAllGrass;
                    autoMower.GrassStates = GrassStates;
                    Log("Attached autoMower to hero");
                }
            } catch (System.Exception e) {
                LogException("Error in HandleCheckAutoMower", e);
            }
        }

        // Sets state of maybeGrass if it is grass
        private void MaybeSetGrassState(GameObject maybeGrass, GrassState state) {
            GrassKey k = GrassKey.FromGameObject(maybeGrass);
            if (GrassStates.Contains(k) || SetOfAllGrass.IsGrass(maybeGrass)) {
                GrassStates.TrySet(k, state);
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
                         UnityEngine.Object.FindObjectsOfType<GameObject>()) {
                    MaybeSetGrassState(maybeGrass, GrassState.Uncut);
                }
            } catch (System.Exception e) {
                LogException("Error in WaitThenFindGrass", e);
            }
        }

        // Meant to be called when a new scene is entered
        private IEnumerator WaitThenUpdateStatus() {
            // The docs suggest wait a moment to make sure everything's set
            yield return new WaitForSeconds(0.1f);

            UpdateStatus();
        }

        private void UpdateStatus() {
            try {
                string sceneName = GameManager.instance?.sceneName;
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

        private static string IndentString(string str, string indent = "... ") {
            return indent + str.Replace("\n", "\n" + indent);
        }

        public void LogException(string heading, System.Exception error) {
            LogError($"{heading}\n{IndentString(error.ToString())}");
        }

        private bool HandleShouldCut(On.GrassCut.orig_ShouldCut orig, Collider2D collision) {
            // Find out whether the original game code thinks this should be
            // cut. We'll pass this value through no matter what.
            bool shouldCut = orig(collision);

            try {
                if (shouldCut) {
                    // ShouldCut is a static function so we've hooked every
                    // function that calls ShouldCut. Our hooks will store the
                    // GameObject whose component's method is calling ShouldCut
                    // in this box so that we can grab it out. This could also
                    // be done by walking the stack upwards IF C# let us
                    // examine the argument values of stack frames, but C# does
                    // not give us a good way to do that so here we are.
                    GameObject grass = GrassyBox.GetValue();
                    MaybeSetGrassState(grass, GrassState.Cut);
                }
            } catch (System.Exception e) {
                LogException("Error in HandleShouldCut", e);

                // Exception stack traces seem to terminate once we're out
                // of this assembly... It doesn't show who called ShouldCut
                // anyways. And that's exactly the information we want if we're
                // looking for more functions to hook HandleGrassCollisionEnter
                // into.
                LogDebug("More complete stack trace:");
                LogDebug(IndentString(System.Environment.StackTrace));
            }

            return shouldCut;
        }

        private void HandleSlashHit(Collider2D otherCollider, GameObject _) {
            try {
                MaybeSetGrassState(otherCollider.gameObject,
                                   GrassState.ShouldBeCut);
            } catch(System.Exception e) {
                LogException("Error in HandleSlashHit", e);
            }
        }
    }
}
