/** This file follows the local FairyGUI generated binding pattern. **/

#if ENABLE_UI_FAIRYGUI
using FairyGUI;
using Cysharp.Threading.Tasks;
using FairyGUI.Utils;
using GameFrameX.Entity.Runtime;
using GameFrameX.UI.Runtime;
using GameFrameX.Runtime;
using GameFrameX.UI.FairyGUI.Runtime;
using UnityEngine;

namespace Hotfix.UI
{
    public sealed partial class UI3Pick1 : FUI
    {
        public const string UIPackageName = "UI3Pick1";
        public const string UIResName = "UI3Pick1";
        public const string URL = "ui://t2ftidd06dus5";

        public GComponent self { get; private set; }

        public GTextField m_option_title1 { get; private set; }
        public GTextField m_option_title2 { get; private set; }
        public GTextField m_option_title3 { get; private set; }
        public GTextField m_option_desc1 { get; private set; }
        public GTextField m_option_desc2 { get; private set; }
        public GTextField m_option_desc3 { get; private set; }
        public GButton m_option_action_btn1 { get; private set; }
        public GButton m_option_action_btn2 { get; private set; }
        public GButton m_option_action_btn3 { get; private set; }
        public GImage m_option_btn1 { get; private set; }
        public GImage m_option_btn2 { get; private set; }
        public GImage m_option_btn3 { get; private set; }

        private static GObject CreateGObject()
        {
            return UIPackage.CreateObject(UIPackageName, UIResName);
        }

        private static void CreateGObjectAsync(UIPackage.CreateObjectCallback result)
        {
            UIPackage.CreateObjectAsync(UIPackageName, UIResName, result);
        }

        public static UI3Pick1 CreateInstance()
        {
            return Create(CreateGObject());
        }

        public static UniTask<UI3Pick1> CreateInstanceAsync(Entity domain)
        {
            UniTaskCompletionSource<UI3Pick1> tcs = new UniTaskCompletionSource<UI3Pick1>();
            CreateGObjectAsync((go) =>
            {
                tcs.TrySetResult(Create(go));
            });
            return tcs.Task;
        }

        public static UI3Pick1 Create(GObject go)
        {
            var fui = go.displayObject.gameObject.GetOrAddComponent<UI3Pick1>();
            fui?.SetGObject(go);
            fui?.InitView();
            return fui;
        }

        public static UI3Pick1 GetFormPool(GObject go)
        {
            var fui = go.Get<UI3Pick1>();
            if (fui == null)
            {
                fui = Create(go);
            }

            fui.IsFromPool = true;
            return fui;
        }

        protected override void InitView()
        {
            if (GObject == null)
            {
                return;
            }

            self = (GComponent)GObject;
            self.Add(this);

            var com = GObject.asCom;
            if (com == null)
            {
                return;
            }

            m_option_title1 = com.GetChild("option_title1") as GTextField;
            m_option_title2 = com.GetChild("option_title2") as GTextField;
            m_option_title3 = com.GetChild("option_title3") as GTextField;
            m_option_desc1 = com.GetChild("option_desc1") as GTextField;
            m_option_desc2 = com.GetChild("option_desc2") as GTextField;
            m_option_desc3 = com.GetChild("option_desc3") as GTextField;
            m_option_action_btn1 = com.GetChild("option_action_btn1") as GButton;
            m_option_action_btn2 = com.GetChild("option_action_btn2") as GButton;
            m_option_action_btn3 = com.GetChild("option_action_btn3") as GButton;
            m_option_btn1 = com.GetChild("option_btn1") as GImage;
            m_option_btn2 = com.GetChild("option_btn2") as GImage;
            m_option_btn3 = com.GetChild("option_btn3") as GImage;
        }

        public override void Dispose()
        {
            if (IsDisposed)
            {
                return;
            }

            base.Dispose();
            self?.Remove();
            self = null;
            m_option_title1 = null;
            m_option_title2 = null;
            m_option_title3 = null;
            m_option_desc1 = null;
            m_option_desc2 = null;
            m_option_desc3 = null;
            m_option_action_btn1 = null;
            m_option_action_btn2 = null;
            m_option_action_btn3 = null;
            m_option_btn1 = null;
            m_option_btn2 = null;
            m_option_btn3 = null;
        }

        private UI3Pick1(GObject gObject) : base(gObject)
        {
        }
    }
}
#endif
