using NUnit.Framework;
using ProjectRelay.Core;
using ProjectRelay.Dev;
using ProjectRelay.Gameplay.Combat;
using ProjectRelay.Gameplay.Player;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ProjectRelay.Tests.EditMode.Player
{
    /// <summary>
    /// 验证现有 Player Prefab 和 SampleScene 持久化了新控制闭环需要的组件引用。
    /// </summary>
    public sealed class PlayerControlAssetWiringTests
    {
        private const string mPlayerPrefabPath =
            "Assets/ProjectRelay/Prefabs/PF_Player.prefab";
        private const string mSampleScenePath =
            "Assets/ProjectRelay/Scenes/SampleScene.unity";

        /// <summary>
        /// 验证 Player Prefab 的控制器、攻击执行器、身份和 Gateway 全部同对象连接。
        /// </summary>
        [Test]
        public void LoadPlayerPrefab_HasCompleteControlAndCombatReferences()
        {
            GameObject _playerPrefab =
                AssetDatabase.LoadAssetAtPath<GameObject>(mPlayerPrefabPath);

            Assert.That(_playerPrefab, Is.Not.Null);
            PlayerController _playerController =
                _playerPrefab.GetComponent<PlayerController>();
            CombatantIdentity _identity =
                _playerPrefab.GetComponent<CombatantIdentity>();
            BasicAttackController _attackController =
                _playerPrefab.GetComponent<BasicAttackController>();
            LocalCombatCommandGateway _gateway =
                _playerPrefab.GetComponent<LocalCombatCommandGateway>();

            Assert.That(_playerController, Is.Not.Null);
            Assert.That(_identity, Is.Not.Null);
            Assert.That(_identity.Faction, Is.EqualTo(Faction.Player));
            Assert.That(_attackController, Is.Not.Null);
            Assert.That(_attackController.Config, Is.Not.Null);
            Assert.That(_attackController.Config.IsValid, Is.True);
            Assert.That(_gateway, Is.Not.Null);

            SerializedObject _controllerObject =
                new SerializedObject(_playerController);
            Assert.That(
                _controllerObject.FindProperty("mBasicAttackController")
                    .objectReferenceValue,
                Is.SameAs(_attackController));
            Assert.That(
                _controllerObject.FindProperty("mLocalCombatCommandGateway")
                    .objectReferenceValue,
                Is.SameAs(_gateway));

            SerializedObject _gatewayObject = new SerializedObject(_gateway);
            Assert.That(
                _gatewayObject.FindProperty("mBasicAttackController")
                    .objectReferenceValue,
                Is.SameAs(_attackController));
        }

        /// <summary>
        /// 验证 SampleScene Installer 引用同一个 Player 实例上的新战斗组件。
        /// </summary>
        [Test]
        public void OpenSampleScene_InstallerReferencesPlayerCombatComponents()
        {
            Scene _scene = EditorSceneManager.OpenScene(
                mSampleScenePath,
                OpenSceneMode.Additive);

            try
            {
                BattleSandboxInstaller _installer =
                    FindInstaller(_scene);
                Assert.That(_installer, Is.Not.Null);

                SerializedObject _installerObject =
                    new SerializedObject(_installer);
                PlayerController _playerController =
                    _installerObject.FindProperty("mPlayerController")
                        .objectReferenceValue as PlayerController;
                BasicAttackController _attackController =
                    _installerObject.FindProperty("mBasicAttackController")
                        .objectReferenceValue as BasicAttackController;
                LocalCombatCommandGateway _gateway =
                    _installerObject.FindProperty("mCombatCommandGateway")
                        .objectReferenceValue as LocalCombatCommandGateway;

                Assert.That(_playerController, Is.Not.Null);
                Assert.That(_attackController, Is.Not.Null);
                Assert.That(_gateway, Is.Not.Null);
                Assert.That(
                    _attackController.gameObject,
                    Is.SameAs(_playerController.gameObject));
                Assert.That(
                    _gateway.gameObject,
                    Is.SameAs(_playerController.gameObject));
            }
            finally
            {
                EditorSceneManager.CloseScene(_scene, true);
            }
        }

        /// <summary>
        /// 在指定已加载场景的根对象中查找唯一开发场景安装器。
        /// </summary>
        /// <param name="_scene">需要搜索的已加载 SampleScene。</param>
        /// <returns>场景中的 Installer；不存在时返回 null。</returns>
        private static BattleSandboxInstaller FindInstaller(Scene _scene)
        {
            foreach (GameObject _rootObject in _scene.GetRootGameObjects())
            {
                BattleSandboxInstaller _installer =
                    _rootObject.GetComponentInChildren<BattleSandboxInstaller>(true);

                if (_installer != null)
                {
                    return _installer;
                }
            }

            return null;
        }
    }
}
