using NUnit.Framework;
using ProjectRelay.Gameplay.Combat;
using UnityEditor;

namespace ProjectRelay.Tests.EditMode.Combat
{
    /// <summary>
    /// 验证原有普通攻击资源在脚本改名并保留 GUID 后仍能解析为 Config 类型。
    /// </summary>
    public sealed class BasicAttackConfigAssetTests
    {
        private const string mDefaultConfigPath =
            "Assets/ProjectRelay/Config/BasicAttack_Default.asset";

        /// <summary>
        /// 验证默认资源没有因旧类型到 BasicAttackConfig 的改名丢失绑定。
        /// </summary>
        [Test]
        public void LoadDefaultConfig_AfterTypeRename_ReturnsValidConfig()
        {
            BasicAttackConfig _config =
                AssetDatabase.LoadAssetAtPath<BasicAttackConfig>(mDefaultConfigPath);

            Assert.That(_config, Is.Not.Null);
            Assert.That(_config.IsValid, Is.True);
        }
    }
}
