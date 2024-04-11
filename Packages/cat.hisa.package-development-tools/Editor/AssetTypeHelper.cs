using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Linq;

namespace HisaCat.PackageDevelopmentTools
{
    public static class AssetTypeHelper
    {
        private const string MenuItemName = "Assets/Asset Type/Copy Asset Type";

        [MenuItem(MenuItemName, validate = true)]
        public static bool CopySelectedAssetsGUIDValidate() => Selection.assetGUIDs != null && Selection.assetGUIDs.Length > 0;

        [MenuItem(MenuItemName)]
        public static string CopySelectedAssetsGUID()
        {
            var assetGUIDs = Selection.assetGUIDs;
            if (assetGUIDs == null || assetGUIDs.Length <= 0)
            {
                Debug.LogError($"[{nameof(AssetGUIDHelper)}] Selection is empty.");
                return null;
            }

            var clipboard = "";
            if (assetGUIDs.Length == 1)
            {
                var guid = assetGUIDs[0];
                var assetPath = AssetDatabase.GUIDToAssetPath(guid);
                var assetName = System.IO.Path.GetFileName(assetPath);
                var assetType = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(assetPath).GetType();
                GUIUtility.systemCopyBuffer = clipboard = assetGUIDs[0];
                Debug.Log($"Asset Type \"<b>{assetType.FullName}</b>\" for asset \"{assetName}\" copied!");
            }
            else
            {
                GUIUtility.systemCopyBuffer = clipboard = string.Join("\r\n", assetGUIDs.Select(guid =>
                {
                    var assetPath = AssetDatabase.GUIDToAssetPath(guid);
                    var assetType = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(assetPath).GetType();
                    return $"{System.IO.Path.GetFileName(AssetDatabase.GUIDToAssetPath(guid))}: {assetType.FullName}";
                }));
                Debug.Log($"Multiple Asset type copied:\r\n{clipboard}");
            }

            return clipboard;
        }
    }
}
