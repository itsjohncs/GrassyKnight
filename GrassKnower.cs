using System;
using System.Collections.Generic;
using UnityEngine;


namespace GrassyKnight
{
    // Responsible for knowing what is grass and what is not
    abstract class GrassKnower {
        public abstract bool IsGrass(GameObject gameObject);
    }

    // Uses a heuristic algorithm to detect whether something is grass. Will
    // not always be right, but does an OK job.
    class HeuristicGrassKnower : GrassKnower {
        // All the grass in the game have some child component with grass in
        // its name. Ex: GrassBehavior or GrassSpriteRenderer.
        private static bool HasGrassyComponent(GameObject gameObject) {
            foreach (Component component in gameObject.GetComponents<Component>())
            {
                if (component.GetType().Name.ToLower().Contains("grass"))
                {
                    return true;
                }
            }

            return false;
        }

        public override bool IsGrass(GameObject gameObject) {
            return
                // If the game calls it grass, we will too
                gameObject.name.ToLower().Contains("grass") &&
                // Filter out _some_ unhittable grass (this won't catch all
                // such grass).
                gameObject.GetComponentsInChildren<Collider2D>().Length > 0 &&
                // Check if there's a grassy component. There's at least one
                // floor tile that has grass in its name that this skips for
                // us.
                HasGrassyComponent(gameObject);
        }
    }

    // TODO: when we have a grass list I'll make a class that uses it to detect
    // whether something is grass
}
