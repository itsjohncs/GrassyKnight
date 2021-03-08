GameObject foo = GameManager.instance?.hero_ctrl?.geoCounter?.gameObject;
GrassyKnight.Instance.Log($"hc instance: {foo?.name} ({foo?.GetInstanceID()})");
foreach (UnityEngine.Object i in Resources.FindObjectsOfTypeAll(typeof(UnityEngine.Object)) as UnityEngine.Object[])
{
    if ((/*i.name == "Geo Amount" ||*/
            i.name == "Geo Text" || i.name == "Geo Sprite" ||
            i.name == "Geo Counter") && i is GameObject geoCount) {
        GrassyKnight.Instance.Log($"Found {geoCount.name} ({geoCount.GetInstanceID()})");
        GrassyKnight.Instance.Log("All components");
        foreach (Component component in geoCount.GetComponents<Component>()) {
            GrassyKnight.Instance.Log($"... ... {component.ToString()}");
        }
        GrassyKnight.Instance.Log("All ancestors");
        GameObject current = geoCount;
        while (current != null) {
            GrassyKnight.Instance.Log($"... ... {current.ToString()}");

            if (current.name == "Geo Counter") {
                GrassyKnight.Instance.Log($"Current localScale {current.transform.localScale}");

                float x = current.transform.localScale.x;
                if (x > 0.1f) {
                    current.transform.localScale = Vector3.one * (x - 0.1f);
                } else {
                    current.transform.localScale = Vector3.one;
                }
            }

            current = current.transform.parent?.gameObject;
        }

        GrassyKnight.Instance.Log("All descendents");
        foreach (Transform transform in geoCount.GetComponentsInChildren<Transform>()) {
            GrassyKnight.Instance.Log($"... ... {transform.gameObject}");
        }
    }
}
