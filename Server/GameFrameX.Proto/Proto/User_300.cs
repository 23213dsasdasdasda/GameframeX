using System;
using ProtoBuf;
using System.Collections.Generic;
using GameFrameX.NetWork.Abstractions;
using GameFrameX.NetWork.Messages;

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
namespace GameFrameX.Proto.Proto
{
	/// <summary>
	/// 请求账号登录
	/// </summary>
	[ProtoContract]
	[System.ComponentModel.Description("请求账号登录")]
	[MessageTypeHandler(((300) << 16) + 10)]
	public sealed class ReqLogin : MessageObject, IRequestMessage
	{
		/// <summary>
		/// 
		/// </summary>
		[ProtoMember(1)]
		[System.ComponentModel.Description("")]
		public long Id { get; set; }

		/// <summary>
		/// 平台标识 (steam, android, ios, windows)
		/// </summary>
		[ProtoMember(2)]
		[System.ComponentModel.Description("平台标识 (steam, android, ios, windows)")]
		public string Platform { get; set; }

		/// <summary>
		/// SDK 类型 (0: 官方, 1: Steam, 2: 微信...)
		/// </summary>
		[ProtoMember(3)]
		[System.ComponentModel.Description("SDK 类型 (0: 官方, 1: Steam, 2: 微信...)")]
		public int SdkType { get; set; }

		/// <summary>
		/// 登录凭证 (Steam Session Ticket)
		/// </summary>
		[ProtoMember(4)]
		[System.ComponentModel.Description("登录凭证 (Steam Session Ticket)")]
		public string SdkToken { get; set; }

		/// <summary>
		/// 设备唯一标识 (用于封禁或追踪)
		/// </summary>
		[ProtoMember(5)]
		[System.ComponentModel.Description("设备唯一标识 (用于封禁或追踪)")]
		public string Device { get; set; }

		/// <summary>
		/// 密码 (非第三方登录时使用)
		/// </summary>
		[ProtoMember(6)]
		[System.ComponentModel.Description("密码 (非第三方登录时使用)")]
		public string Password { get; set; }

		/// <summary>
		/// [补全] 设备型号 (如: RTX 4090, iPhone 15)
		/// </summary>
		[ProtoMember(7)]
		[System.ComponentModel.Description("[补全] 设备型号 (如: RTX 4090, iPhone 15)")]
		public string DeviceModel { get; set; }

		/// <summary>
		/// [补全] 客户端版本号 (用于版本强制更新检查)
		/// </summary>
		[ProtoMember(8)]
		[System.ComponentModel.Description("[补全] 客户端版本号 (用于版本强制更新检查)")]
		public string Version { get; set; }

		/// <summary>
		/// 账号名 (SteamID / 手机号 / 自定义账号)
		/// </summary>
		[ProtoMember(9)]
		[System.ComponentModel.Description("账号名 (SteamID / 手机号 / 自定义账号)")]
		public string UserName { get; set; }

		public override void Clear()
		{
			Id = default;
			Platform = default;
			SdkType = default;
			SdkToken = default;
			Device = default;
			Password = default;
			DeviceModel = default;
			Version = default;
			UserName = default;
		}
	}

	/// <summary>
	/// 请求账号登录返回
	/// </summary>
	[ProtoContract]
	[System.ComponentModel.Description("请求账号登录返回")]
	[MessageTypeHandler(((300) << 16) + 11)]
	public sealed class RespLogin : MessageObject, IResponseMessage
	{
		/// <summary>
		/// 
		/// </summary>
		[ProtoMember(1)]
		[System.ComponentModel.Description("")]
		public long Id { get; set; }

		/// <summary>
		/// 
		/// </summary>
		[ProtoMember(2)]
		[System.ComponentModel.Description("")]
		public string RoleName { get; set; }

		/// <summary>
		/// 
		/// </summary>
		[ProtoMember(3)]
		[System.ComponentModel.Description("")]
		public int Code { get; set; }

		/// <summary>
		/// 
		/// </summary>
		[ProtoMember(4)]
		[System.ComponentModel.Description("")]
		public uint Level { get; set; }

		/// <summary>
		/// 
		/// </summary>
		[ProtoMember(5)]
		[System.ComponentModel.Description("")]
		public long CreateTime { get; set; }

		/// <summary>
		/// 
		/// </summary>
		[ProtoMember(6)]
		[System.ComponentModel.Description("")]
		public string Token { get; set; }

		/// <summary>
		/// 
		/// </summary>
		[ProtoMember(7)]
		[System.ComponentModel.Description("")]
		public string ServerTime { get; set; }

		/// <summary>
		/// 返回的错误码
		/// </summary>
		[ProtoMember(2047)]
		[System.ComponentModel.Description("返回的错误码")]
		public int ErrorCode { get; set; }

		public override void Clear()
		{
			Id = default;
			RoleName = default;
			Code = default;
			Level = default;
			CreateTime = default;
			Token = default;
			ServerTime = default;
			ErrorCode = default;
		}
	}

	/// <summary>
	/// 请求角色创建
	/// </summary>
	[ProtoContract]
	[System.ComponentModel.Description("请求角色创建")]
	[MessageTypeHandler(((300) << 16) + 12)]
	public sealed class ReqPlayerCreate : MessageObject, IRequestMessage
	{
		/// <summary>
		/// 账号ID
		/// </summary>
		[ProtoMember(1)]
		[System.ComponentModel.Description("账号ID")]
		public long Id { get; set; }

		/// <summary>
		/// 角色名
		/// </summary>
		[ProtoMember(2)]
		[System.ComponentModel.Description("角色名")]
		public string Name { get; set; }

		public override void Clear()
		{
			Id = default;
			Name = default;
		}
	}

	/// <summary>
	/// 请求角色创建返回
	/// </summary>
	[ProtoContract]
	[System.ComponentModel.Description("请求角色创建返回")]
	[MessageTypeHandler(((300) << 16) + 13)]
	public sealed class RespPlayerCreate : MessageObject, IResponseMessage
	{
		/// <summary>
		/// 角色信息
		/// </summary>
		[ProtoMember(1)]
		[System.ComponentModel.Description("角色信息")]
		public PlayerInfo PlayerInfo { get; set; }

		/// <summary>
		/// 返回的错误码
		/// </summary>
		[ProtoMember(2047)]
		[System.ComponentModel.Description("返回的错误码")]
		public int ErrorCode { get; set; }

		public override void Clear()
		{
			PlayerInfo = default;
			ErrorCode = default;
		}
	}

	/// <summary>
	/// 请求角色列表
	/// </summary>
	[ProtoContract]
	[System.ComponentModel.Description("请求角色列表")]
	[MessageTypeHandler(((300) << 16) + 14)]
	public sealed class ReqPlayerList : MessageObject, IRequestMessage
	{
		/// <summary>
		/// 账号ID
		/// </summary>
		[ProtoMember(1)]
		[System.ComponentModel.Description("账号ID")]
		public long Id { get; set; }

		public override void Clear()
		{
			Id = default;
		}
	}

	/// <summary>
	/// 请求角色列表返回
	/// </summary>
	[ProtoContract]
	[System.ComponentModel.Description("请求角色列表返回")]
	[MessageTypeHandler(((300) << 16) + 15)]
	public sealed class RespPlayerList : MessageObject, IResponseMessage
	{
		/// <summary>
		/// 角色列表
		/// </summary>
		[ProtoMember(1)]
		[System.ComponentModel.Description("角色列表")]
		public List<PlayerInfo> PlayerList { get; set; } = new List<PlayerInfo>();

		/// <summary>
		/// 返回的错误码
		/// </summary>
		[ProtoMember(2047)]
		[System.ComponentModel.Description("返回的错误码")]
		public int ErrorCode { get; set; }

		public override void Clear()
		{
			PlayerList.Clear();
			ErrorCode = default;
		}
	}

	/// <summary>
	/// 
	/// </summary>
	[ProtoContract]
	[System.ComponentModel.Description("")]
	public sealed class PlayerInfo
	{
		/// <summary>
		/// 角色ID
		/// </summary>
		[ProtoMember(1)]
		[System.ComponentModel.Description("角色ID")]
		public long Id { get; set; }

		/// <summary>
		/// 角色名
		/// </summary>
		[ProtoMember(2)]
		[System.ComponentModel.Description("角色名")]
		public string Name { get; set; }

		/// <summary>
		/// 角色等级
		/// </summary>
		[ProtoMember(3)]
		[System.ComponentModel.Description("角色等级")]
		public uint Level { get; set; }

		/// <summary>
		/// 角色状态
		/// </summary>
		[ProtoMember(4)]
		[System.ComponentModel.Description("角色状态")]
		public int State { get; set; }

		/// <summary>
		/// 角色头像
		/// </summary>
		[ProtoMember(5)]
		[System.ComponentModel.Description("角色头像")]
		public uint Avatar { get; set; }

		/// <summary>
		/// 角色当前经验
		/// </summary>
		[ProtoMember(6)]
		[System.ComponentModel.Description("角色当前经验")]
		public ulong CurrentExp { get; set; }
	}

	/// <summary>
	/// 请求玩家登录
	/// </summary>
	[ProtoContract]
	[System.ComponentModel.Description("请求玩家登录")]
	[MessageTypeHandler(((300) << 16) + 16)]
	public sealed class ReqPlayerLogin : MessageObject, IRequestMessage
	{
		/// <summary>
		/// 账号名 (SteamID / 手机号 / 自定义账号)
		/// </summary>
		[ProtoMember(9)]
		[System.ComponentModel.Description("账号名 (SteamID / 手机号 / 自定义账号)")]
		public string UserName { get; set; }

		/// <summary>
		/// 平台标识 (steam, android, ios, windows)
		/// </summary>
		[ProtoMember(2)]
		[System.ComponentModel.Description("平台标识 (steam, android, ios, windows)")]
		public string Platform { get; set; }

		/// <summary>
		/// SDK 类型 (0: 官方, 1: Steam, 2: 微信...)
		/// </summary>
		[ProtoMember(3)]
		[System.ComponentModel.Description("SDK 类型 (0: 官方, 1: Steam, 2: 微信...)")]
		public int SdkType { get; set; }

		/// <summary>
		/// 登录凭证 (Steam Session Ticket)
		/// </summary>
		[ProtoMember(4)]
		[System.ComponentModel.Description("登录凭证 (Steam Session Ticket)")]
		public string SdkToken { get; set; }

		/// <summary>
		/// 设备唯一标识 (用于封禁或追踪)
		/// </summary>
		[ProtoMember(5)]
		[System.ComponentModel.Description("设备唯一标识 (用于封禁或追踪)")]
		public string Device { get; set; }

		/// <summary>
		/// 密码 (非第三方登录时使用)
		/// </summary>
		[ProtoMember(6)]
		[System.ComponentModel.Description("密码 (非第三方登录时使用)")]
		public string Password { get; set; }

		/// <summary>
		/// [补全] 设备型号 (如: RTX 4090, iPhone 15)
		/// </summary>
		[ProtoMember(7)]
		[System.ComponentModel.Description("[补全] 设备型号 (如: RTX 4090, iPhone 15)")]
		public string DeviceModel { get; set; }

		/// <summary>
		/// [补全] 客户端版本号 (用于版本强制更新检查)
		/// </summary>
		[ProtoMember(8)]
		[System.ComponentModel.Description("[补全] 客户端版本号 (用于版本强制更新检查)")]
		public string Version { get; set; }

		/// <summary>
		/// 
		/// </summary>
		[ProtoMember(1)]
		[System.ComponentModel.Description("")]
		public long Id { get; set; }

		public override void Clear()
		{
			UserName = default;
			Platform = default;
			SdkType = default;
			SdkToken = default;
			Device = default;
			Password = default;
			DeviceModel = default;
			Version = default;
			Id = default;
		}
	}

	/// <summary>
	/// 请求玩家登录返回
	/// </summary>
	[ProtoContract]
	[System.ComponentModel.Description("请求玩家登录返回")]
	[MessageTypeHandler(((300) << 16) + 17)]
	public sealed class RespPlayerLogin : MessageObject, IResponseMessage
	{
		/// <summary>
		/// 
		/// </summary>
		[ProtoMember(1)]
		[System.ComponentModel.Description("")]
		public int Code { get; set; }

		/// <summary>
		/// 创建时间
		/// </summary>
		[ProtoMember(2)]
		[System.ComponentModel.Description("创建时间")]
		public long CreateTime { get; set; }

		/// <summary>
		/// 角色信息
		/// </summary>
		[ProtoMember(3)]
		[System.ComponentModel.Description("角色信息")]
		public PlayerInfo PlayerInfo { get; set; }

		/// <summary>
		/// 返回的错误码
		/// </summary>
		[ProtoMember(2047)]
		[System.ComponentModel.Description("返回的错误码")]
		public int ErrorCode { get; set; }

		public override void Clear()
		{
			Code = default;
			CreateTime = default;
			PlayerInfo = default;
			ErrorCode = default;
		}
	}

	/// <summary>
	/// 客户端每次请求都会回复错误码
	/// </summary>
	[ProtoContract]
	[System.ComponentModel.Description("客户端每次请求都会回复错误码")]
	[MessageTypeHandler(((300) << 16) + 18)]
	public sealed class RespErrorCode : MessageObject, IResponseMessage
	{
		/// <summary>
		/// 0:表示无错误
		/// </summary>
		[ProtoMember(1)]
		[System.ComponentModel.Description("0:表示无错误")]
		public long ErrCode { get; set; }

		/// <summary>
		/// 错误描述（不为0时有效）
		/// </summary>
		[ProtoMember(2)]
		[System.ComponentModel.Description("错误描述（不为0时有效）")]
		public string Desc { get; set; }

		/// <summary>
		/// 返回的错误码
		/// </summary>
		[ProtoMember(2047)]
		[System.ComponentModel.Description("返回的错误码")]
		public int ErrorCode { get; set; }

		public override void Clear()
		{
			ErrCode = default;
			Desc = default;
			ErrorCode = default;
		}
	}

	/// <summary>
	/// 
	/// </summary>
	[ProtoContract]
	[System.ComponentModel.Description("")]
	[MessageTypeHandler(((300) << 16) + 19)]
	public sealed class RespPrompt : MessageObject, IResponseMessage
	{
		/// <summary>
		/// 提示信息类型（1Tip提示，2跑马灯，3插队跑马灯，4弹窗，5弹窗回到登陆，6弹窗退出游戏）
		/// </summary>
		[ProtoMember(1)]
		[System.ComponentModel.Description("提示信息类型（1Tip提示，2跑马灯，3插队跑马灯，4弹窗，5弹窗回到登陆，6弹窗退出游戏）")]
		public int Type { get; set; }

		/// <summary>
		/// 提示内容
		/// </summary>
		[ProtoMember(2)]
		[System.ComponentModel.Description("提示内容")]
		public string Content { get; set; }

		/// <summary>
		/// 返回的错误码
		/// </summary>
		[ProtoMember(2047)]
		[System.ComponentModel.Description("返回的错误码")]
		public int ErrorCode { get; set; }

		public override void Clear()
		{
			Type = default;
			Content = default;
			ErrorCode = default;
		}
	}

	/// <summary>
	/// 请求玩家参数
	/// </summary>
	[ProtoContract]
	[System.ComponentModel.Description("请求玩家参数")]
	[MessageTypeHandler(((300) << 16) + 20)]
	public sealed class ReqPlayerInfo : MessageObject, IRequestMessage
	{
		/// <summary>
		/// 
		/// </summary>
		[ProtoMember(1)]
		[System.ComponentModel.Description("")]
		public long PlayerId { get; set; }

		public override void Clear()
		{
			PlayerId = default;
		}
	}

	/// <summary>
	/// 
	/// </summary>
	[ProtoContract]
	[System.ComponentModel.Description("")]
	[MessageTypeHandler(((300) << 16) + 21)]
	public sealed class RespPlayerInfo : MessageObject, IResponseMessage
	{
		/// <summary>
		/// 
		/// </summary>
		[ProtoMember(1)]
		[System.ComponentModel.Description("")]
		public PlayerInfo PlayerInfo { get; set; }

		/// <summary>
		/// 返回的错误码
		/// </summary>
		[ProtoMember(2047)]
		[System.ComponentModel.Description("返回的错误码")]
		public int ErrorCode { get; set; }

		public override void Clear()
		{
			PlayerInfo = default;
			ErrorCode = default;
		}
	}

	/// <summary>
	/// 请求玩家宠物数据
	/// </summary>
	[ProtoContract]
	[System.ComponentModel.Description("请求玩家宠物数据")]
	[MessageTypeHandler(((300) << 16) + 22)]
	public sealed class ReqPlayerPet : MessageObject, IRequestMessage
	{
		/// <summary>
		/// 
		/// </summary>
		[ProtoMember(1)]
		[System.ComponentModel.Description("")]
		public long PlayerId { get; set; }

		public override void Clear()
		{
			PlayerId = default;
		}
	}

	/// <summary>
	/// 请求开始匹配
	/// </summary>
	[ProtoContract]
	[System.ComponentModel.Description("请求开始匹配")]
	[MessageTypeHandler(((300) << 16) + 23)]
	public sealed class ReqMatchStart : MessageObject, IRequestMessage
	{
		public override void Clear()
		{
		}
	}

	/// <summary>
	/// 请求开始匹配返回
	/// </summary>
	[ProtoContract]
	[System.ComponentModel.Description("请求开始匹配返回")]
	[MessageTypeHandler(((300) << 16) + 24)]
	public sealed class RespMatchStart : MessageObject, IResponseMessage
	{
		[ProtoMember(1)]
		[System.ComponentModel.Description("是否已进入匹配队列")]
		public bool IsMatching { get; set; }

		[ProtoMember(2)]
		[System.ComponentModel.Description("当前匹配队列人数")]
		public int QueueCount { get; set; }

		[ProtoMember(3)]
		[System.ComponentModel.Description("当前房间ID，未匹配成功时为0")]
		public long RoomId { get; set; }

		[ProtoMember(2047)]
		[System.ComponentModel.Description("返回的错误码")]
		public int ErrorCode { get; set; }

		public override void Clear()
		{
			IsMatching = default;
			QueueCount = default;
			RoomId = default;
			ErrorCode = default;
		}
	}

	/// <summary>
	/// 请求取消匹配
	/// </summary>
	[ProtoContract]
	[System.ComponentModel.Description("请求取消匹配")]
	[MessageTypeHandler(((300) << 16) + 25)]
	public sealed class ReqMatchCancel : MessageObject, IRequestMessage
	{
		public override void Clear()
		{
		}
	}

	/// <summary>
	/// 请求取消匹配返回
	/// </summary>
	[ProtoContract]
	[System.ComponentModel.Description("请求取消匹配返回")]
	[MessageTypeHandler(((300) << 16) + 26)]
	public sealed class RespMatchCancel : MessageObject, IResponseMessage
	{
		[ProtoMember(1)]
		[System.ComponentModel.Description("是否取消成功")]
		public bool IsCancelled { get; set; }

		[ProtoMember(2)]
		[System.ComponentModel.Description("当前匹配队列人数")]
		public int QueueCount { get; set; }

		[ProtoMember(2047)]
		[System.ComponentModel.Description("返回的错误码")]
		public int ErrorCode { get; set; }

		public override void Clear()
		{
			IsCancelled = default;
			QueueCount = default;
			ErrorCode = default;
		}
	}

	/// <summary>
	/// 房间玩家信息
	/// </summary>
	[ProtoContract]
	[System.ComponentModel.Description("房间玩家信息")]
	public sealed class MatchRoomPlayerInfo
	{
		[ProtoMember(1)]
		[System.ComponentModel.Description("玩家ID")]
		public long PlayerId { get; set; }

		[ProtoMember(2)]
		[System.ComponentModel.Description("玩家名称")]
		public string Name { get; set; }

		[ProtoMember(3)]
		[System.ComponentModel.Description("玩家等级")]
		public uint Level { get; set; }

		[ProtoMember(4)]
		[System.ComponentModel.Description("玩家头像")]
		public uint Avatar { get; set; }

		[ProtoMember(5)]
		[System.ComponentModel.Description("是否已准备")]
		public bool IsReady { get; set; }
	}

	/// <summary>
	/// 匹配成功通知
	/// </summary>
	[ProtoContract]
	[System.ComponentModel.Description("匹配成功通知")]
	[MessageTypeHandler(((300) << 16) + 27)]
	public sealed class NotifyMatchSuccess : MessageObject, INotifyMessage
	{
		[ProtoMember(1)]
		[System.ComponentModel.Description("房间ID")]
		public long RoomId { get; set; }

		[ProtoMember(2)]
		[System.ComponentModel.Description("房间玩家列表")]
		public List<MatchRoomPlayerInfo> PlayerList { get; set; } = new List<MatchRoomPlayerInfo>();

		public override void Clear()
		{
			RoomId = default;
			PlayerList.Clear();
		}
	}

	/// <summary>
	/// 请求房间准备
	/// </summary>
	[ProtoContract]
	[System.ComponentModel.Description("请求房间准备")]
	[MessageTypeHandler(((300) << 16) + 28)]
	public sealed class ReqMatchRoomReady : MessageObject, IRequestMessage
	{
		[ProtoMember(1)]
		[System.ComponentModel.Description("是否准备")]
		public bool IsReady { get; set; }

		public override void Clear()
		{
			IsReady = default;
		}
	}

	/// <summary>
	/// 请求房间准备返回
	/// </summary>
	[ProtoContract]
	[System.ComponentModel.Description("请求房间准备返回")]
	[MessageTypeHandler(((300) << 16) + 29)]
	public sealed class RespMatchRoomReady : MessageObject, IResponseMessage
	{
		[ProtoMember(1)]
		[System.ComponentModel.Description("房间ID")]
		public long RoomId { get; set; }

		[ProtoMember(2)]
		[System.ComponentModel.Description("当前玩家是否准备")]
		public bool IsReady { get; set; }

		[ProtoMember(3)]
		[System.ComponentModel.Description("房间玩家列表")]
		public List<MatchRoomPlayerInfo> PlayerList { get; set; } = new List<MatchRoomPlayerInfo>();

		[ProtoMember(2047)]
		[System.ComponentModel.Description("返回的错误码")]
		public int ErrorCode { get; set; }

		public override void Clear()
		{
			RoomId = default;
			IsReady = default;
			PlayerList.Clear();
			ErrorCode = default;
		}
	}

	/// <summary>
	/// 房间状态变更通知
	/// </summary>
	[ProtoContract]
	[System.ComponentModel.Description("房间状态变更通知")]
	[MessageTypeHandler(((300) << 16) + 30)]
	public sealed class NotifyMatchRoomChanged : MessageObject, INotifyMessage
	{
		[ProtoMember(1)]
		[System.ComponentModel.Description("房间ID")]
		public long RoomId { get; set; }

		[ProtoMember(2)]
		[System.ComponentModel.Description("房间玩家列表")]
		public List<MatchRoomPlayerInfo> PlayerList { get; set; } = new List<MatchRoomPlayerInfo>();

		public override void Clear()
		{
			RoomId = default;
			PlayerList.Clear();
		}
	}

}

