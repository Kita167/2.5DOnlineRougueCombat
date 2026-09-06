using ProjectRelay.Gameplay.Combat;
using ProjectRelay.Gameplay.Player;
using ProjectRelay.Input;
using ProjectRelay.Presentation.Camera;
using UnityEngine;

namespace ProjectRelay.Dev
{
    /// <summary>
    /// 为 BattleSandbox 显式连接本地输入源、玩家控制器和场景 Camera。
    /// 本组件只用于开发场景，正式 Battle 将由对应的场景组合根替代。
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class BattleSandboxInstaller : MonoBehaviour
    {
        [SerializeField]
        [Tooltip("BattleSandbox 中需要接收本地控制的玩家控制器。")]
        private PlayerController mPlayerController;

        [SerializeField]
        [Tooltip("BattleSandbox 中玩家对象持有的本地输入源。")]
        private LocalPlayerInputSource mInputSource;

        [SerializeField]
        [Tooltip("用于计算相机相对移动方向的 Gameplay Camera。")]
        private Camera mGameplayCamera;

        [SerializeField]
        [Tooltip("BattleSandbox 中负责跟随本地玩家的 CameraRig 控制器。")]
        private TopDownCameraController mCameraController;

        [SerializeField]
        [Tooltip("BattleSandbox 中本地玩家的普通攻击阶段与命中执行器。")]
        private BasicAttackController mBasicAttackController;

        [SerializeField]
        [Tooltip("将本地玩家攻击请求同步交给攻击执行器的权威入口。")]
        private LocalCombatCommandGateway mCombatCommandGateway;

        private bool mIsInstalled;

        /// <summary>
        /// 在所有场景对象完成 Awake 后连接玩家依赖，并启用本地控制。
        /// </summary>
        private void Start()
        {
            if (
                mPlayerController == null ||
                mInputSource == null ||
                mGameplayCamera == null ||
                mCameraController == null ||
                mBasicAttackController == null ||
                mCombatCommandGateway == null)
            {
                Debug.LogError(
                    "[Gameplay] BattleSandboxInstaller 缺少玩家、输入、相机或战斗组件引用。",
                    this);
                return;
            }

            mIsInstalled =
                mCombatCommandGateway.Initialize(mBasicAttackController);

            if (mIsInstalled)
            {
                mIsInstalled = mPlayerController.Initialize(
                    mInputSource,
                    mGameplayCamera,
                    mBasicAttackController,
                    mCombatCommandGateway);
            }

            if (mIsInstalled)
            {
                mIsInstalled = mCameraController.Bind(mPlayerController.transform);
            }

            if (mIsInstalled)
            {
                mPlayerController.SetControlEnabled(true);
            }
        }

        /// <summary>
        /// 在安装器重新启用时恢复已经完成初始化的玩家控制。
        /// </summary>
        private void OnEnable()
        {
            if (mIsInstalled && mPlayerController != null)
            {
                if (mCameraController != null)
                {
                    mCameraController.Bind(mPlayerController.transform);
                }

                mPlayerController.SetControlEnabled(true);
            }
        }

        /// <summary>
        /// 在开发场景退出或安装器禁用时关闭输入，避免产生跨场景残留意图。
        /// </summary>
        private void OnDisable()
        {
            if (mIsInstalled && mPlayerController != null)
            {
                mPlayerController.SetControlEnabled(false);
            }

            if (mCameraController != null)
            {
                mCameraController.Unbind();
            }
        }
    }
}
