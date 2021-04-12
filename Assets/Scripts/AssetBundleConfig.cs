using System;
using System.Collections.Generic;
using System.Xml.Serialization;

[Serializable]
public class AssetBundleConfig
{
    [XmlElement("ABList")]
    public List<ABBase> ABList { get; set; }
}

[Serializable]
public class ABBase
{
    /// <summary>
    /// 文件路径
    /// </summary>
    [XmlAttribute("Path")]
    public string Path { get; set; }
    
    /// <summary>
    /// CRC校验
    /// </summary>
    [XmlAttribute("Crc")]
    public uint Crc { get; set; }
    
    /// <summary>
    /// AB包名
    /// </summary>
    [XmlAttribute("ABName")]
    public string ABName { get; set; }
    
    /// <summary>
    /// 资源名
    /// </summary>
    [XmlAttribute("AssetName")]
    public string AssetName { get; set; }
    
    /// <summary>
    /// 依赖项
    /// </summary>
    [XmlElement("ABDependenceList")]
    public List<string> ABDependenceList { get; set; }
}