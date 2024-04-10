using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace HisaCat.PackageDevelopmentTools
{
    public static class PathUtility
    {
        public static string GetProjectPath() => System.IO.Path.GetFullPath(System.IO.Path.Combine(Application.dataPath, "../"));
        public static string GetAssetIOPath(string assetPath) => System.IO.Path.Combine(GetProjectPath(), assetPath);
    }
}