using System;
using System.Collections.Generic;
using FairyGUI;
using GameframeX.Config;
using UnityEngine;

public class UIDispatcher : MonoBehaviour
{
    public static UIDispatcher Instance;
    
    // 等待弹出的队列
    private Queue<SysNotifyConfig> _popupQueue = new Queue<SysNotifyConfig>();
    private bool _isPopupActive = false;

    void Awake() => Instance = this;

    /// <summary>
    /// 核心逻辑：开始执行登录后的“弹窗轰炸”序列
    /// </summary>
    public void StartLoginSequence()
    {
        _popupQueue.Clear();
        // 从配置总线获取已排序的弹窗列表
        var configs = GameConfigBus.UILib.Popups;
        foreach (var config in configs)
        {
            _popupQueue.Enqueue(config);
        }
        ShowNextPopup();
    }

    private void ShowNextPopup()
    {
        if (_popupQueue.Count == 0)
        {
            _isPopupActive = false;
            Debug.Log("所有序章弹窗已处理完毕");
            return;
        }

        _isPopupActive = true;
        var current = _popupQueue.Dequeue();
        
        // 调用 UIManager 加载对应的 FairyGUI 组件
        GComponent view = UIManager.Instance.OpenUI("Popups", current.PrefabName, UILayer.Popup);
        
        // 这里的关键：给弹窗的“关闭按钮”绑定逻辑，关闭后再弹下一个
        var closeBtn = view.GetChild("btn_close");
        if (closeBtn != null)
        {
            closeBtn.onClick.Set(() => {
                UIManager.Instance.CloseUI("Popups", current.PrefabName);
                ShowNextPopup(); // 递归调用，弹下一个
            });
        }
    }
}
