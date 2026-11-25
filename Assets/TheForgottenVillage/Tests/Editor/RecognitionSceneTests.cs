using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
namespace ForgottenVillage.Tests {
    public class RecognitionSceneTests {
        [Test]
        public void StartupSceneHasLiveCameraAndAllSixBuildDestinations() {
            const string path="Assets/Scenes/rpgpp_lt_scene_1.0.unity";
            var scene=SceneManager.GetSceneByPath(path);
            bool opened=!scene.IsValid()||!scene.isLoaded;
            if(opened)scene=EditorSceneManager.OpenScene(path,OpenSceneMode.Additive);
            try {
                var game=scene.GetRootGameObjects().SelectMany(x=>x.GetComponentsInChildren<VillageGame>(true)).Single();
                Assert.That(game.recognition,Is.Not.Null);
                Assert.That(game.recognition.game,Is.EqualTo(game));
                Assert.That(game.recognition.simulation,Is.False);
                Assert.That(game.recognition.accessToken,Is.Empty);
                Assert.That(game.transitions,Is.Not.Null);
                foreach(var name in HouseTransitions.SceneNames)
                    Assert.That(EditorBuildSettings.scenes.Any(x=>x.enabled&&x.path.EndsWith("/"+name+".unity")),Is.True,name);
            } finally { if(opened)EditorSceneManager.CloseScene(scene,true); }
        }
    }
}
