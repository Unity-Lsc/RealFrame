using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "ABConfig", menuName = "CreateABConfig", order = 0)]
public class ABConfig : ScriptableObject {
    
	//单个文件所在路径.会遍历文件夹下所有Prefab,所有的prefab名字不能重复,必须保证名字的唯一性
	public List<string> AllPrefabPathList = new List<string>();
	
	public List<FileDirABName> AllFileDirABList = new List<FileDirABName>();
	
	[Serializable]
	public struct FileDirABName
	{
		public string ABName;
		public string Path;
	}
    
}
