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
    public sealed class ProcedureGetGlobalInfoState : ProcedureBase
    {
        protected override void OnEnter(IFsm<IProcedureManager> procedureOwner)
        {
            base.OnEnter(procedureOwner);
            if (GameApp.Asset.GamePlayMode == EPlayMode.EditorSimulateMode)
            {
                Debug.Log("当前为编辑器模式，直接启动 ProcedureGetAppVersionInfoState");
                ChangeState<ProcedureGetAppVersionInfoState>(procedureOwner);
                return;
            }

            if (GameApp.Asset.GamePlayMode == EPlayMode.OfflinePlayMode)
            {
                Debug.Log("当前为离线模式，直接启动 ProcedurePatchInit");
                ChangeState<ProcedurePatchInit>(procedureOwner);
                return;
            }

            GetGlobalInfo(procedureOwner);
        }

        private async void GetGlobalInfo(IFsm<IProcedureManager> procedureOwner)
        {
            const string rootUrl = "http://47.113.221.3:28080/api/GameGlobalInfo/GetInfo";
            var jsonParams = HttpHelper.GetBaseParams();
            try
            {
                var json = await GameApp.Web.PostToString(rootUrl, jsonParams);
                Debug.Log(json);

                HttpJsonResult httpJsonResult = Utility.Json.ToObject<HttpJsonResult>(json.Result);
                if (httpJsonResult.Code > 0)
                {
                    LauncherUIHandler.SetTipText("Global info unavailable, using built-in resources...");
                    Log.Error($"获取全局信息返回异常=> Req:{jsonParams} Resp:{json}");
                    ChangeState<ProcedurePatchInit>(procedureOwner);
                    return;
                }

                ResponseGlobalInfo responseGlobalInfo = Utility.Json.ToObject<ResponseGlobalInfo>(httpJsonResult.Data);
                GlobalConfigComponent globalConfigComponent = GameApp.GlobalConfig;
                globalConfigComponent.CheckAppVersionUrl = responseGlobalInfo.CheckAppVersionUrl;
                globalConfigComponent.CheckResourceVersionUrl = responseGlobalInfo.CheckResourceVersionUrl;
                globalConfigComponent.Content = responseGlobalInfo.Content;
                LauncherUIHandler.SetTipText("Loading...");
                ChangeState<ProcedureGetAppVersionInfoState>(procedureOwner);
            }
            catch (Exception e)
            {
                Log.Error(e);
                LauncherUIHandler.SetTipText("Global info unreachable, using built-in resources...");
                Log.Error($"获取全局信息异常=>Error:{e.Message} Req:{jsonParams}");
                ChangeState<ProcedurePatchInit>(procedureOwner);
            }
        }
    }
}
