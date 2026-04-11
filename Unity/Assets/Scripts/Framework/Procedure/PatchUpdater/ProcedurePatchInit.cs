using Cysharp.Threading.Tasks;
using GameFrameX.Asset.Runtime;
using GameFrameX.Fsm.Runtime;
using GameFrameX.Procedure.Runtime;
using GameFrameX.Runtime;
using YooAsset;

namespace Unity.Startup.Procedure
{
    internal sealed class ProcedurePatchInit : ProcedureBase
    {
        protected override async void OnEnter(IFsm<IProcedureManager> procedureOwner)
        {
            base.OnEnter(procedureOwner);
            if (GameApp.Asset.GamePlayMode == EPlayMode.EditorSimulateMode)
            {
                await GameApp.Asset.InitPackageAsync(AssetComponent.BuildInPackageName, string.Empty, string.Empty, true);
                ChangeState<ProcedureUpdateStaticVersion>(procedureOwner);
                return;
            }

            if (GameApp.Asset.GamePlayMode == EPlayMode.OfflinePlayMode)
            {
                Log.Info("当前为离线模式，直接启动 ProcedureUpdateStaticVersion");
                await GameApp.Asset.InitPackageAsync(AssetComponent.BuildInPackageName, string.Empty, string.Empty, true);
                ChangeState<ProcedureUpdateStaticVersion>(procedureOwner);
                return;
            }

            Start(procedureOwner);
        }

        async void Start(IFsm<IProcedureManager> procedureOwner)
        {
            var buildInPackageNameURL = procedureOwner.GetData<VarString>(AssetComponent.BuildInPackageName);
            if (buildInPackageNameURL == null || string.IsNullOrWhiteSpace(buildInPackageNameURL.Value))
            {
                Log.Warning("未获取到远程资源地址，使用内置资源继续启动。");
                await GameApp.Asset.InitPackageAsync(AssetComponent.BuildInPackageName, string.Empty, string.Empty, true);
            }
            else
            {
                Log.Debug("下载资源的路径：" + buildInPackageNameURL.Value);
                await GameApp.Asset.InitPackageAsync(AssetComponent.BuildInPackageName, buildInPackageNameURL.Value, buildInPackageNameURL.Value, true);
                procedureOwner.RemoveData(AssetComponent.BuildInPackageName);
            }

            await UniTask.DelayFrame();
            ChangeState<ProcedureUpdateStaticVersion>(procedureOwner);
        }
    }
}
