using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace ForgottenVillage.Editor {
    public static class PeasantResidents {
        const string Source = "Assets/PolyPeople_MedievalEurope_Peasants_Free/";
        const string Character = "MedievalEurope_Peasants_Male_Kid";
        const string IdleController = "Assets/TheForgottenVillage/Animations/CityPeopleMaleIdle.controller";

        static readonly (string role, string outfit, float height)[] Residents = {
            ("Elder", "A", 1.68f),
            ("Alchemist", "B", 1.72f),
            ("Carpenter", "A", 1.82f),
            ("Technician", "B", 1.76f),
            ("Clockmaker", "B", 1.70f)
        };

        [MenuItem("Forgotten Village/NPC/Update peasant visuals")]
        public static void UpdatePrefabs() {
            var controller = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(IdleController);
            if (!controller) throw new InvalidOperationException("Resident idle controller is missing.");

            foreach (var resident in Residents) {
                string sourcePath = OutfitPrefab(resident.outfit);
                var source = AssetDatabase.LoadAssetAtPath<GameObject>(sourcePath);
                if (!source) throw new InvalidOperationException("Missing peasant prefab: " + sourcePath);

                string path = "Assets/TheForgottenVillage/Prefabs/NPC/" + resident.role + ".prefab";
                var root = PrefabUtility.LoadPrefabContents(path);
                try {
                    var presentation = root.GetComponent<NpcPresentation>();
                    if (!presentation || root.transform.childCount != 1)
                        throw new InvalidOperationException("Unexpected resident prefab structure: " + path);

                    UnityEngine.Object.DestroyImmediate(root.transform.GetChild(0).gameObject);
                    var model = (GameObject)PrefabUtility.InstantiatePrefab(source, root.scene);
                    model.transform.SetParent(root.transform, false);

                    var animator = model.GetComponent<Animator>();
                    if (!animator || !animator.avatar || !animator.avatar.isValid || !animator.avatar.isHuman)
                        throw new InvalidOperationException("Invalid peasant humanoid avatar: " + sourcePath);

                    animator.runtimeAnimatorController = controller;
                    animator.applyRootMotion = false;
                    animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;

                    var renderers = model.GetComponentsInChildren<SkinnedMeshRenderer>();
                    var bounds = GetBounds(renderers);
                    if (bounds.size.y < .01f)
                        throw new InvalidOperationException("Peasant model has no usable height: " + sourcePath);
                    model.transform.localScale *= resident.height / bounds.size.y;
                    bounds = GetBounds(renderers);
                    model.transform.position -= new Vector3(bounds.center.x, bounds.min.y, bounds.center.z);

                    presentation.animator = animator;
                    presentation.head = animator.GetBoneTransform(HumanBodyBones.Head);
                    if (!presentation.head)
                        throw new InvalidOperationException("Peasant humanoid has no head bone: " + sourcePath);

                    if (!PrefabUtility.SaveAsPrefabAsset(root, path))
                        throw new InvalidOperationException("Could not save resident: " + path);
                } finally {
                    PrefabUtility.UnloadPrefabContents(root);
                }
            }
            AssetDatabase.SaveAssets();
        }

        public static bool UsesExpectedAssets(NpcPresentation presentation) {
            var animator = presentation.animator;
            var controller = animator.runtimeAnimatorController;
            var clips = controller ? controller.animationClips : null;
            string materialPath = Source + "Materials/MedievalEurope_Peasants_" + Residents[presentation.role].outfit + ".mat";
            return AssetDatabase.GetAssetPath(animator.avatar) == Source + "Meshes/" + Character + ".fbx"
                && AssetDatabase.GetAssetPath(controller) == IdleController
                && clips != null && clips.Length == 1 && clips[0].isLooping
                && animator.GetComponentsInChildren<SkinnedMeshRenderer>().SelectMany(renderer => renderer.sharedMaterials)
                    .All(material => AssetDatabase.GetAssetPath(material) == materialPath);
        }

        static string OutfitPrefab(string outfit) {
            return Source + "Prefabs/Male_Kid/" + Character + "_" + outfit + ".prefab";
        }

        static Bounds GetBounds(SkinnedMeshRenderer[] renderers) {
            if (renderers.Length == 0) throw new InvalidOperationException("Peasant model has no skinned meshes.");
            var bounds = renderers[0].bounds;
            foreach (var renderer in renderers) bounds.Encapsulate(renderer.bounds);
            return bounds;
        }
    }
}
