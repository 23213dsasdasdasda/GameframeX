using FairyGUI;
using UnityEngine;

namespace GameframeX.UI
{
    public static class NoticeDispatcher
    {
        /// <summary>
        /// 全局通知入口：由服务端协议或本地计时器触发
        /// </summary>
        public static void DispatchNotify(string type, object data = null)
        {
            switch (type)
            {
                case "StrengthFull":
                    ShowToast("报告指挥官，体力已回满！");
                    break;
                    
                case "SeasonUpdate":
                    ShowToast("新赛季开启，快去看看吧！");
                    // 这里可以触发主界面 UI 刷新红点
                    // MainHomeController.Instance.RefreshRedDots();
                    break;

                case "ItemObtained":
                    ShowToast($"获得特殊奖励: {data}");
                    break;
            }
        }

        private static void ShowToast(string msg)
        {
            // 注意：Toast 通常放在 UILayer.Top 保证在最前面
            var toast = UIManager.Instance.OpenUI("Common", "Toast", UILayer.Top);
            if (toast != null)
            {
                toast.GetChild("title").text = msg;
                // 播放 FairyGUI 编辑器里做好的消失动效
                var trans = toast.GetTransition("t0");
                if (trans != null)
                {
                    trans.Play(() => toast.Dispose());
                }
                else
                {
                    // 没做动效就2秒后销毁
                    Timers.inst.Add(2.0f, 1, (obj) => toast.Dispose());
                }
            }
        }
    }
}