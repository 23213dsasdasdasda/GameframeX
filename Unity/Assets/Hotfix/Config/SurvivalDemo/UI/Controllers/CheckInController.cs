using FairyGUI;
using System.Collections.Generic;
using GameframeX.Config;
using UnityEngine;

namespace GameframeX.UI
{
    public class CheckInController : MonoBehaviour
    {
        private GComponent _view;
        private GList _list;
        private List<CheckInConfig> _data;

        public void BindView(GComponent view)
        {
            _view = view;
            _list = _view.GetChild("list_days").asList;

            // 1. 获取硬编码的30天配置
            _data = GameConfigBus.UILib.CheckInDays;

            // 2. 设置渲染函数 (类似回调)
            _list.itemRenderer = RenderListItem;
            _list.numItems = _data.Count; // 这一行会触发渲染
        }

        private void RenderListItem(int index, GObject obj)
        {
            GComponent item = obj.asCom;
            CheckInConfig config = _data[index];

            // 设置天数文本
            item.GetChild("txt_day").text = $"第{config.Day}天";
            
            // 设置物品Icon (这里对应你FairyGUI里的装载器)
            item.GetChild("icon").asLoader.url = $"ui://Icons/Item_{config.ItemId}";
            item.GetChild("txt_count").text = "x" + config.Count;

            // 状态控制 (利用FairyGUI的Controller切换：0-未领, 1-可领, 2-已领)
            var statusCtrl = item.GetController("status");
            
            // 这里的逻辑根据你服务器传回的已签到天数判断
            int signedDays = 3; // 假数据
            if (config.Day <= signedDays) 
                statusCtrl.selectedIndex = 2; // 已领
            else if (config.Day == signedDays + 1)
                statusCtrl.selectedIndex = 1; // 可领
            else
                statusCtrl.selectedIndex = 0; // 不可领

            // 点击领取
            item.onClick.Set(() => {
                if(statusCtrl.selectedIndex == 1)
                {
                    Debug.Log($"领取了第{config.Day}天奖励");
                    // 逻辑：发协议给后端，后端回包后再刷新UI
                }
            });
        }
    }
}