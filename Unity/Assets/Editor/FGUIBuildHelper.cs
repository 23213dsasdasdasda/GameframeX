using System.IO;
using UnityEditor;

namespace Unity.Editor.Editor // 或者是你报错提示中要求的那个路径
{
    public class FguiBuildHelper
    {
        private const string UIBundlePath = "Assets/Bundles/UI";

        [MenuItem("Tools/FGUI/Clean UI Bundles")]
        public static void CleanUIFolder()
        {
            if (Directory.Exists(UIBundlePath))
            {
                // 删除目录下所有文件及子文件夹
                DirectoryInfo di = new DirectoryInfo(UIBundlePath);
                foreach (FileInfo file in di.GetFiles()) file.Delete();
                foreach (DirectoryInfo dir in di.GetDirectories()) dir.Delete(true);

                AssetDatabase.Refresh(); // 刷新资源数据库
            }
        }
    }
}