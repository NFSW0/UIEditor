using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using UnityEngine;


public class UIEditor : MonoBehaviour
{
    // XML �ļ�·��
    public string xmlPath = Application.streamingAssetsPath + "/Test.xml";
    public string resultPath = Application.streamingAssetsPath + "/Result.xml";


    void Start()
    {
        // ��ָ��·������ XML �ļ������ɳ����ڵ�
        GameObject root = XmlToScene(xmlPath);

        // �����ɵĸ��ڵ�����Ϊ��ǰ������ӽڵ�
        if (root != null)
        {
            root.transform.SetParent(transform);
            Debug.Log("�ɹ����� XML �����ɳ����ڵ㣡");
        }
        else
        {
            Debug.LogError("XML ����ʧ�ܣ��޷����ɳ����ڵ㣡");
        }

        // ����ǰ�����е� GameObject ת��Ϊ XML �ַ���
        string xmlString = SceneToXml(root);
        Debug.Log("���ɵ�XML���ݣ�\n" + xmlString);
    }


    public GameObject XmlToScene(string filePath)
    {
        // ����ļ��Ƿ����
        if (!File.Exists(filePath))
        {
            Debug.LogError("�޷��ҵ� XML �ļ�: " + filePath);
            return null;
        }

        // ���� XML �ļ�
        XmlDocument xmlDoc = new XmlDocument();
        xmlDoc.Load(filePath);

        // ���� <Prefab><Window> �ڵ�
        XmlNode prefabNode = xmlDoc.SelectSingleNode("Prefab/Window");
        if (prefabNode == null)
        {
            Debug.LogError("XML ��ʽ����: ȱ�� <Prefab><Window> �ṹ");
            return null;
        }

        // ���� XML �ڵ㲢���ɶ�Ӧ�� GameObject
        return ParseXmlNode(prefabNode);
    }

    private GameObject ParseXmlNode(XmlNode xmlNode)
    {
        // ��鵱ǰ�ڵ��Ƿ�ΪԪ�ؽڵ�
        if (xmlNode.NodeType != XmlNodeType.Element) return null;

        // ������ǰ�ڵ��Ӧ�� GameObject
        string nodeName = xmlNode.Name;
        GameObject currentObject = CreateGameObject(nodeName, xmlNode.Attributes);

        // �����ӽڵ㲢�ݹ����
        foreach (XmlNode childNode in xmlNode.ChildNodes)
        {
            GameObject childObject = ParseXmlNode(childNode);
            if (childObject != null)
            {
                // �����������Ӷ�������Ϊ��ǰ������Ӷ���
                childObject.transform.SetParent(currentObject.transform);
            }
        }

        // ���ص�ǰ�ڵ��Ӧ�� GameObject
        return currentObject;
    }

    private GameObject CreateGameObject(string objectName, XmlAttributeCollection attributes)
    {
        // ����һ���µ� GameObject
        GameObject newObj = new GameObject(objectName);

        // ��� RectTransform �����֧�� UI ����
        newObj.AddComponent<RectTransform>();

        // ����Զ���� UIAttributes ����Դ洢����
        UIAttributes uiAttributes = newObj.AddComponent<UIAttributes>();
        foreach (XmlAttribute attr in attributes)
        {
            // ����ÿ�����Ե�ֵ
            uiAttributes.SetAttribute(attr.Name, attr.Value);
        }

        // ���ش����� GameObject
        return newObj;
    }

    public string SceneToXml(GameObject root)
    {
        XmlDocument xmlDoc = new XmlDocument();

        // �������ڵ�
        XmlElement prefabElement = xmlDoc.CreateElement("Prefab");
        xmlDoc.AppendChild(prefabElement);

        // �ݹ����� XML �ڵ�
        GenerateXmlNode(xmlDoc, prefabElement, root);

        // ���� XML �ַ���
        return xmlDoc.OuterXml;
    }

    private void GenerateXmlNode(XmlDocument xmlDoc, XmlElement parentElement, GameObject gameObject)
    {
        // ������ǰ�ڵ�
        XmlElement nodeElement = xmlDoc.CreateElement(gameObject.name);
        parentElement.AppendChild(nodeElement);

        // ��ȡ UIAttributes �������������
        UIAttributes uiAttributes = gameObject.GetComponent<UIAttributes>();
        if (uiAttributes != null)
        {
            foreach (var attribute in uiAttributes.GetAttributes())
            {
                nodeElement.SetAttribute(attribute.Key, attribute.Value);
            }
        }

        // �ݹ鴦���Ӷ���
        foreach (Transform child in gameObject.transform)
        {
            GenerateXmlNode(xmlDoc, nodeElement, child.gameObject);
        }
    }
}
