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

using System;
using ProtoBuf;
using System.Collections.Generic;
using GameFrameX.Network.Runtime;

namespace Hotfix.Proto
{
	/// <summary>
	/// 战斗输入上报
	/// </summary>
	[ProtoContract]
	[MessageTypeHandler(((400) << 16) + 10)]
	public sealed class ReqBattleInput : MessageObject, IRequestMessage
	{
		/// <summary>
		/// 输入方向 X
		/// </summary>
		[ProtoMember(1)]
		public float MoveX { get; set; }

		/// <summary>
		/// 输入方向 Y
		/// </summary>
		[ProtoMember(2)]
		public float MoveY { get; set; }

		/// <summary>
		/// 当前朝向（Y轴角度）
		/// </summary>
		[ProtoMember(3)]
		public float Yaw { get; set; }

		/// <summary>
		/// 是否跳跃
		/// </summary>
		[ProtoMember(4)]
		public bool Jump { get; set; }

		/// <summary>
		/// 是否冲刺
		/// </summary>
		[ProtoMember(5)]
		public bool Sprint { get; set; }

		/// <summary>
		/// 输入序号
		/// </summary>
		[ProtoMember(6)]
		public uint Seq { get; set; }


		public override void Clear()
		{
			MoveX = default;
			MoveY = default;
			Yaw = default;
			Jump = default;
			Sprint = default;
			Seq = default;
		}
	}

	/// <summary>
	/// 单个玩家的战斗快照
	/// </summary>
	[ProtoContract]
	public sealed class BattlePlayerState
	{
		/// <summary>
		/// 玩家ID
		/// </summary>
		[ProtoMember(1)]
		public long PlayerId { get; set; }

		/// <summary>
		/// 位置X
		/// </summary>
		[ProtoMember(2)]
		public float PositionX { get; set; }

		/// <summary>
		/// 位置Y
		/// </summary>
		[ProtoMember(3)]
		public float PositionY { get; set; }

		/// <summary>
		/// 位置Z
		/// </summary>
		[ProtoMember(4)]
		public float PositionZ { get; set; }

		/// <summary>
		/// 朝向Y
		/// </summary>
		[ProtoMember(5)]
		public float RotationY { get; set; }

		/// <summary>
		/// 速度X
		/// </summary>
		[ProtoMember(6)]
		public float VelocityX { get; set; }

		/// <summary>
		/// 速度Y
		/// </summary>
		[ProtoMember(7)]
		public float VelocityY { get; set; }

		/// <summary>
		/// 速度Z
		/// </summary>
		[ProtoMember(8)]
		public float VelocityZ { get; set; }

		/// <summary>
		/// 是否在地面
		/// </summary>
		[ProtoMember(9)]
		public bool Grounded { get; set; }

		/// <summary>
		/// 服务端已处理的最新输入序号
		/// </summary>
		[ProtoMember(10)]
		public uint Seq { get; set; }

	}

	/// <summary>
	/// 服务端广播的房间战斗快照
	/// </summary>
	[ProtoContract]
	[MessageTypeHandler(((400) << 16) + 11)]
	public sealed class NotifyBattleSnapshot : MessageObject, INotifyMessage
	{
		/// <summary>
		/// 房间ID
		/// </summary>
		[ProtoMember(1)]
		public long RoomId { get; set; }

		/// <summary>
		/// 服务端逻辑帧
		/// </summary>
		[ProtoMember(2)]
		public ulong ServerTick { get; set; }

		/// <summary>
		/// 房间内玩家状态
		/// </summary>
		[ProtoMember(3)]
		public List<BattlePlayerState> PlayerStates { get; set; } = new List<BattlePlayerState>();


		public override void Clear()
		{
			RoomId = default;
			ServerTick = default;
			PlayerStates.Clear();
		}
	}

	/// <summary>
	/// 玩家进入战斗广播
	/// </summary>
	[ProtoContract]
	[MessageTypeHandler(((400) << 16) + 12)]
	public sealed class NotifyBattlePlayerJoin : MessageObject, INotifyMessage
	{
		/// <summary>
		/// 房间ID
		/// </summary>
		[ProtoMember(1)]
		public long RoomId { get; set; }

		/// <summary>
		/// 新进入玩家状态
		/// </summary>
		[ProtoMember(2)]
		public BattlePlayerState PlayerState { get; set; }


		public override void Clear()
		{
			RoomId = default;
			PlayerState = default;
		}
	}

	/// <summary>
	/// 玩家离开战斗广播
	/// </summary>
	[ProtoContract]
	[MessageTypeHandler(((400) << 16) + 13)]
	public sealed class NotifyBattlePlayerLeave : MessageObject, INotifyMessage
	{
		/// <summary>
		/// 房间ID
		/// </summary>
		[ProtoMember(1)]
		public long RoomId { get; set; }

		/// <summary>
		/// 离开玩家ID
		/// </summary>
		[ProtoMember(2)]
		public long PlayerId { get; set; }


		public override void Clear()
		{
			RoomId = default;
			PlayerId = default;
		}
	}

}
