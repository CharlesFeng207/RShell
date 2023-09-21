using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Object = UnityEngine.Object;

namespace RShell
{
    public static class Scene
    {
        public static string List()
        {
            return List("");
        }

        public static string List(string path)
        {
            List<GameObject> targetGameObjects;
            if (string.IsNullOrEmpty(path))
            {
                targetGameObjects = UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects().ToList();
            }
            else
            {
                Transform target = Find(path).transform;
                targetGameObjects = new List<GameObject>();
                for (int i = 0; i < target.childCount; i++)
                    targetGameObjects.Add(target.transform.GetChild(i).gameObject);
            }

            return string.Join(",", targetGameObjects.Select(go => go.name));
        }

        public static GameObject Find(string path)
        {
            GameObject[] rootGameObjects =
                UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects();
            var parts = path.Split('/');
            string rootGameObjectName = path.Split('/')[0];
            string pathFromRoot = parts.Length > 1 ? path.Substring(rootGameObjectName.Length + 1) : "";
            var rootGameObject = rootGameObjects.First(go => go.name == rootGameObjectName);
            if (rootGameObject == null) return null;
            
            Transform target = string.IsNullOrEmpty(pathFromRoot)
                ? rootGameObject.transform
                : rootGameObject.transform.Find(pathFromRoot);
            return target == null ? null : target.gameObject;
        }
        
        public static string Info(string path)
        {
            var go = Find(path);
            if (go == null) return null;
            
            var sb = new StringBuilder();
            sb.AppendLine($"name:{go.name}");
            sb.AppendLine($"childCount:{go.transform.childCount}");
            sb.AppendLine($"activeSelf:{go.activeSelf}");
            sb.AppendLine($"activeInHierarchy:{go.activeInHierarchy}");
            
            Component[] components = go.GetComponents<Component>();
            sb.AppendLine($"components:{components.Length}");
            
            foreach (var component in components)
            {
                sb.AppendLine($"{component.name} {Dumper.Do(component)}");
            }
            
            return sb.ToString();
        }
        
        public static string FindPlayingEffect()
        {
            var sb = new StringBuilder();
            ParticleSystem[] pslist = Object.FindObjectsOfType<ParticleSystem>();
            int counter = 0;
            for (int i = 0; i < pslist.Length; i++)
            {
                var ps = pslist[i];
                if (ps.isPlaying)
                {
                    counter++;
                    sb.AppendLine($"{GetTransformPath(ps.transform)} cullingMode:{ps.main.cullingMode} loop:{ps.main.loop}");
                }
            }

            sb.AppendLine($"counter: {counter} / {pslist.Length}");
            return sb.ToString();
        }

        public static void ChangeEffectCullingMode(ParticleSystemCullingMode mode)
        {
            ParticleSystem[] pslist = Object.FindObjectsOfType<ParticleSystem>();
            
            for (int i = 0; i < pslist.Length; i++)
            {
                var ps = pslist[i];
                var main = ps.main;
                main.cullingMode = mode;
            }
        }
        
        public static string GetTransformPath(Transform transform)
        {
            StringBuilder pathBuilder = new StringBuilder(128); // 预分配容量
            while (transform != null)
            {
                pathBuilder.Insert(0, transform.name); // 每次插入到最前面
                transform = transform.parent;
                if (transform != null) pathBuilder.Insert(0, "/");
            }
            return pathBuilder.ToString();
        }
    }
}
