using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace ForgottenVillage.Editor {
    public static class CarpenterToolsBuilder {
        const string Root = "Assets/TheForgottenVillage/";
        [MenuItem("Forgotten Village/Replace Carpenter flying obstacle visuals with tools")]
        public static void Build() {
            var scene = EditorSceneManager.OpenScene(Root + "Scenes/CarpentersHouse.unity");
            var house = UnityEngine.Object.FindFirstObjectByType<HouseSceneRoot>();
            string before = PlanetaryDefenceBuilder.TransitionSnapshot(house);
            var run = house.GetComponentInChildren<ActiveWorkshopRun>(true);
            if (!run || !run.obstacleVisual) throw new Exception("Existing Quick Hands obstacle is missing.");
            Apply(run);
            if (before != PlanetaryDefenceBuilder.TransitionSnapshot(house)) throw new Exception("Carpenter transition changed.");
            PrefabUtility.SaveAsPrefabAsset(house.gameObject, Root + "Prefabs/CarpentersHouseContent.prefab");
            EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene); AssetDatabase.SaveAssets();
            File.WriteAllText("Verification/carpenter-tools-build.txt", "Replaced dodge warning mesh with imported hammer, hand saw and crowbar. Original obstacle transform, timing, head-pose collision rules and session/scoring code retained. Trigger-only colliders; imported Rigidbody/solid collider components removed.\n" + before);
        }
        public static void Apply(ActiveWorkshopRun run) {
            var obstacle = run.obstacleVisual;
            foreach (Transform child in obstacle.Cast<Transform>().ToArray()) UnityEngine.Object.DestroyImmediate(child.gameObject);
            var display = obstacle.GetComponent<WorkshopToolVisual>(); if (!display) display = obstacle.gameObject.AddComponent<WorkshopToolVisual>();
            display.run = run; display.tools = new Transform[3]; run.view.obstacleRenderer = null;
            display.instructionsPanel = run.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name == "Quick Hands instructions")?.gameObject;
            display.scorePanel = run.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name == "Quick Hands score HUD");
            string[] names = { "hammer", "hand_saw", "Crowbar" };
            float[] lengths = { .42f, .50f, .56f };
            var oldScale = obstacle.localScale; obstacle.localScale = Vector3.one;
            for (int i = 0; i < names.Length; i++) {
                var frame = new GameObject("Flying " + names[i]); frame.layer = 2; frame.transform.SetParent(obstacle, false); display.tools[i] = frame.transform;
                var orientation = new GameObject("Tool orientation"); orientation.layer = 2; orientation.transform.SetParent(frame.transform, false);
                var source = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/CarpenterToolSet/prefabs/" + names[i] + ".prefab");
                if (!source) throw new Exception("Imported tool is missing: " + names[i]);
                var tool = UnityEngine.Object.Instantiate(source, orientation.transform); tool.name = names[i] + " imported model";
                tool.transform.localPosition = Vector3.zero; tool.transform.localRotation = Quaternion.identity; tool.transform.localScale = Vector3.one;
                foreach (var c in tool.GetComponentsInChildren<Collider>(true)) UnityEngine.Object.DestroyImmediate(c);
                foreach (var rb in tool.GetComponentsInChildren<Rigidbody>(true)) UnityEngine.Object.DestroyImmediate(rb);
                var bounds = BoundsOf(tool);
                if (bounds.size.x > bounds.size.y && bounds.size.x > bounds.size.z) tool.transform.localRotation = Quaternion.Euler(0, 0, 90);
                else if (bounds.size.z > bounds.size.y) tool.transform.localRotation = Quaternion.Euler(90, 0, 0);
                bounds = BoundsOf(tool);
                if (bounds.size.z > bounds.size.x) tool.transform.localRotation = Quaternion.Euler(0, 90, 0) * tool.transform.localRotation;
                bounds = BoundsOf(tool); tool.transform.localScale *= lengths[i] / Mathf.Max(bounds.size.x, bounds.size.y, bounds.size.z);
                bounds = BoundsOf(tool); tool.transform.position += frame.transform.position - bounds.center;
                foreach (var renderer in tool.GetComponentsInChildren<Renderer>(true)) {
                    renderer.sharedMaterials = renderer.sharedMaterials.Select(ConvertMaterial).ToArray(); renderer.shadowCastingMode = ShadowCastingMode.Off; renderer.receiveShadows = false;
                    renderer.gameObject.layer = 2;
                }
                bounds = BoundsOf(tool);
                var collider = orientation.AddComponent<BoxCollider>(); collider.isTrigger = true;
                collider.center = orientation.transform.InverseTransformPoint(bounds.center);
                collider.size = new Vector3(bounds.size.x / Mathf.Abs(orientation.transform.lossyScale.x), bounds.size.y / Mathf.Abs(orientation.transform.lossyScale.y), bounds.size.z / Mathf.Abs(orientation.transform.lossyScale.z));
                frame.SetActive(i == 0);
            }
            display.renderers = obstacle.GetComponentsInChildren<Renderer>(true);
            obstacle.localScale = oldScale;
            EditorUtility.SetDirty(run); EditorUtility.SetDirty(run.view);
        }
        static Bounds BoundsOf(GameObject go) {
            var renderers = go.GetComponentsInChildren<Renderer>(true); if (renderers.Length == 0) throw new Exception("Tool has no mesh: " + go.name);
            var bounds = renderers[0].bounds; foreach (var renderer in renderers) bounds.Encapsulate(renderer.bounds); return bounds;
        }
        static Material ConvertMaterial(Material source) {
            if (!source) throw new Exception("Imported tool material missing.");
            string path = Root + "Materials/Flying tool " + source.name + ".mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (!mat) { mat = new Material(Shader.Find("Universal Render Pipeline/Lit")); AssetDatabase.CreateAsset(mat, path); }
            mat.SetTexture("_BaseMap", source.HasProperty("_MainTex") ? source.GetTexture("_MainTex") : source.mainTexture);
            mat.SetColor("_BaseColor", Color.white); mat.SetFloat("_Metallic", .2f); mat.SetFloat("_Smoothness", .25f); mat.enableInstancing = true;
            EditorUtility.SetDirty(mat); return mat;
        }
    }
}
