using System;
using UnityEngine;
using UnityEngine.UI;

namespace GrassyKnight
{
    public class ModMain : Modding.Mod {
        // Stores which grass is cut and allows queries (like "where's the
        // nearest uncut grass?")
        GrassDB GrassStates = new GrassDB();

        // Knows if an object is grass. Very wise. Uwu.
        GrassKnower SetOfAllGrass;

        StatusBar Status;

        public override string GetVersion() => "0.1.0";

        public ModMain() : base("Grassy Knight") {
            //
        }

        public override void Initialize() {
            base.Initialize();

            Status = new StatusBar();

            // TODO: Check the global settings to know which grass knower to
            // use.
            SetOfAllGrass = new HeuristicGrassKnower();

            // Triggered when real grass is being cut for real
            On.GrassCut.ShouldCut += HandleShouldCut;

            // Backup we use to make sure we notice uncuttable grass getting
            // swung at. This is the detector of shameful grass.
            Modding.ModHooks.Instance.SlashHitHook += HandleSlashHit;

            // Ensure the status text stays updated (UpdateStatus also takes
            // care of visibility on the main menu vs in-game)
            GrassStates.OnStatsChanged += (_, _1) => UpdateStatus();
            UnityEngine.SceneManagement.SceneManager.sceneLoaded +=
                (scene, _) => UpdateStatus(scene.name);
        }

        private void UpdateStatus(string sceneName = null) {
            if (sceneName == null) {
                sceneName = GameManager.instance.sceneName;
            }
            
            if (sceneName != null &&
                    sceneName != "" &&
                    !sceneName.ToLower().Contains("menu")) {
                Status.Update(
                    GrassStates.GetStatsForScene(sceneName),
                    GrassStates.GetGlobalStats());
                Status.Visible = true;
            } else {
                Status.Visible = false;
            }
        }


        private void LogException(string heading, System.Exception error) {
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
            // cut. We'll pass this value through no matter what.
            bool shouldCut = orig(collision);

            try {
                if (shouldCut) {
                    // Hackily figure out which grass the game is asking about
                    // by finding ourselves in the list of objects that
                    // collision is colliding with. This might also find other
                    // grass but that's fine.
                    int numFound = collision.GetContacts(_OnShouldCutColliders);
                    for (int i = 0; i < numFound && i < _OnShouldCutColliders.Length; ++i) {
                        GameObject maybeGrass = _OnShouldCutColliders[i].gameObject;

                        if (SetOfAllGrass.IsGrass(maybeGrass)) {
                            GrassStates.TrySet(
                                GrassKey.FromGameObject(maybeGrass),
                                GrassState.Cut);
                        }
                    }
                }
            } catch (System.Exception e) {
                LogException("Error in OnShouldCutGrass", e);
            }

            return shouldCut;
        }

        private void HandleSlashHit(Collider2D otherCollider, GameObject _) {
            try {
                GameObject maybeGrass = otherCollider.gameObject;
                if (SetOfAllGrass.IsGrass(maybeGrass)) {
                    GrassStates.TrySet(
                        GrassKey.FromGameObject(maybeGrass),
                        GrassState.ShouldBeCut);
                }
            } catch(System.Exception e) {
                LogException("Error in OnSlashHit", e);
            }
        }
    }
}
