using System.Collections;
using NUnit.Framework;
using ProjectRelay.Gameplay.Player;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace ProjectRelay.Tests.PlayMode.Player
{
    /// <summary>
    /// 验证现有 SampleScene 在真实生命周期中完成 Player 新控制状态机装配。
    /// </summary>
    public sealed class PlayerControlScenePlayModeTests
    {
        private const string mSampleSceneName = "SampleScene";

        /// <summary>
        /// 加载 SampleScene，等待 Installer.Start 后验证 Player 已由新 FSM 进入 Idle。
        /// </summary>
        [UnityTest]
        public IEnumerator LoadSampleScene_InstallerInitializesNewControlStateMachine()
        {
            AsyncOperation _loadOperation = SceneManager.LoadSceneAsync(
                mSampleSceneName,
                LoadSceneMode.Additive);
            yield return _loadOperation;
            yield return null;

            Scene _sampleScene = SceneManager.GetSceneByName(mSampleSceneName);
            PlayerController _playerController = FindPlayerController(_sampleScene);

            Assert.That(_sampleScene.IsValid(), Is.True);
            Assert.That(_sampleScene.isLoaded, Is.True);
            Assert.That(_playerController, Is.Not.Null);
            Assert.That(_playerController.IsInitialized, Is.True);
            Assert.That(
                _playerController.CurrentControlState,
                Is.EqualTo(PlayerControlStateId.Idle));

            AsyncOperation _unloadOperation =
                SceneManager.UnloadSceneAsync(_sampleScene);
            yield return _unloadOperation;
        }

        /// <summary>
        /// 在指定已加载场景的根对象中查找 PlayerController。
        /// </summary>
        /// <param name="_scene">需要搜索的 SampleScene。</param>
        /// <returns>场景中的 PlayerController；不存在时返回 null。</returns>
        private static PlayerController FindPlayerController(Scene _scene)
        {
            foreach (GameObject _rootObject in _scene.GetRootGameObjects())
            {
                PlayerController _playerController =
                    _rootObject.GetComponentInChildren<PlayerController>(true);

                if (_playerController != null)
                {
                    return _playerController;
                }
            }

            return null;
        }
    }
}
