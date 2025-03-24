using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine;
using UnityEngine.InputSystem;

[System.Serializable]
public class PropertyMapping
{
    public string key; // 属性名称
    public string componentType; // 组件名称
    public string propertyPath; // 属性应用路径
    public string valueType; // 值类型名
    public bool allowAddComponent = true; // 是否允许添加组件
}

public class DynamicPropertyApplier : MonoSingleton<DynamicPropertyApplier>
{
    private string configFilePath = Path.Combine(Application.streamingAssetsPath, "ConfigFile.csv");
    private List<PropertyMapping> mappings = new List<PropertyMapping>();
    private bool hasMissingMapping = false; // 标记是否存在缺失的映射数据

    protected override void Awake()
    {
        base.Awake();
        List<PropertyMapping> result = FileManager.Instance.LoadData<List<PropertyMapping>>(configFilePath, FileManager.DataFormat.Csv);
        if (result != null) { mappings = result; }
    }

    protected override void OnDestroy()
    {
        if (hasMissingMapping)
        {
            mappings.Sort((a, b) => string.Compare(a.key, b.key, StringComparison.Ordinal));
            FileManager.Instance.SaveData(mappings, configFilePath, FileManager.DataFormat.Csv);
        }
        base.OnDestroy();
    }

    public void ApplyProperties(GameObject target, Dictionary<string, string> data)
    {
        foreach (var entry in data)
        {
            // 查找映射
            var mapping = mappings.Find(m => m.key == entry.Key);

            if (mapping != null)
            {
                // 如果已经发现缺失映射，则不再调用 ApplyProperty
                if (!hasMissingMapping) { ApplyProperty(target, mapping, entry.Value); }
            }
            else
            {
                Debug.LogWarning($"缺少{entry.Key}的映射数据");
                mappings.Add(new PropertyMapping() { key = entry.Key, componentType = "", propertyPath = "", valueType = "" });
                hasMissingMapping = true; // 标记存在缺失映射
            }
        }
    }

    private void ApplyProperty(GameObject target, PropertyMapping mapping, string value)
    {
        try
        {
            Type componentType = Type.GetType($"UnityEngine.{mapping.componentType}, UnityEngine") ??
                               Type.GetType(mapping.componentType); // 支持自定义组件

            if (componentType == null)
            {
                Debug.LogError($"属性 {mapping.key} 的组件 {mapping.componentType} 不存在");
                return;
            }

            // 获取或创建组件
            Component component = target.GetComponent(componentType);
            if (component == null)
            {
                if (!mapping.allowAddComponent)
                {
                    Debug.LogWarning($"缺少组件, 同时属性 {mapping.key} 不允许添加组件 {mapping.componentType}");
                    return;
                }

                // 检查特殊组件类型
                if (typeof(Transform).IsAssignableFrom(componentType))
                {
                    Debug.LogWarning($"已忽略添加Transform组件的请求: {componentType.Name}");
                    return;
                }

                component = target.AddComponent(componentType);
                Debug.Log($"已添加组件: {componentType.Name}");
            }

            // 属性路径解析逻辑
            string[] pathSegments = mapping.propertyPath.Split('.');
            object targetObject = component;

            // 递归处理嵌套属性（例如rectTransform.sizeDelta.x）
            for (int i = 0; i < pathSegments.Length - 1; i++)
            {
                var newTarget = GetMemberValue(targetObject, pathSegments[i]);
                if (newTarget == null)
                {
                    Debug.LogError($"属性 {mapping.key} 的组件参数路径存在问题: {string.Join(".", pathSegments, 0, i + 1)}");
                    return;
                }
                targetObject = newTarget;
            }

            // 最终属性设置
            SetMemberValue(
                targetObject,
                pathSegments[^1],
                ConvertValue(value, mapping.valueType)
            );
        }
        catch (Exception e)
        {
            Debug.LogError($"组件参数设置失败: {e}");
        }
    }

    // 辅助方法：获取成员值
    private object GetMemberValue(object target, string memberName)
    {
        PropertyInfo prop = target.GetType().GetProperty(memberName);
        if (prop != null) return prop.GetValue(target);

        FieldInfo field = target.GetType().GetField(memberName);
        if (field != null) return field.GetValue(target);

        return null;
    }

    // 辅助方法：设置成员值
    private void SetMemberValue(object target, string memberName, object value)
    {
        PropertyInfo prop = target.GetType().GetProperty(memberName);
        if (prop != null)
        {
            prop.SetValue(target, value);
            return;
        }

        FieldInfo field = target.GetType().GetField(memberName);
        if (field != null)
        {
            field.SetValue(target, value);
            return;
        }

        Debug.LogError($"参数缺失: {target.GetType().Name}.{memberName}");
    }

    private object ConvertValue(string value, string typeName)
    {
        try
        {
            return typeName switch
            {
                "int" => int.Parse(value),
                "float" => float.Parse(value),
                "bool" => bool.Parse(value),
                "string" => value,
                "Color" => ParseColor(value),
                "Vector2" => ParseVector2(value),
                "Vector3" => ParseVector3(value),
                "UnityEngine.Sprite" => ParseSprite(value),
                _ => Type.GetType(typeName)?.IsEnum == true
                    ? Enum.Parse(Type.GetType(typeName), value)
                    : Convert.ChangeType(value, Type.GetType(typeName))
            };
        }
        catch
        {
            Debug.LogError($"值类型转换失败: {value} -> {typeName}");
            return null;
        }
    }

    // 颜色值解析（支持#RRGGBBAA格式）
    private Color ParseColor(string value)
    {
        if (ColorUtility.TryParseHtmlString(value, out Color color))
        {
            return color;
        }
        throw new FormatException("无效的颜色格式");
    }

    // 二维向量解析（支持x,y格式）
    private Vector2 ParseVector2(string value)
    {
        string[] parts = value.Split(',');
        if (parts.Length == 2 &&
            float.TryParse(parts[0], out float x) &&
            float.TryParse(parts[1], out float y))
        {
            return new Vector2(x, y);
        }
        throw new FormatException("无效的二维矢量格式");
    }

    // 三维向量解析（支持x,y,z格式）
    private Vector3 ParseVector3(string value)
    {
        // 将输入字符串按逗号分割
        string[] parts = value.Split(',');

        // 检查是否恰好有三个部分，并尝试将每个部分解析为浮点数
        if (parts.Length == 3 &&
            float.TryParse(parts[0], out float x) &&
            float.TryParse(parts[1], out float y) &&
            float.TryParse(parts[2], out float z))
        {
            // 如果解析成功，返回一个新的三维向量
            return new Vector3(x, y, z);
        }

        // 如果格式不正确，抛出异常
        throw new FormatException("无效的三维矢量格式");
    }

    // 图片解析
    public Sprite ParseSprite(string value)
    {
        // 拼接完整路径
        string spritePath = Path.Combine(Application.streamingAssetsPath, "Sprites", value);

        // 检查文件是否存在
        if (!File.Exists(spritePath))
        {
            Debug.LogError($"图片文件不存在: {spritePath}");
            return null;
        }

        try
        {
            // 读取图片文件的字节数据
            byte[] imageData = File.ReadAllBytes(spritePath);

            // 创建Texture2D对象
            Texture2D texture = new Texture2D(2, 2);
            if (texture.LoadImage(imageData))
            {
                // 从Texture2D创建Sprite
                Sprite sprite = Sprite.Create(
                    texture,
                    new Rect(0, 0, texture.width, texture.height),
                    new Vector2(0.5f, 0.5f) // 中心点设置为图片中心
                );
                return sprite;
            }
            else
            {
                Debug.LogError($"无法加载图片数据: {spritePath}");
                return null;
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"加载图片时发生异常: {e.Message}");
            return null;
        }
    }



}