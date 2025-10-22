#if UNITY_IOS
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.iOS.Xcode;
using System.IO;

public static class IOSPlistPatch
{
    [PostProcessBuild]
    public static void OnPostProcessBuild(BuildTarget target, string pathToBuiltProject)
    {
        if (target != BuildTarget.iOS) return;

        var plistPath = Path.Combine(pathToBuiltProject, "Info.plist");
        var plist = new PlistDocument();
        plist.ReadFromFile(plistPath);
        var root = plist.root;

        root.SetString("NSMotionUsageDescription", "걸음 수를 측정해 게임 플레이에 사용합니다.");
        root.SetString("NSLocationWhenInUseUsageDescription", "현재 위치의 날씨를 불러오기 위해 필요합니다.");

        File.WriteAllText(plistPath, plist.WriteToString());
    }
}
#endif