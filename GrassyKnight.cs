﻿using System;
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

        // An object that gives us access to unity's coroutine scheduler
        Behaviour CoroutineHelper = null;

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
            CoroutineHelper = Behaviour.CreateBehaviour();

            // TODO: Check the global settings to know which grass knower to
            // use. Bool is here just to help me prepare for future.
            bool useHeuristic = true;
            if (useHeuristic) {
                SetOfAllGrass = new HeuristicGrassKnower();
                UnityEngine.SceneManagement.SceneManager.sceneLoaded +=
                    (_, _1) => CoroutineHelper.StartCoroutine(
                        WaitThenFindGrass());
            }

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

            // Make sure our hero has their friendly grass compass
            Modding.ModHooks.Instance.HeroUpdateHook += HandleHeroUpdate;
        }

        private void HandleHeroUpdate() {
            GameObject hero = GameManager.instance?.hero_ctrl?.gameObject;
            if (hero != null) {
                GrassyCompass compass = hero.GetComponent<GrassyCompass>();
                if (compass == null) {
                    hero.AddComponent<GrassyCompass>().AllGrass = GrassStates;
                }
            }
        }

        // Only used if we're using the HeuristicGrassKnower, meant to be
        // called when a new scene is entered.
        private IEnumerator WaitThenFindGrass() {
            // The docs suggest waiting a frame after scene loads before we
            // consider the scene fully instantiated. We've got time, so wait
            // a whole second.
            yield return new WaitForSeconds(1);

            try {
                foreach (GameObject maybeGrass in
                         UnityEngine.Object.FindObjectsOfType<GameObject>())
                {
                    if (SetOfAllGrass.IsGrass(maybeGrass)) {
                        GrassStates.TrySet(
                            GrassKey.FromGameObject(maybeGrass),
                            GrassState.Uncut);
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
                
                bool shouldStatusBeVisible = (
                    UIManager.instance.uiState == GlobalEnums.UIState.PLAYING ||
                    UIManager.instance.uiState == GlobalEnums.UIState.PAUSED);
                if (sceneName != null && shouldStatusBeVisible) {
                    Status.Update(
                        GrassStates.GetStatsForScene(sceneName),
                        GrassStates.GetGlobalStats());
                    Status.Visible = true;
                } else {
                    Status.Visible = false;
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
                LogException("Error in HandleShouldCut", e);
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
                LogException("Error in HandleSlashHit", e);
            }
        }
    }
}