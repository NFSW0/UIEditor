// 数据文件读写功能的集成类

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Xml.Serialization;
using UnityEngine;

public class FileManager : MonoSingleton<FileManager>
{
    /// <summary>
    /// 保存数据
    /// </summary>
    /// <typeparam name="T">数据类型</typeparam>
    /// <param name="data">待保存数据</param>
    /// <param name="path">保存路径</param>
    /// <param name="format">保存格式(枚举)</param>
    /// <returns>保存成功</returns>
    public bool SaveData<T>(T data, string path, DataFormat format)
    {
        try
        {
            EnsureDirectoryExists(path);
            switch (format)
            {
                case DataFormat.Binary:
                    SaveToBin(data, path);
                    break;
                case DataFormat.Json:
                    SaveToJson(data, path);
                    break;
                case DataFormat.Xml:
                    SaveToXml(data, path);
                    break;
                case DataFormat.Txt:
                    SaveToTxt(data, path);
                    break;
                case DataFormat.Csv:
                    SaveToCsv(data, path);
                    break;
            }
            Debug.Log($"已保存文件: {path}");
            return true;
        }
        catch (System.Exception ex)
        {
            LogError(ex, path);
            return false;
        }
    }

    /// <summary>
    /// 加载数据
    /// </summary>
    /// <typeparam name="T">数据类型</typeparam>
    /// <param name="path">读取路径</param>
    /// <param name="format">读取格式(枚举)</param>
    /// <returns>读取结果</returns>
    public T LoadData<T>(string path, DataFormat format) where T : new()
    {
        try
        {
            switch (format)
            {
                case DataFormat.Binary:
                    return ReadFromBin<T>(path);
                case DataFormat.Json:
                    return ReadFromJson<T>(path);
                case DataFormat.Xml:
                    return ReadFromXml<T>(path);
                case DataFormat.Txt:
                    return ReadFromTxt<T>(path);
                case DataFormat.Csv:
                    return ReadFromCsv<T>(path);
                default:
                    return default;
            }
        }
        catch (System.Exception ex)
        {
            LogError(ex, path);
            return default;
        }
    }

    private void SaveToBin<T>(T t, string path)
    {
        using (FileStream file = File.Open(path, FileMode.Create))
        {
            BinaryFormatter bf = new BinaryFormatter();
            bf.Serialize(file, t);
        }
    }

    private T ReadFromBin<T>(string path)
    {
        using (FileStream file = File.Open(path, FileMode.Open))
        {
            BinaryFormatter bf = new BinaryFormatter();
            T data = (T)bf.Deserialize(file);
            return data;
        }
    }

    private void SaveToJson<T>(T data, string path)
    {
        string json = JsonUtility.ToJson(data);
        File.WriteAllText(path, json);
    }

    private T ReadFromJson<T>(string path)
    {
        string json = File.ReadAllText(path);
        T data = JsonUtility.FromJson<T>(json);
        return data;
    }

    private void SaveToXml<T>(T data, string path)
    {
        XmlSerializer serializer = new(typeof(T));
        using (var fileStream = File.Create(path))
        {
            serializer.Serialize(fileStream, data);
        }
    }

    private T ReadFromXml<T>(string path)
    {
        XmlSerializer serializer = new(typeof(T));
        using (var fileStream = File.OpenRead(path))
        {
            T data = (T)serializer.Deserialize(fileStream);
            return data;
        }
    }

    private void SaveToTxt<T>(T data, string path)
    {
        string text = data?.ToString() ?? string.Empty;
        File.WriteAllText(path, text);
    }

    private T ReadFromTxt<T>(string path)
    {
        string text = File.ReadAllText(path);

        if (typeof(T) == typeof(string))
        {
            return (T)(object)text;
        }

        try
        {
            return (T)Convert.ChangeType(text, typeof(T));
        }
        catch
        {
            Debug.LogWarning($"无法将 TXT 内容转换为目标类型 {typeof(T)}");
            return default;
        }
    }

    private void SaveToCsv<T>(T data, string path)
    {
        try
        {
            if (data == null)
            {
                Debug.LogWarning("尝试保存到 CSV 但数据为空。");
                return;
            }

            Type targetType;
            List<object> items = new List<object>();

            if (data is System.Collections.IEnumerable enumerable)
            {
                foreach (var item in enumerable)
                {
                    if (item != null)
                    {
                        items.Add(item);
                    }
                }

                if (items.Count == 0)
                {
                    Debug.LogWarning("列表数据为空，无法保存到 CSV。");
                    return;
                }

                targetType = items[0].GetType();
            }
            else
            {
                items.Add(data);
                targetType = typeof(T);
            }

            var fields = targetType.GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
            if (fields.Length == 0)
            {
                Debug.LogWarning("数据对象缺少可序列化的字段，无法保存到 CSV。");
                return;
            }

            var header = string.Join(",", fields.Select(f => f.Name));

            var rows = new List<string>();
            foreach (var item in items)
            {
                try
                {
                    var row = string.Join(",", fields.Select(f =>
                    {
                        var value = f.GetValue(item);
                        return value != null ? $"\"{value.ToString().Replace("\"", "\"\"")}\"" : "";
                    }));
                    rows.Add(row);
                }
                catch (Exception ex)
                {
                    Debug.LogError($"序列化对象 {item.GetType().Name} 时发生错误: {ex.Message}");
                }
            }

            File.WriteAllText(path, header + Environment.NewLine + string.Join(Environment.NewLine, rows));
        }
        catch (IOException ioEx)
        {
            Debug.LogError($"写入 CSV 文件 {path} 时发生 IO 错误: {ioEx.Message}");
        }
        catch (UnauthorizedAccessException uaEx)
        {
            Debug.LogError($"无权访问文件 {path}: {uaEx.Message}");
        }
        catch (Exception ex)
        {
            Debug.LogError($"保存 CSV 文件 {path} 时发生未知错误: {ex.Message}");
        }
    }

    private T ReadFromCsv<T>(string path) where T : new()
    {
        try
        {
            if (!File.Exists(path))
            {
                Debug.LogWarning($"CSV 文件不存在: {path}");
                return default;
            }

            var lines = File.ReadAllLines(path)
                            .Where(line => !string.IsNullOrWhiteSpace(line))
                            .ToArray();

            if (lines.Length < 2) // 至少需要标题行 + 数据行
            {
                Debug.LogWarning($"CSV 文件 {path} 缺少数据行");
                return default;
            }

            var header = lines[0].Split(',');
            var properties = typeof(T).GetProperties();

            // 检查 T 是否为 List<TItem>
            var listType = typeof(T).IsGenericType && typeof(T).GetGenericTypeDefinition() == typeof(List<>);
            var listInstance = listType ? Activator.CreateInstance(typeof(T)) as System.Collections.IList : null;
            var itemType = listType ? typeof(T).GetGenericArguments()[0] : typeof(T);

            for (int i = 1; i < lines.Length; i++)
            {
                var values = lines[i].Split(',');

                if (values.Length != header.Length)
                {
                    Debug.LogWarning($"跳过格式错误的行: {lines[i]}");
                    continue;
                }

                var item = Activator.CreateInstance(itemType);

                foreach (var property in itemType.GetProperties())
                {
                    int index = Array.IndexOf(header, property.Name);
                    if (index >= 0 && index < values.Length)
                    {
                        try
                        {
                            object convertedValue;
                            if (property.PropertyType == typeof(bool))
                            {
                                convertedValue = values[index].ToLower() == "true";
                            }
                            else
                            {
                                convertedValue = Convert.ChangeType(values[index], property.PropertyType);
                            }
                            property.SetValue(item, convertedValue);
                        }
                        catch (Exception ex)
                        {
                            Debug.LogWarning($"无法转换值 '{values[index]}' 到属性 '{property.Name}': {ex.Message}");
                        }
                    }
                }

                listInstance?.Add(item);
            }

            return listType ? (T)listInstance : (T)(object)listInstance?.Cast<object>().FirstOrDefault();
        }
        catch (Exception ex)
        {
            Debug.LogError($"读取 CSV 文件 {path} 时发生错误: {ex.Message}");
            return default;
        }
    }

    private void EnsureDirectoryExists(string filePath)
    {
        string directoryPath = Path.GetDirectoryName(filePath);
        if (!Directory.Exists(directoryPath) && directoryPath != null)
        {
            Directory.CreateDirectory(directoryPath);
            Debug.Log($"已创建路径: {directoryPath}");
        }
    }

    private void LogError(System.Exception ex, string path)
    {
        Debug.LogError($"文件处理失败: {path}\n错误报告: {ex}");
    }

    public enum DataFormat
    {
        Binary,
        Json,
        Xml,
        Txt,
        Csv
    }
}
