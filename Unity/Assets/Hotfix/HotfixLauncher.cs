// ==========================================================================================
//  GameFrameX 组织及其衍生项目的版权、商标、专利及其他相关权利
//  GameFrameX organization and its derivative projects' copyrights, trademarks, patents, and related rights
//  均受中华人民共和国及相关国际法律法规保护。
//  are protected by the laws of the People's Republic of China and relevant international regulations.
//  
//  使用本项目须严格遵守相应法律法规及开源许可证之规定。
//  Usage of this project must strictly comply with applicable laws, regulations, and open-source licenses.
//  
//  本项目采用 MIT 许可证与 Apache License 2.0 双许可证分发，
//  This project is dual-licensed under the MIT License and Apache License 2.0,
//  完整许可证文本请参见源代码根目录下的 LICENSE 文件。
//  please refer to the LICENSE file in the root directory of the source code for the full license text.
//  
//  禁止利用本项目实施任何危害国家安全、破坏社会秩序、
//  It is prohibited to use this project to engage in any activities that endanger national security, disrupt social order,
//  侵犯他人合法权益等法律法规所禁止的行为！
//  or infringe upon the legitimate rights and interests of others, as prohibited by laws and regulations!
//  因基于本项目二次开发所产生的一切法律纠纷与责任，
//  Any legal disputes and liabilities arising from secondary development based on this project
//  本项目组织与贡献者概不承担。
//  shall be borne solely by the developer; the project organization and contributors assume no responsibility.
//  
//  GitHub 仓库：https://github.com/GameFrameX
//  GitHub Repository: https://github.com/GameFrameX
//  Gitee  仓库：https://gitee.com/GameFrameX
//  Gitee Repository:  https://gitee.com/GameFrameX
//  官方文档：https://gameframex.doc.alianblank.com/
//  Official Documentation: https://gameframex.doc.alianblank.com/
// ==========================================================================================
using System.Net;
using System.Threading.Tasks;
using System.IO;
using Cysharp.Threading.Tasks;
using GameFrameX;
using GameFrameX.Event.Runtime;
#if ENABLE_UI_FAIRYGUI
using GameFrameX.UI.FairyGUI.Runtime;
#endif
using GameFrameX.Network.Runtime;
using Hotfix.Proto;
using SimpleJSON;
using UnityEngine;
using GameFrameX.Runtime;
using GameFrameX.UI.Runtime;
using Hotfix.Config;
using Hotfix.Config.Tables;
using Hotfix.Manager;
using Hotfix.UI;
#if ENABLE_BINARY_CONFIG
using LuBan.Runtime;
#endif

namespace Hotfix
{
    public static class HotfixLauncher
    {
        public static void Main()
        {
            Log.Info("Hello World HybridCLR");
            ProtoMessageIdHandler.Init(HotfixProtoHandler.CurrentAssembly);
            _ = MatchManager.Instance;
            SurvivalMatchBattleRuntime.Install();
            BootstrapAsync().Forget();
        }

        private static async UniTaskVoid BootstrapAsync()
        {
            await LoadConfigAsync();
            await LoadUIAsync();
        }

        private static async UniTask LoadUIAsync()
        {
#if ENABLE_UI_FAIRYGUI
            GameApp.FUIPackage.AddPackageAsync(Utility.Asset.Path.GetUIPackagePath(FUIPackage.UICommon));
            GameApp.FUIPackage.AddPackageAsync(Utility.Asset.Path.GetUIPackagePath(FUIPackage.UICommonAvatar));
            GameApp.FUIPackage.AddPackageAsync(Utility.Asset.Path.GetUIPackagePath(FUIPackage.UI3Pick1));
#endif
            await GameApp.UI.OpenAsync<UILogin>();
            var item = GameApp.Config.GetConfig<TbSoundsConfig>().FirstOrDefault;
            Log.Info(item);
        }

        private static async UniTask LoadConfigAsync()
        {
            var tablesComponent = new TablesComponent();
            tablesComponent.Init(GameApp.Config);
            Log.Info("[ConfigLoad] begin tables load.");
#if ENABLE_BINARY_CONFIG
            await tablesComponent.LoadAsync(ConfigBufferLoader);
#else
            await tablesComponent.LoadAsync(ConfigLoader);
#endif
            Log.Info($"[ConfigLoad] complete loaded={tablesComponent.IsLoaded} " +
                     $"tbweapon={GameApp.Config.GetConfig<TbWeapon>()?.Count ?? -1} " +
                     $"survivalWeapon={GameApp.Config.GetConfig<Hotfix.Config.Tables.survival.Tbweapon>()?.Count ?? -1} " +
                     $"monster={GameApp.Config.GetConfig<Hotfix.Config.Tables.TbMonsterConfig>()?.Count ?? -1} " +
                     $"monsterSkill={GameApp.Config.GetConfig<Hotfix.Config.Tables.TbMonsterSkillConfig>()?.Count ?? -1} " +
                     $"wave={GameApp.Config.GetConfig<Hotfix.Config.Tables.TbMonsterSpawnWaveConfig>()?.Count ?? -1} " +
                     $"bulletVisual={GameApp.Config.GetConfig<Hotfix.Config.Tables.survival.TbBulletVisualConfig>()?.Count ?? -1}");
        }


#if ENABLE_BINARY_CONFIG
        /// <summary>
        /// 加载二进制配置表
        /// </summary>
        /// <param name="file"></param>
        /// <returns></returns>
        private static async Task<ByteBuf> ConfigBufferLoader(string file)
        {
            var assetHandle = await GameApp.Asset.LoadAssetAsync<TextAsset>(Utility.Asset.Path.GetConfigPath(file, Utility.Const.FileNameSuffix.Binary));
            return ByteBuf.Wrap(assetHandle.GetAssetObject<TextAsset>().bytes);
        }
#else
        /// <summary>
        /// 加载json配置表
        /// </summary>
        /// <param name="file"></param>
        /// <returns></returns>
        private static async Task<JSONNode> ConfigLoader(string file)
        {
            var assetPath = Utility.Asset.Path.GetConfigPath(file, Utility.Const.FileNameSuffix.Json);
            Log.Info($"[ConfigLoad] request file={file} path={assetPath}");

            var directConfigPath = Path.Combine(Application.dataPath, "Bundles", "Config", file + Utility.Const.FileNameSuffix.Json);
            if (File.Exists(directConfigPath))
            {
                var directText = await File.ReadAllTextAsync(directConfigPath);
                Log.Info($"[ConfigLoad] direct file hit file={file} path={directConfigPath} textLength={directText?.Length ?? 0}");
                var directJson = JSON.Parse(directText);
                Log.Info($"[ConfigLoad] direct parsed file={file} nodeType={directJson?.Tag.ToString() ?? "null"} count={directJson?.Count ?? -1}");
                return directJson;
            }

            var assetHandle = await GameApp.Asset.LoadAssetAsync<TextAsset>(assetPath);
            var textAsset = assetHandle.GetAssetObject<TextAsset>();
            if (textAsset == null)
            {
                Log.Error($"[ConfigLoad] asset missing file={file} path={assetPath}");
                return null;
            }

            Log.Info($"[ConfigLoad] asset ok file={file} path={assetPath} bytes={textAsset.bytes?.Length ?? 0} textLength={textAsset.text?.Length ?? 0}");
            var json = JSON.Parse(textAsset.text);
            var childCount = json != null ? json.Count : -1;
            Log.Info($"[ConfigLoad] parsed file={file} nodeType={json?.Tag.ToString() ?? "null"} count={childCount}");
            return json;
        }
#endif
    }
}
