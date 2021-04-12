using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Xml.Serialization;
using UnityEditor;
using UnityEngine;

public class BuildEditor
{
    private const string ABCONFIG_PATH = "Assets/ABConfig.asset";


    private static string mBundleTargetPath = Application.streamingAssetsPath;
    
    /// <summary>
    /// 所有文件夹ab包的集合  key是ab包名 value是路径
    /// </summary>
    private static Dictionary<string, string> mAllFileDirDict = new Dictionary<string, string>();
    /// <summary>
    /// 存储的是文件夹AB包的路径 当作过滤集合
    /// </summary>
    private static List<string> mAllFileABList = new List<string>();
    /// <summary>
    /// 单个Prefab的ab包 key为路径 value为依赖项(包含自身)
    /// </summary>
    private static Dictionary<string, List<string>> mAllPrefabDict = new Dictionary<string, List<string>>();
    /// <summary>
    /// 储存所有有效路径(需要动态加载的文件)
    /// </summary>
    private static List<string> mValidFileList = new List<string>();
    
    [MenuItem("Tools/打包")]
    public static void Build()
    {
        mAllFileDirDict.Clear();
        mAllFileABList.Clear();
        mAllPrefabDict.Clear();
        mValidFileList.Clear();
        
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
                mValidFileList.Add(fileDir.Path);
            }
        });
        //查找目标路径下的所有Prefab文件
        string[] allPrefabPathGUIDs = AssetDatabase.FindAssets("t:Prefab", abConfig.AllPrefabPathList.ToArray());
        for (int i = 0; i < allPrefabPathGUIDs.Length; i++)
        {
            string path = AssetDatabase.GUIDToAssetPath(allPrefabPathGUIDs[i]);
            mValidFileList.Add(path);
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
                    mAllPrefabDict.Add(obj.name, allDependPathList);
            }
        }
        //文件夹AB包 设置AB包名
        foreach (string key in mAllFileDirDict.Keys)
        {
            SetABName(key, mAllFileDirDict[key]);
        }
        //Prefab AB包 设置AB包名
        foreach (string key in mAllPrefabDict.Keys)
        {
            SetABName(key, mAllPrefabDict[key]);
        }
        
        BuildAssetBundle();
        
        //清除设置的AB包名,防止.meta文件发生改变
        string[] oldABNames = AssetDatabase.GetAllAssetBundleNames();
        for (int i = 0; i < oldABNames.Length; i++)
        {
            AssetDatabase.RemoveAssetBundleName(oldABNames[i], true);
            EditorUtility.DisplayProgressBar("清除AB包名", "AB包名:" + oldABNames[i], i * 1.0f / oldABNames.Length);
        }
        
        AssetDatabase.Refresh();
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
    /// AB打包
    /// </summary>
    private static void BuildAssetBundle()
    {
        string[] allBundleNames = AssetDatabase.GetAllAssetBundleNames();
        //key为全路径 value为包名
        Dictionary<string, string> resPathDict = new Dictionary<string, string>();
        for (var i = 0; i < allBundleNames.Length; i++)
        {
            string[] allBundlePaths = AssetDatabase.GetAssetPathsFromAssetBundle(allBundleNames[i]);
            for (var j = 0; j < allBundlePaths.Length; j++)
            {
                if(allBundlePaths[j].EndsWith(".cs")) continue;
                Debug.LogFormat("此AB包:{0}下包含的文件:{1}", allBundleNames[i], allBundlePaths[j]);
                resPathDict.Add(allBundlePaths[j], allBundleNames[i]);
            }
        }
        DeleteAB();
        
        //生成配置表
        WriteData(resPathDict);
        
        BuildPipeline.BuildAssetBundles(mBundleTargetPath, BuildAssetBundleOptions.ChunkBasedCompression,
            EditorUserBuildSettings.activeBuildTarget);

    }
    
    /// <summary>
    /// 写入数据
    /// </summary>
    private static void WriteData(Dictionary<string, string> resPathDict)
    {
        AssetBundleConfig config = new AssetBundleConfig();
        config.ABList = new List<ABBase>();
        foreach (string path in resPathDict.Keys)
        {
            if(!IsValidPath(path)) continue;
            ABBase abBase = new ABBase()
            {
                Path = path,
                Crc = Crc32.GetCrc32(path),
                ABName = resPathDict[path],
                AssetName = path.Remove(0, path.LastIndexOf('/') + 1)
            };
            abBase.ABDependenceList =new List<string>();
            string[] resDependences = AssetDatabase.GetDependencies(path);
            for (int i = 0; i < resDependences.Length; i++)
            {
                string tempPath = resDependences[i];
                if(tempPath == path || path.EndsWith(".cs")) continue;
                string abName = string.Empty;
                if (resPathDict.TryGetValue(tempPath, out abName))
                {
                    if(abName == resPathDict[path]) continue;
                    if(!abBase.ABDependenceList.Contains(abName))
                        abBase.ABDependenceList.Add(abName);
                }
            }
            config.ABList.Add(abBase);
        }
        
        //写入XML
        string xmlPath = Application.dataPath + "/AssetBundleConfig.xml";
        if(File.Exists(xmlPath)) File.Delete(xmlPath);
        FileStream fileStream = new FileStream(xmlPath, FileMode.Create, FileAccess.ReadWrite, FileShare.ReadWrite);
        StreamWriter streamWriter = new StreamWriter(fileStream, System.Text.Encoding.UTF8);
        XmlSerializer xmlSerializer = new XmlSerializer(config.GetType());
        xmlSerializer.Serialize(streamWriter, config);
        streamWriter.Close();
        fileStream.Close();

        //写入二进制
        config.ABList.ForEach(abBase => abBase.Path = string.Empty);
        string bytePath = mBundleTargetPath + "/AssetBundleConfig.bytes";
        FileStream byteFileStream = new FileStream(bytePath, FileMode.Create, FileAccess.ReadWrite, FileShare.ReadWrite);
        BinaryFormatter binaryFormatter = new BinaryFormatter();
        binaryFormatter.Serialize(byteFileStream, config);
        byteFileStream.Close();
    }
    
    private static void DeleteAB()
    {
        string[] allBundleNames = AssetDatabase.GetAllAssetBundleNames();
        DirectoryInfo directoryInfo = new DirectoryInfo(mBundleTargetPath);
        FileInfo[] fileInfos = directoryInfo.GetFiles("*", SearchOption.AllDirectories);
        for (var i = 0; i < fileInfos.Length; i++)
        {
            if(IsContainABName(fileInfos[i].Name, allBundleNames) || fileInfos[i].Name.EndsWith(".meta")) continue;
            else
            {
                if(File.Exists(fileInfos[i].FullName))
                    File.Delete(fileInfos[i].FullName);
            }
        }
    }

    private static bool IsContainABName(string name, string[] strs)
    {
        for (var i = 0; i < strs.Length; i++)
        {
            if (name == strs[i]) return true;
        }

        return false;
    }
    
    /// <summary>
    /// 检测路径是否包含在过滤集合中 做AB包冗余剔除
    /// </summary>
    /// <param name="path">检测的路径</param>
    private static bool IsContainAllFileAB(string path)
    {
        for (int i = 0; i < mAllFileABList.Count; i++)
        {
            if (path == mAllFileABList[i] || (path.Contains(mAllFileABList[i]) && (path.Replace(mAllFileABList[i],"")[0] == '/')))
                return true;
        }

        return false;
    }
    
    /// <summary>
    /// 是否是有效路径
    /// </summary>
    private static bool IsValidPath(string path)
    {
        for (var i = 0; i < mValidFileList.Count; i++)
        {
            if (path.Contains(mValidFileList[i])) return true;
        }

        return false;
    }
    
}