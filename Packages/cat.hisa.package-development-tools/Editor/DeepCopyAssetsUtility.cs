using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Linq;

namespace HisaCat.PackageDevelopmentTools
{
    public class DeepCopyAssetsUtility : EditorWindow
    {
        public static void DeepCopyAssets(IEnumerable<UnityEngine.Object> targetAssets, string saveIOPath, bool overWrite = false)
        {
            foreach (var (targetAsset, i) in targetAssets.Select((value, i) => (value, i)))
                if (targetAsset == null)
                    throw new System.Exception($"DeepCopyAssets assets cannot be null. index: {i}.");
            if (targetAssets.GroupBy(e => e).Any(e => e.Count() > 1))
                throw new System.Exception($"DeepCopyAssets assets array cannot be contains duplicated assets.");
            if (targetAssets.GroupBy(e => System.IO.Path.GetFileName(AssetDatabase.GetAssetPath(e))).Any(e => e.Count() > 1))
                throw new System.Exception($"DeepCopyAssets assets array cannot be contains assets with duplicated name.");

            string assetsFullPath = System.IO.Path.GetFullPath(Application.dataPath).Replace('\\', '/');
            saveIOPath = saveIOPath.Replace('\\', '/');

            #region Validate
            if (saveIOPath.StartsWith(assetsFullPath) == false)
                throw new System.Exception("saveIOPath must be located under the Assets folder of the project.");

            string saveAssetPath = "Assets" + saveIOPath.Substring(assetsFullPath.Length);
            if (AssetDatabase.IsValidFolder(saveAssetPath) == false)
                throw new System.Exception($"\"{saveAssetPath}\" is not valid asset folder.");

            foreach (var targetAsset in targetAssets)
            {
                if (IsPlacedInSameFolder(targetAsset, saveAssetPath, out var newAssetPath))
                    throw new System.Exception($"Asset \"{newAssetPath}\" is placed in selected folder.");
            }

            foreach (var targetAsset in targetAssets)
            {
                if (IsSaveAssetAlreadyExists(targetAsset, saveAssetPath, out var newAssetPath))
                {
                    if (overWrite == false)
                        throw new System.Exception($"Asset \"{newAssetPath}\" already exists.");

                    AssetDatabase.DeleteAsset(newAssetPath);
                    AssetDatabase.Refresh();
                }
            }
            #endregion Validate

            // Clone assets.
            var assetGUIDPair = new List<KeyValuePair<string, string>>();
            foreach (var targetAsset in targetAssets)
            {
                var targetAssetPath = AssetDatabase.GetAssetPath(targetAsset);
                var newAssetPath = System.IO.Path.Combine(saveAssetPath, System.IO.Path.GetFileName(targetAssetPath)).Replace('\\', '/');
                AssetDatabase.CopyAsset(targetAssetPath, newAssetPath);
                AssetDatabase.Refresh();

                assetGUIDPair.Add(new KeyValuePair<string, string>(AssetDatabase.AssetPathToGUID(targetAssetPath), AssetDatabase.AssetPathToGUID(newAssetPath)));
                if (AssetDatabase.IsValidFolder(targetAssetPath))
                {
                    string[] targetSubGUIDs = AssetDatabase.FindAssets("", new[] { targetAssetPath });
                    foreach (string targetSubGUID in targetSubGUIDs)
                    {
                        var targetSubAssetpath = AssetDatabase.GUIDToAssetPath(targetSubGUID).Replace('\\', '/');
                        var newSubAssetpath = newAssetPath + targetSubAssetpath.Substring(targetAssetPath.Length);
                        var newSubGUID = AssetDatabase.AssetPathToGUID(newSubAssetpath);

                        assetGUIDPair.Add(new KeyValuePair<string, string>(targetSubGUID, newSubGUID));
                    }
                }
            }

            // Replace GUIDs.
            foreach (var targetAsset in targetAssets)
            {
                var targetAssetPath = AssetDatabase.GetAssetPath(targetAsset);
                var newAssetPath = System.IO.Path.Combine(saveAssetPath, System.IO.Path.GetFileName(targetAssetPath)).Replace('\\', '/');
                var newIOPath = PathUtility.GetAssetIOPath(newAssetPath).Replace('\\', '/');
                var newMetaIOPath = newIOPath + ".meta";

                void replaceGUID(string path)
                {
                    var text = System.IO.File.ReadAllText(path);
                    bool isAnyGUIDReplaced = false;
                    foreach (var guid in assetGUIDPair)
                    {
                        if (text.Contains(guid.Key))
                        {
                            isAnyGUIDReplaced = true;
                            text = text.Replace(guid.Key, guid.Value);
                            var assetPath = "Assets" + path.Substring(Application.dataPath.Replace('\\', '/').Length);
                            Debug.Log($"\"{assetPath}\"'s referenced GUID \"{guid.Key}\" changed to \"{guid.Value}\"");
                        }
                    };
                    if (isAnyGUIDReplaced) System.IO.File.WriteAllText(path, text);
                }

                // Replace Meta's GUID.
                replaceGUID(newMetaIOPath);

                if (System.IO.Directory.Exists(newIOPath) == false)
                {
                    // Replace Itself GUID.
                    replaceGUID(newIOPath);
                }
                else
                {
                    // Replace sub files GUID.
                    string[] subFileIOPaths = System.IO.Directory.GetFiles(newIOPath, "*.*", System.IO.SearchOption.AllDirectories);
                    foreach (var subFileIOPath in subFileIOPaths)
                    {
                        var subFileAssetPath = "Assets" + subFileIOPath.Substring(Application.dataPath.Replace('\\', '/').Length);
                        replaceGUID(subFileIOPath);
                    }
                }
            }

            AssetDatabase.Refresh();
        }

        #region Validate
        public static bool IsPlacedInSameFolder(UnityEngine.Object targetAsset, string saveAssetPath)
            => IsPlacedInSameFolder(targetAsset, saveAssetPath, out var _);
        public static bool IsPlacedInSameFolder(UnityEngine.Object targetAsset, string saveAssetPath, out string newAssetPath)
        {
            if (targetAsset == null) throw new System.Exception($"IsPlacedInSameFolder cannot be null.");
            var targetAssetPath = AssetDatabase.GetAssetPath(targetAsset);
            newAssetPath = System.IO.Path.Combine(saveAssetPath, System.IO.Path.GetFileName(targetAssetPath)).Replace('\\', '/');
            return targetAssetPath == newAssetPath;
        }
        public static bool IsSaveAssetAlreadyExists(UnityEngine.Object targetAsset, string saveAssetPath)
            => IsSaveAssetAlreadyExists(targetAsset, saveAssetPath, out var _);
        public static bool IsSaveAssetAlreadyExists(UnityEngine.Object targetAsset, string saveAssetPath, out string newAssetPath)
        {
            if (targetAsset == null) throw new System.Exception($"IsSaveAssetAlreadyExists cannot be null.");
            var targetAssetPath = AssetDatabase.GetAssetPath(targetAsset);
            newAssetPath = System.IO.Path.Combine(saveAssetPath, System.IO.Path.GetFileName(targetAssetPath)).Replace('\\', '/');
            return AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(newAssetPath) != null;
        }
        #endregion Validate
    }
}
