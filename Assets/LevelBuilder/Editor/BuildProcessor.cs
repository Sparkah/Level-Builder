using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;

public class BuildProcessor : IPreprocessBuildWithReport, IPostprocessBuildWithReport
{
    public int callbackOrder => 0;

    public void OnPreprocessBuild(BuildReport report)
    {
        if (report.summary.platform == BuildTarget.Android)
        {
            if (Path.GetExtension(report.summary.outputPath) == ".aab")
            {
                AddDefineSymbol("UNITY_AAB");
            }
            else
            {
                RemoveDefineSymbol("UNITY_AAB");
            }
        }
    }

    public void OnPostprocessBuild(BuildReport report)
    {
        if (report.summary.platform == BuildTarget.Android)
        {
            RemoveDefineSymbol("UNITY_AAB");
        }
    }

    private void AddDefineSymbol(string symbol)
    {
        var buildTargetGroup = EditorUserBuildSettings.selectedBuildTargetGroup;
        var defines = PlayerSettings.GetScriptingDefineSymbolsForGroup(buildTargetGroup);
        if (!defines.Contains(symbol))
        {
            defines += ";" + symbol;
            PlayerSettings.SetScriptingDefineSymbolsForGroup(buildTargetGroup, defines);
        }
    }

    private void RemoveDefineSymbol(string symbol)
    {
        var buildTargetGroup = EditorUserBuildSettings.selectedBuildTargetGroup;
        var defines = PlayerSettings.GetScriptingDefineSymbolsForGroup(buildTargetGroup);
        if (defines.Contains(symbol))
        {
            defines = defines.Replace(symbol, "");
            defines = defines.Replace(";;", ";");
            PlayerSettings.SetScriptingDefineSymbolsForGroup(buildTargetGroup, defines);
        }
    }
}