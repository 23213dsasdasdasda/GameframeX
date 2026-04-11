using System;
using GameFrameX.Fsm.Runtime;
using GameFrameX.GlobalConfig.Runtime;
using GameFrameX.Procedure.Runtime;
using GameFrameX.Runtime;
using GameFrameX.Web.Runtime;
using UnityEngine;
using YooAsset;

namespace Unity.Startup.Procedure
{
    public sealed class ProcedureGetAppVersionInfoState : ProcedureBase
    {
        protected override void OnEnter(IFsm<IProcedureManager> procedureOwner)
        {
            base.OnEnter(procedureOwner);

            if (GameApp.Asset.GamePlayMode == EPlayMode.EditorSimulateMode)
            {
                Debug.Log("当前为编辑器模式，直接启动 ProcedurePatchInit");
                ChangeState<ProcedurePatchInit>(procedureOwner);
                return;
            }

            GetAppVersionInfo(procedureOwner);
        }

        private async void GetAppVersionInfo(IFsm<IProcedureManager> procedureOwner)
        {
            var jsonParams = HttpHelper.GetBaseParams();
            try
            {
                var json = await GameApp.Web.PostToString(GameApp.GlobalConfig.CheckAppVersionUrl, jsonParams);
                Debug.Log(json);

                HttpJsonResult httpJsonResult = Utility.Json.ToObject<HttpJsonResult>(json.Result);
                if (httpJsonResult.Code > 0)
                {
                    LauncherUIHandler.SetTipText("Version service unavailable, using built-in resources...");
                    Debug.LogError($"获取版本信息返回异常=> Req:{jsonParams} Resp:{json}");
                    ChangeState<ProcedurePatchInit>(procedureOwner);
                    return;
                }

                Utility.Json.ToObject<ResponseGameAppVersion>(httpJsonResult.Data);
                ChangeState<ProcedureGetGameAssetPackageVersionInfoByDefaultPackageState>(procedureOwner);
            }
            catch (Exception e)
            {
                Log.Error(e);
                LauncherUIHandler.SetTipText("Version service unreachable, using built-in resources...");
                Debug.LogError($"获取版本信息异常=>Error:{e.Message} Req:{jsonParams}");
                ChangeState<ProcedurePatchInit>(procedureOwner);
            }
        }
    }
}
