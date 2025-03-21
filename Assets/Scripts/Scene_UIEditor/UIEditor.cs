using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using UnityEngine;


public class UIEditor : MonoBehaviour
{
    // XML 文件路径
    public string xmlPath = Application.streamingAssetsPath + "/Test.xml";
    public string resultPath = Application.streamingAssetsPath + "/Result.xml";


    void Start()
    {
        // 从指定路径加载 XML 文件并生成场景节点
        GameObject root = XmlToScene(xmlPath);

        // 将生成的根节点设置为当前对象的子节点
        if (root != null)
        {
            root.transform.SetParent(transform);
            Debug.Log("成功加载 XML 并生成场景节点！");
        }
        else
        {
            Debug.LogError("XML 加载失败，无法生成场景节点！");
        }

        // 将当前场景中的 GameObject 转换为 XML 字符串
        string xmlString = SceneToXml(root);
        Debug.Log("生成的XML内容：\n" + xmlString);
    }


    public GameObject XmlToScene(string filePath)
    {
        // 检查文件是否存在
        if (!File.Exists(filePath))
        {
            Debug.LogError("无法找到 XML 文件: " + filePath);
            return null;
        }

        // 加载 XML 文件
        XmlDocument xmlDoc = new XmlDocument();
        xmlDoc.Load(filePath);

        // 查找 <Prefab><Window> 节点
        XmlNode prefabNode = xmlDoc.SelectSingleNode("Prefab/Window");
        if (prefabNode == null)
        {
            Debug.LogError("XML 格式错误: 缺少 <Prefab><Window> 结构");
            return null;
        }

        // 解析 XML 节点并生成对应的 GameObject
        return ParseXmlNode(prefabNode);
    }

    private GameObject ParseXmlNode(XmlNode xmlNode)
    {
        // 检查当前节点是否为元素节点
        if (xmlNode.NodeType != XmlNodeType.Element) return null;

        // 创建当前节点对应的 GameObject
        string nodeName = xmlNode.Name;
        GameObject currentObject = CreateGameObject(nodeName, xmlNode.Attributes);

        // 遍历子节点并递归解析
        foreach (XmlNode childNode in xmlNode.ChildNodes)
        {
            GameObject childObject = ParseXmlNode(childNode);
            if (childObject != null)
            {
                // 将解析出的子对象设置为当前对象的子对象
                childObject.transform.SetParent(currentObject.transform);
            }
        }

        // 返回当前节点对应的 GameObject
        return currentObject;
    }

    private GameObject CreateGameObject(string objectName, XmlAttributeCollection attributes)
    {
        // 创建一个新的 GameObject
        GameObject newObj = new GameObject(objectName);

        // 添加 RectTransform 组件以支持 UI 布局
        newObj.AddComponent<RectTransform>();

        // 添加自定义的 UIAttributes 组件以存储属性
        UIAttributes uiAttributes = newObj.AddComponent<UIAttributes>();
        foreach (XmlAttribute attr in attributes)
        {
            // 设置每个属性的值
            uiAttributes.SetAttribute(attr.Name, attr.Value);
        }

        // 返回创建的 GameObject
        return newObj;
    }

    public string SceneToXml(GameObject root)
    {
        XmlDocument xmlDoc = new XmlDocument();

        // 创建根节点
        XmlElement prefabElement = xmlDoc.CreateElement("Prefab");
        xmlDoc.AppendChild(prefabElement);

        // 递归生成 XML 节点
        GenerateXmlNode(xmlDoc, prefabElement, root);

        // 返回 XML 字符串
        return xmlDoc.OuterXml;
    }

    private void GenerateXmlNode(XmlDocument xmlDoc, XmlElement parentElement, GameObject gameObject)
    {
        // 创建当前节点
        XmlElement nodeElement = xmlDoc.CreateElement(gameObject.name);
        parentElement.AppendChild(nodeElement);

        // 获取 UIAttributes 组件并导出属性
        UIAttributes uiAttributes = gameObject.GetComponent<UIAttributes>();
        if (uiAttributes != null)
        {
            foreach (var attribute in uiAttributes.GetAttributes())
            {
                nodeElement.SetAttribute(attribute.Key, attribute.Value);
            }
        }

        // 递归处理子对象
        foreach (Transform child in gameObject.transform)
        {
            GenerateXmlNode(xmlDoc, nodeElement, child.gameObject);
        }
    }
}
