using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Linq;

namespace HisaCat.PackageDevelopmentTools
{
    public static class AssetGUIDHelper
    {
        private const string MenuItemName = "Assets/Copy GUID";

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
                var assetName = System.IO.Path.GetFileName(AssetDatabase.GUIDToAssetPath(guid));
                GUIUtility.systemCopyBuffer = clipboard = assetGUIDs[0];
                Debug.Log($"GUID \"<b>{guid}</b>\" for asset \"{assetName}\" copied!");
            }
            else
            {
                GUIUtility.systemCopyBuffer = clipboard = string.Join("\r\n", assetGUIDs.Select(e => $"{System.IO.Path.GetFileName(AssetDatabase.GUIDToAssetPath(e))}: {e}"));
                Debug.Log($"Multiple GUID copied:\r\n{clipboard}");
            }

            return clipboard;
        }

        public static void ChangeGUID(string assetPath, string guid, bool refreshAssetDatabase = true)
        {
            var oldGUID = AssetDatabase.AssetPathToGUID(assetPath);
            var newGUID = guid;
            if (oldGUID != newGUID)
            {
                string metaPath = $"{assetPath}.meta";
                if (System.IO.File.Exists(metaPath) == false)
                {
                    Debug.LogError($"Failed to change GUID. meta file not exists: \"{assetPath}\"");
                }
                else
                {
                    string metaContent = System.IO.File.ReadAllText(metaPath);
                    metaContent = metaContent.Replace(oldGUID, newGUID);
                    System.IO.File.WriteAllText(metaPath, metaContent);

                    if (refreshAssetDatabase)
                        AssetDatabase.Refresh();
                }
            }
        }
    }
}
