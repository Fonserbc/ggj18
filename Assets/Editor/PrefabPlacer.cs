using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class PrefabPlacer : EditorWindow {

    [MenuItem("Window/Prefab Placer")]
    static void ShowWindow()
    {
        EditorWindow.GetWindow(typeof(PrefabPlacer));
    }

    StoneHolder stones;
    Transform from, to;
    Transform parent;
    float rotFrom = 0f, rotTo = 0f;

    void OnGUI()
    {
        stones = (StoneHolder)EditorGUILayout.ObjectField("Stones", stones, typeof(StoneHolder), true);

        from = (Transform)EditorGUILayout.ObjectField("From", from, typeof(Transform), true);
        to = (Transform)EditorGUILayout.ObjectField("To", to, typeof(Transform), true);
        parent = (Transform)EditorGUILayout.ObjectField("Parent", parent, typeof(Transform), true);
        rotFrom = EditorGUILayout.FloatField(rotFrom);
        rotTo = EditorGUILayout.FloatField(rotTo);

        GUI.enabled = stones != null && from != null && to != null && parent != null;
        if (GUILayout.Button("Place Stones")) {
            int max = Mathf.CeilToInt(Vector3.Distance(from.position, to.position));
            for (int i = 0; i < max; ++i) {
                GameObject newStone = Instantiate(Utilities.RandomValue(stones.stones), Vector3.Lerp(from.position, to.position, i / (float)max), Quaternion.AngleAxis(Random.Range(rotFrom, rotTo), Vector3.up));
                newStone.transform.localScale = Vector3.Lerp(Vector3.one, Vector3.one * 2.5f, Random.Range(0, 1f));
                MeshRenderer mr = newStone.GetComponentInChildren<MeshRenderer>();
                mr.material = stones.stoneMat;
                newStone.transform.SetParent(parent);
            }
        }
    }
}
