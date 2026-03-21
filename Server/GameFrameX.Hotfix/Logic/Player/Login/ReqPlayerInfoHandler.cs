namespace GameFrameX.Hotfix.Logic.Player.Login;

[MessageMapping(typeof(ReqPlayerInfo))]
internal sealed class ReqPlayerInfoHandler : PlayerRpcComponentHandler<PlayerComponentAgent, ReqPlayerInfo, RespPlayerInfo>
{
    protected override Task ActionAsync(ReqPlayerInfo request, RespPlayerInfo response)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(response);
        return ComponentAgent.OnPlayerInfo(NetWorkChannel, request, response);
    }
}

[MessageMapping(typeof(ReqPlayerPet))]
 internal sealed class ReqPlayerPetHandler : PlayerComponentHandler<PlayerComponentAgent, ReqPlayerPet>
 {
     protected override Task ActionAsync(ReqPlayerPet request)
     {
         ArgumentNullException.ThrowIfNull(request);
         return ComponentAgent.OnPlayerPet(NetWorkChannel, request);
     }
 }
