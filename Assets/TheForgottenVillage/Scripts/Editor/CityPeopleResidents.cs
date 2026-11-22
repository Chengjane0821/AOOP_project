using System;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace ForgottenVillage.Editor {
    public static class CityPeopleResidents {
        const string Source = "Assets/DenysAlmaral/CityPeople/";
        const string Destination = "Assets/TheForgottenVillage/";
        const string FemaleIdle = "idle_f_1_150f";
        const string MaleIdle = "idle_m_1_200f";

        static readonly (string role, string model, string idle, float height)[] Residents = {
            ("Elder", "elder/elder_Female_A", FemaleIdle, 1.68f),
            ("Alchemist", "professions/Doctor_Male_B", MaleIdle, 1.72f),
            ("Carpenter", "construction/worker_Male_constructor_B", MaleIdle, 1.82f),
            ("Technician", "downtown/casual_Female_K", FemaleIdle, 1.76f),
            ("Clockmaker", "city/casual_Male_G", MaleIdle, 1.70f)
        };

        [MenuItem("Forgotten Village/NPC/Update City People visuals")]
        public static void UpdatePrefabs() {
            var material = AssetDatabase.LoadAssetAtPath<Material>(Source + "Materials/people_pal.mat");
            if (!material) throw new InvalidOperationException("City People palette is missing.");

            var female = GetIdleController(FemaleIdle, "Female");
            var male = GetIdleController(MaleIdle, "Male");
            foreach (var resident in Residents) {
                string modelPath = Source + "Meshes/" + resident.model + ".fbx";
                var source = AssetDatabase.LoadAssetAtPath<GameObject>(modelPath);
                if (!source) throw new InvalidOperationException("Missing City People model: " + modelPath);

                string path = Destination + "Prefabs/NPC/" + resident.role + ".prefab";
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
                        throw new InvalidOperationException("Invalid City People humanoid avatar: " + modelPath);

                    animator.runtimeAnimatorController = resident.idle == FemaleIdle ? female : male;
                    animator.applyRootMotion = false;
                    animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;

                    var renderers = model.GetComponentsInChildren<SkinnedMeshRenderer>();
                    foreach (var renderer in renderers) {
                        var materials = renderer.sharedMaterials;
                        Array.Fill(materials, material);
                        renderer.sharedMaterials = materials;
                    }

                    var bounds = GetBounds(renderers);
                    if (bounds.size.y < .01f)
                        throw new InvalidOperationException("City People model has no usable height: " + modelPath);
                    model.transform.localScale *= resident.height / bounds.size.y;
                    bounds = GetBounds(renderers);
                    model.transform.position -= new Vector3(bounds.center.x, bounds.min.y, bounds.center.z);

                    presentation.animator = animator;
                    presentation.head = animator.GetBoneTransform(HumanBodyBones.Head);
                    if (!presentation.head)
                        throw new InvalidOperationException("City People humanoid has no head bone: " + modelPath);

                    if (!PrefabUtility.SaveAsPrefabAsset(root, path))
                        throw new InvalidOperationException("Could not save resident: " + path);
                } finally {
                    PrefabUtility.UnloadPrefabContents(root);
                }
            }
            AssetDatabase.SaveAssets();
        }

        public static bool UsesExpectedAssets(NpcPresentation presentation) {
            var resident = Residents[presentation.role];
            var clips = presentation.animator.runtimeAnimatorController?.animationClips;
            return AssetDatabase.GetAssetPath(presentation.animator.avatar) == Source + "Meshes/" + resident.model + ".fbx"
                && clips != null && clips.Length == 1 && clips[0].isLooping
                && AssetDatabase.GetAssetPath(clips[0]) == Source + "Animations/" + resident.idle + ".fbx";
        }

        static AnimatorController GetIdleController(string idle, string rig) {
            var clip = AssetDatabase.LoadAllAssetsAtPath(Source + "Animations/" + idle + ".fbx")
                .OfType<AnimationClip>().Single(clip => clip.name == idle);
            string path = Destination + "Animations/CityPeople" + rig + "Idle.controller";
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(path);
            if (controller) return controller;

            controller = AnimatorController.CreateAnimatorControllerAtPath(path);
            var stateMachine = controller.layers[0].stateMachine;
            var state = stateMachine.AddState("Idle");
            state.motion = clip;
            stateMachine.defaultState = state;
            return controller;
        }

        static Bounds GetBounds(SkinnedMeshRenderer[] renderers) {
            if (renderers.Length == 0) throw new InvalidOperationException("City People model has no skinned meshes.");
            var bounds = renderers[0].bounds;
            foreach (var renderer in renderers) bounds.Encapsulate(renderer.bounds);
            return bounds;
        }
    }
}
