using UnityEngine;
using UnityEditor;
using System.Collections;

public class MyTools : MonoBehaviour {
    [MenuItem("MyTools/CreateGameObjects")]
    static void Create() {
        GameObject template = GameObject.FindGameObjectWithTag("legoExample");
        Transform parent = GameObject.FindGameObjectWithTag("gridHolder").transform;
        Selection.activeGameObject.GetComponent<KMSelectable>().Children = new KMSelectable[64];
        for (int y = 0; y < 8; y++) {
            for (int x = 0; x < 8; x++) {
                GameObject go = Instantiate(template);
                go.transform.SetParent(parent);
                go.transform.localPosition = new Vector3(14.22f / 1000f * x, 0, 14.22f / 1000f * y);
                go.name = "legoGrid" + (8 * y + x);
                go.transform.GetChild(0).gameObject.name = "legoGrid" + (8 * y + x) + "highlight";
                Selection.activeGameObject.GetComponent<KMSelectable>().Children[8 * y + x] = go.GetComponent<KMSelectable>();
            }
        }
    }

    [MenuItem("MyTools/PopulateKMSelectableChildren")]
    static void Populate() {
        GameObject template = GameObject.FindGameObjectWithTag("legoExample");
        Transform parent = GameObject.FindGameObjectWithTag("gridHolder").transform;
        for (int y = 0; y < 8; y++) {
            for (int x = 0; x < 8; x++) {
                GameObject go = Instantiate(template);
                go.transform.SetParent(parent);
                go.transform.localPosition = new Vector3(14.22f / 1000f * x, 0, 14.22f / 1000f * y);
                go.name = "legoGrid" + (8 * y + x);
                go.transform.GetChild(0).gameObject.name = "legoGrid" + (8 * y + x) + "highlight";
            }
        }
    }
}