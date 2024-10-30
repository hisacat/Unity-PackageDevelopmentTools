using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace HisaCat.PackageDevelopmentTools
{
    [CustomEditor(typeof(PackageReleaseSettingsAsset))]
    [DisallowMultipleComponent]
    public class PackageReleaseSettingsAssetEditor : Editor
    {
        private SerializedProperty m_PackageManifest;
        private SerializedProperty m_BoothFiles;
        private SerializedProperty m_AssetRootFolderForUnitypackage;
        private void OnEnable()
        {
            this.m_PackageManifest = this.serializedObject.FindProperty(nameof(this.m_PackageManifest));
            this.m_BoothFiles = this.serializedObject.FindProperty(nameof(this.m_BoothFiles));
            this.m_AssetRootFolderForUnitypackage = this.serializedObject.FindProperty(nameof(this.m_AssetRootFolderForUnitypackage));
        }

        public override void OnInspectorGUI()
        {
            this.serializedObject.Update();
            {
                EditorGUILayout.PropertyField(this.m_PackageManifest);
                EditorGUILayout.PropertyField(this.m_BoothFiles);
                EditorGUILayout.HelpBox("This is the list of files to be included in the .zip file for release on Booth.", MessageType.Info);
                EditorGUILayout.Space(EditorGUIUtility.singleLineHeight);
                EditorGUILayout.PropertyField(this.m_AssetRootFolderForUnitypackage);
            }
            this.serializedObject.ApplyModifiedProperties();

            EditorGUILayout.Space(EditorGUIUtility.singleLineHeight);
            if (GUILayout.Button("Export Releases", GUILayout.Height(50)))
            {
                PackageReleaseManager.ExportReleases(this.target as PackageReleaseSettingsAsset);
            }
        }
    }
}
