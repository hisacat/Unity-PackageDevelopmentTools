using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace HisaCat.PackageDevelopmentTools
{
    public static class GameObjectPathUtility
    {
        [MenuItem("GameObject/Copy GameObject Path", false, 10)]
        public static void CopyGameObjectPath()
        {
            var go = Selection.activeGameObject;
            if (go == null) return;
            var path = GetGameObjectPath(go.transform);
            EditorGUIUtility.systemCopyBuffer = path;

            Debug.Log($"GameObject Path \"<b>{path}</b>\" copied!");
        }
        private static string GetGameObjectPath(Transform obj)
        {
            if (obj == null)
                return string.Empty;

            string path = obj.name;

            Transform rootObj = null;
            {
                var _obj = obj;
                while (_obj.transform.parent != null)
                {
                    var parent = _obj.parent;
                    _obj = parent;
                    if (_obj.GetComponent<Animator>() != null) rootObj = _obj;
                    if (_obj.GetComponent<Animation>() != null) rootObj = _obj;
                }
            }

            while (obj.transform.parent != null)
            {
                var parent = obj.parent;
                if (parent == rootObj) break;

                obj = parent;
                path = obj.name + "/" + path;
            }

            return path;
        }
    }
}