#if UNITY_IOS
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.iOS.Xcode;
using System.IO;

public static class IOSFrameworkPatch
{
    [PostProcessBuild(1)]
    public static void AddFrameworks(BuildTarget target, string path)
    {
        if (target != BuildTarget.iOS) return;

        string projPath = PBXProject.GetPBXProjectPath(path);
        var proj = new PBXProject();
        proj.ReadFromFile(projPath);

        string targetGuid =
#if UNITY_2019_3_OR_NEWER
            proj.GetUnityFrameworkTargetGuid();
#else
            proj.TargetGuidByName(PBXProject.GetUnityTargetName());
#endif

        proj.AddFrameworkToProject(targetGuid, "CoreMotion.framework", false); // false=required
        File.WriteAllText(projPath, proj.WriteToString());
    }
}
#endif