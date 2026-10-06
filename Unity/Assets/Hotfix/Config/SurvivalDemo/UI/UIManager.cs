using System.Collections.Generic;
using FairyGUI;
using UnityEngine;

public enum UILayer
{
    Bottom,
    Normal,
    Fixed,
    Pop,
    Popup,
    Top
}

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    // 存储不同层级的容器
    private Dictionary<UILayer, GComponent> _layers = new Dictionary<UILayer, GComponent>();
    // 存储当前打开的窗口
    private Dictionary<string, GComponent> _openedWindows = new Dictionary<string, GComponent>();

    void Awake()
    {
        Instance = this;
        InitLayers();
    }

    private void InitLayers()
    {
        // 按照顺序初始化层级，越往后层级越高
        foreach (UILayer layer in System.Enum.GetValues(typeof(UILayer)))
        {
            GComponent layerComp = new GComponent();
            layerComp.name = layer.ToString();
            GRoot.inst.AddChild(layerComp);
            layerComp.MakeFullScreen(); // 让层级容器铺满全屏
            _layers[layer] = layerComp;
        }
    }

    /// <summary>
    /// 打开 UI 窗口
    /// </summary>
    /// <param name="packageName">包名</param>
    /// <param name="componentName">组件名</param>
    /// <param name="layer">所在层级</param>
    public GComponent OpenUI(string packageName, string componentName, UILayer layer = UILayer.Normal)
    {
        string key = $"{packageName}_{componentName}";
        
        if (_openedWindows.TryGetValue(key, out var opened))
        {
            opened.visible = true;
            return opened;
        }

        // 实际开发中，这里应该先判断包是否加载，没加载则 UIPackage.AddPackage
        GComponent view = UIPackage.CreateObject(packageName, componentName).asCom;
        view.MakeFullScreen();
        
        _layers[layer].AddChild(view);
        _openedWindows[key] = view;
        
        return view;
    }

    public void CloseUI(string packageName, string componentName)
    {
        string key = $"{packageName}_{componentName}";
        if (_openedWindows.TryGetValue(key, out var view))
        {
            view.Dispose(); // 或者只是 visible = false
            _openedWindows.Remove(key);
        }
    }
}
