using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class BuildEditor
{
    public const string ABCONFIG_PATH = "Assets/ABConfig.asset";
    
    /// <summary>
    /// 所有文件夹ab包的集合  key是ab包名 value是路径
    /// </summary>
    private static Dictionary<string, string> mAllFileDirDict = new Dictionary<string, string>();
    /// <summary>
    /// 存储的是文件夹AB包的路径 当作过滤集合
    /// </summary>
    private static List<string> mAllFileABList = new List<string>();
    /// <summary>
    /// 单个Prefab的ab包
    /// </summary>
    private static Dictionary<string, List<string>> mAllPrefabDict = new Dictionary<string, List<string>>();
    
    [MenuItem("Tools/打包")]
    public static void Build()
    {
        mAllFileDirDict.Clear();
        mAllFileABList.Clear();
        mAllPrefabDict.Clear();
        
        ABConfig abConfig = AssetDatabase.LoadAssetAtPath<ABConfig>(ABCONFIG_PATH);
        
        abConfig.AllFileDirABList.ForEach(fileDir =>
        {
            if (mAllFileDirDict.ContainsKey(fileDir.ABName))
            {
                Debug.LogError("AB包配置名字重复,请检查...");
            }
            else
            {
                mAllFileDirDict.Add(fileDir.ABName, fileDir.Path);
                mAllFileABList.Add(fileDir.Path);
            }
        });
        //查找目标路径下的所有Prefab文件
        string[] allPrefabPathGUIDs = AssetDatabase.FindAssets("t:Prefab", abConfig.AllPrefabPathList.ToArray());
        for (int i = 0; i < allPrefabPathGUIDs.Length; i++)
        {
            string path = AssetDatabase.GUIDToAssetPath(allPrefabPathGUIDs[i]);
            EditorUtility.DisplayProgressBar("查找Prefab", "Prefab Path:" + path, i * 1.0f / allPrefabPathGUIDs.Length);
            if (!IsContainAllFileAB(path))
            {
                GameObject obj = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                string[] allDepends = AssetDatabase.GetDependencies(path);
                List<string> allDependPathList = new List<string>();
                for (int j = 0; j < allDepends.Length; j++)
                {
                    if (!IsContainAllFileAB(allDepends[j]) && !allDepends[j].EndsWith(".cs"))
                    {
                        //加入过滤集合中
                        mAllFileABList.Add(allDepends[j]);
                        allDependPathList.Add(allDepends[j]);
                    }
                }

                if (mAllPrefabDict.ContainsKey(obj.name))
                    Debug.LogError("存在相同名字的Prefab...");
                else
                    mAllPrefabDict.Add(path, allDependPathList);
            }
        }
        
        foreach (string key in mAllFileDirDict.Keys)
        {
            SetABName(key, mAllFileDirDict[key]);
        }
        
        foreach (string key in mAllPrefabDict.Keys)
        {
            SetABName(key, mAllPrefabDict[key]);
        }
        
        
        EditorUtility.ClearProgressBar();

    }
    
    /// <summary>
    /// 设置AB包名
    /// </summary>
    /// <param name="name">要设置的AB包名</param>
    /// <param name="path">文件路径</param>
    private static void SetABName(string name, string path)
    {
        AssetImporter importer = AssetImporter.GetAtPath(path);
        if (importer == null)
            Debug.LogError("不存在此路径文件:" + path);
        else
            importer.assetBundleName = name;
    }
    
    /// <summary>
    /// 设置AB包名
    /// </summary>
    /// <param name="name">要设置的AB包名</param>
    /// <param name="paths">文件路径集合</param>
    private static void SetABName(string name, List<string> paths)
    {
        paths.ForEach(path => SetABName(name, path));
    }
    
    /// <summary>
    /// 检测路径是否包含在过滤集合中
    /// </summary>
    /// <param name="path">检测的路径</param>
    private static bool IsContainAllFileAB(string path)
    {
        for (int i = 0; i < mAllFileABList.Count; i++)
        {
            if (path == mAllFileABList[i] || path.Contains(mAllFileABList[i]))
                return true;
        }

        return false;
    }
    
}