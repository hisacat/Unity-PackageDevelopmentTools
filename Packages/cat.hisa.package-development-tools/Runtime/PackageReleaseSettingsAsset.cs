using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace HisaCat.PackageDevelopmentTools
{
    [CreateAssetMenu(menuName = "Package Development Tools/Create Package Release Settings", fileName = "Package Release Settings")]
    public class PackageReleaseSettingsAsset : ScriptableObject
    {
        [SerializeField] private TextAsset m_PackageManifest = null;
        public TextAsset PackageManifest
        {
            get => m_PackageManifest;
            set => this.m_PackageManifest = value;
        }

        [SerializeField] private BoothFile[] m_BoothFiles = null;
        public BoothFile[] BoothFiles
        {
            get => m_BoothFiles;
            set => this.m_BoothFiles = value;
        }

        [System.Serializable]
        public class BoothFile
        {
            [SerializeField] private UnityEngine.Object m_File = null;
            public UnityEngine.Object File
            {
                get => m_File;
                set => this.m_File = value;
            }
            [SerializeField] private string m_ChangeName = null;
            public string ChangeName
            {
                get => m_ChangeName;
                set => this.m_ChangeName = value;
            }
        }

        [SerializeField] private UnityEngine.Object m_AssetRootFolderForUnitypackage = null;
        public UnityEngine.Object AssetRootFolderForUnitypackage
        {
            get => m_AssetRootFolderForUnitypackage;
            set => this.m_AssetRootFolderForUnitypackage = value;
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (this.m_PackageManifest != null)
            {
                if (System.IO.Path.GetFileName(AssetDatabase.GetAssetPath(this.m_PackageManifest)).Equals("package.json", System.StringComparison.OrdinalIgnoreCase))
                    this.m_PackageManifest = null;
            }
        }
#endif
    }
}
