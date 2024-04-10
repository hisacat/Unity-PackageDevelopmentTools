using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO.Compression;

namespace HisaCat.PackageDevelopmentTools
{
    public static class PackageReleaseManager
    {
        private const string DirectoryKey = "HisaCat.PackageDevelopmentTools.ReleaseManager.SaveDir";
        public static string SaveDir
        {
            get => EditorPrefs.GetString(DirectoryKey);
            set => EditorPrefs.SetString(DirectoryKey, value);
        }

        public static void ExportReleases(PackageReleaseSettingsAsset releaseSettings)
        {
            var dict = Json.Deserialize(releaseSettings.PackageManifest.text) as Dictionary<string, object>;
            var packageVersion = dict["version"].ToString();
            var packageName = dict["name"].ToString();

            var releaseFileName = $"{packageName}-v{packageVersion}";

            var packageRootFolderAssetPath = System.IO.Path.GetDirectoryName(AssetDatabase.GetAssetPath(releaseSettings.PackageManifest));
            var packageRootFolderIOPath = PathUtility.GetAssetIOPath(packageRootFolderAssetPath);

            var assetRootFolderAssetPath = AssetDatabase.GetAssetPath(releaseSettings.AssetRootFolderForUnitypackage);
            var assetRootFolderIOPath = PathUtility.GetAssetIOPath(assetRootFolderAssetPath);

            var saveDir = EditorUtility.SaveFolderPanel($"Save Release {releaseFileName}", string.IsNullOrEmpty(SaveDir) ? null : System.IO.Path.GetDirectoryName(SaveDir), null);
            if (string.IsNullOrEmpty(saveDir)) return;
            SaveDir = saveDir;

            var zipPath = System.IO.Path.Combine(saveDir, $"{releaseFileName}.zip");
            var unitypackagePath = System.IO.Path.Combine(saveDir, $"{releaseFileName}.unitypackage");

            // Copy package.json
            var packageManifestIOPath = PathUtility.GetAssetIOPath(AssetDatabase.GetAssetPath(releaseSettings.PackageManifest));
            System.IO.File.Copy(packageManifestIOPath, System.IO.Path.Combine(saveDir, System.IO.Path.GetFileName(packageManifestIOPath)), true);

            // Create zip (VCC Package) release.
            {
                if (System.IO.File.Exists(zipPath))
                {
                    if (EditorUtility.DisplayDialog($"Release Package", "zip already exists. overwrite it?", "Ok") == false)
                        return;
                }
                System.IO.File.Delete(zipPath);

                ArchiveFilesInDirectoryAsZip(packageRootFolderIOPath, zipPath);
            }

            // Create unitypackage release
            {
                if (System.IO.File.Exists(unitypackagePath))
                {
                    if (EditorUtility.DisplayDialog($"Release Package", "unitypackage already exists. overwrite it?", "Ok") == false)
                        return;
                }
                System.IO.File.Delete(unitypackagePath);

                void moveAllAssets(string from, string to)
                {
                    var fromIOPath = PathUtility.GetAssetIOPath(from);
                    var toOPath = PathUtility.GetAssetIOPath(to);

                    string[] files = System.IO.Directory.GetFiles(from);
                    foreach (string file in files)
                    {
                        string fileName = System.IO.Path.GetFileName(file);
                        if (fileName.StartsWith(".")) continue;

                        string destFile = System.IO.Path.Combine(toOPath, fileName);
                        if (System.IO.File.Exists(destFile))
                            throw new System.Exception($"File already exists: {destFile}");

                        System.IO.File.Move(file, destFile);
                    }

                    string[] folders = System.IO.Directory.GetDirectories(fromIOPath);
                    foreach (string folder in folders)
                    {
                        string folderName = System.IO.Path.GetFileName(folder);
                        string destFolder = System.IO.Path.Combine(toOPath, folderName);

                        if (System.IO.Directory.Exists(destFolder))
                            throw new System.Exception($"Directory already exists: {destFolder}");

                        System.IO.Directory.Move(folder, destFolder);
                    }
                    AssetDatabase.Refresh();
                }

                // Move files into Assets
                moveAllAssets(packageRootFolderAssetPath, assetRootFolderAssetPath);

                // Export unitypackage
                AssetDatabase.ExportPackage(assetRootFolderAssetPath, unitypackagePath, ExportPackageOptions.Recurse | ExportPackageOptions.IncludeDependencies);
                Debug.Log($"unitypackage exported: {unitypackagePath}");

                // Revert files that moved to Assets to the Package path.
                moveAllAssets(assetRootFolderAssetPath, packageRootFolderAssetPath);

                // Copy Booth release files.
                {
                    var boothDir = System.IO.Path.Combine(saveDir, "Booth");
                    if (System.IO.Directory.Exists(boothDir) == false)
                        System.IO.Directory.CreateDirectory(boothDir);

                    // Copy unitypackage file
                    System.IO.File.Copy(unitypackagePath, System.IO.Path.Combine(boothDir, System.IO.Path.GetFileName(unitypackagePath)));

                    foreach (var boothFile in releaseSettings.BoothFiles)
                    {
                        var boothFileAssetPath = AssetDatabase.GetAssetPath(boothFile.File);
                        var destIOPath = boothFileAssetPath;
                        if (string.IsNullOrEmpty(boothFile.ChangeName))
                            destIOPath = System.IO.Path.GetFileName(boothFileAssetPath);
                        else
                            destIOPath = boothFile.ChangeName;
                        destIOPath = System.IO.Path.Combine(boothDir, destIOPath);

                        System.IO.File.Copy(PathUtility.GetAssetIOPath(boothFileAssetPath), destIOPath, true);
                    }

                    ArchiveFilesInDirectoryAsZip(boothDir, System.IO.Path.Combine(boothDir, $"{releaseFileName}.zip"));
                }

                Debug.Log($"{releaseFileName} exported.");
                System.Diagnostics.Process.Start(saveDir);
            }
        }

        private static void ArchiveFilesInDirectoryAsZip(string rootDirectoryPath, string zipPath)
        {
            using (var memoryStream = new System.IO.MemoryStream())
            {
                using (var archive = new ZipArchive(memoryStream, ZipArchiveMode.Create, true))
                {
                    var dirInfo = new System.IO.DirectoryInfo(rootDirectoryPath);
                    foreach (var file in dirInfo.GetFiles("*", System.IO.SearchOption.AllDirectories))
                    {
                        string pathInArchive = file.FullName.Substring(dirInfo.FullName.Length + 1);
                        var entry = archive.CreateEntry(pathInArchive);
                        using (var entryStream = entry.Open())
                        using (var fileStream = System.IO.File.OpenRead(file.FullName))
                        {
                            fileStream.CopyTo(entryStream);
                        }
                    }
                }

                using (var fileStream = new System.IO.FileStream(zipPath, System.IO.FileMode.Create))
                {
                    memoryStream.Seek(0, System.IO.SeekOrigin.Begin);
                    memoryStream.CopyTo(fileStream);
                }
                Debug.Log($"zip exported: {zipPath}");
            }
        }
    }
}
