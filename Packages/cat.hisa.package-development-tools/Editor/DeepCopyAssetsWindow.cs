using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditorInternal;
using System.Linq;

namespace HisaCat.PackageDevelopmentTools
{
    public class DeepCopyAssetsWindow : EditorWindow
    {
        [MenuItem("Package Development Tools/Deep Copy Asset")]
        public static void Init()
        {
            var window = EditorWindow.GetWindow<DeepCopyAssetsWindow>();
            window.Show();
        }

        private SerializedObject so = null;
        private SerializedProperty targetAssetsProp = null;
        [SerializeField] private List<UnityEngine.Object> targetAssets = new List<UnityEngine.Object>();
        private ReorderableList targetAssetsReorderableList;
        private void OnEnable()
        {
            this.titleContent.text = "Deep Copy Asset";
            //this.titleContent.image = ImageDrawer.WindowIconImage;

            ScriptableObject target = this;
            this.so = new SerializedObject(target);
            this.targetAssetsProp = so.FindProperty(nameof(this.targetAssets));
            this.targetAssetsReorderableList = new ReorderableList(this.so, this.targetAssetsProp, true, true, true, true);
            this.targetAssetsReorderableList.drawHeaderCallback = (Rect rect) =>
            {
                EditorGUI.LabelField(rect, "Assets");
            };
            this.targetAssetsReorderableList.drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) =>
            {
                SerializedProperty element = this.targetAssetsReorderableList.serializedProperty.GetArrayElementAtIndex(index);
                EditorGUI.PropertyField(rect, element, GUIContent.none);
            };
        }

        private static string savePath = null;
        private void OnGUI()
        {
            EditorGUI.BeginChangeCheck();
            so.Update();
            {
                this.targetAssetsReorderableList.DoLayoutList();
            }
            so.ApplyModifiedProperties();
            if (EditorGUI.EndChangeCheck())
            {
                // Remove duplicated assets
                var duplicatedAssets = this.targetAssets.Select((asset, i) => (asset, i))
                    .Where(e => e.asset != null)
                    .GroupBy(e => e.asset).Where(e => e.Count() > 1);
                foreach (var group in duplicatedAssets)
                {
                    for (int i = 1; i < group.Count(); i++)
                        this.targetAssets[group.ElementAt(i).i] = null;
                }

                // Remove same named assets.
                bool duplicatedNameAssetsRemoved = false;
                var duplicatedNameAssets = this.targetAssets.Select((asset, i) => (asset, i))
                    .Where(e => e.asset != null)
                    .GroupBy(e => System.IO.Path.GetFileName(AssetDatabase.GetAssetPath(e.asset))).Where(e => e.Count() > 1);
                foreach (var group in duplicatedNameAssets)
                {
                    for (int i = 1; i < group.Count(); i++)
                    {
                        this.targetAssets[group.ElementAt(i).i] = null;
                        duplicatedNameAssetsRemoved = true;
                    }
                }
                if (duplicatedNameAssetsRemoved)
                    EditorUtility.DisplayDialog("Deep Copy Asset", "Deep Copy Asset does not supports copy assets with duplicated name.\r\nPlease rename asset and do it again.", "Ok");

                so.Update();
            }

            GUILayout.Space(25);

            EditorGUI.BeginDisabledGroup(targetAssets.Where(e => e != null).Count() <= 0);
            {
                if (GUILayout.Button("Deep Copy Assets", GUILayout.Height(50)))
                {
                    if (EditorSettings.serializationMode != SerializationMode.ForceText)
                    {
                        if (EditorUtility.DisplayDialog("Deep Copy Assets", "Only works serializationMode is Force Text. change now?.", "Yes", "No") == false)
                            return;

                        EditorSettings.serializationMode = SerializationMode.ForceText;
                        AssetDatabase.Refresh();
                    }

                    if (string.IsNullOrEmpty(savePath)) savePath = "Assets";
                    savePath = EditorUtility.SaveFolderPanel("Select Clone Path", savePath, "");
                    AssetDatabase.Refresh();
                    if (string.IsNullOrEmpty(savePath)) return;

                    string assetsFullPath = System.IO.Path.GetFullPath(Application.dataPath).Replace('\\', '/');
                    savePath = savePath.Replace('\\', '/');

                    void execute()
                    {
                        if (savePath.StartsWith(assetsFullPath) == false)
                        {
                            EditorUtility.DisplayDialog("Deep Copy Assets", "Please select in project assets path.", "Ok");
                            return;
                        }

                        string saveAssetPath = "Assets" + savePath.Substring(assetsFullPath.Length);
                        if (AssetDatabase.IsValidFolder(saveAssetPath) == false)
                        {
                            Debug.LogError($"{saveAssetPath} is not valid folder.");
                            return;
                        }

                        var _targetAssets = this.targetAssets.Where(e => e != null).ToList();

                        // Check placed in same folder.
                        foreach (var targetAsset in _targetAssets)
                        {
                            if (DeepCopyAssetsUtility.IsPlacedInSameFolder(targetAsset, saveAssetPath, out var newAssetPath))
                            {
                                EditorUtility.DisplayDialog("Deep Copy Assets", $"Asset \"{newAssetPath}\" is placed in selected folder.\r\nPlease select other folder.", "Ok");
                                return;
                            }
                        }

                        // Check Asset already exists.
                        foreach (var targetAsset in _targetAssets)
                        {
                            if (DeepCopyAssetsUtility.IsSaveAssetAlreadyExists(targetAsset, saveAssetPath, out var newAssetPath))
                            {
                                if (EditorUtility.DisplayDialog("Deep Copy Assets", $"Asset \"{newAssetPath}\" already exists. overwrite it?", "Yes", "No") == false)
                                    return;
                            }
                        }

                        DeepCopyAssetsUtility.DeepCopyAssets(_targetAssets, savePath, overWrite: true);
                        EditorUtility.DisplayDialog("Deep Copy Assets", "Done.", "Ok");
                    }

                    execute();
                }
            }
            EditorGUI.EndDisabledGroup();
        }

    }
}
