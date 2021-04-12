using System;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEngine;

public class ResourcesTest : MonoBehaviour
{
    private void Start()
    {
        TestLoadAB();
    }

    private void TestLoadAB()
    {
        TextAsset ta = UnityEditor.AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/AssetBundleConfig.bytes");
        MemoryStream memoryStream = new MemoryStream(ta.bytes);
        BinaryFormatter binaryFormatter = new BinaryFormatter();
        AssetBundleConfig config = binaryFormatter.Deserialize(memoryStream) as AssetBundleConfig;
        memoryStream.Close();

        string path = "Assets/GameData/Prefabs/Attack.prefab";
        uint crc = Crc32.GetCrc32(path);
        ABBase abBase = null;
        config.ABList.ForEach(data =>
        {
            if (data.Crc == crc)
                abBase = data;
        });

        abBase.ABDependenceList.ForEach(data =>
        {
            AssetBundle.LoadFromFile(Application.streamingAssetsPath + "/" + data);
        });
        
        AssetBundle ab = AssetBundle.LoadFromFile(Application.streamingAssetsPath + "/" + abBase.ABName);
        GameObject obj = Instantiate(ab.LoadAsset<GameObject>("attack"));

    }
    
}