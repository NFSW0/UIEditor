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
        if (data is System.Collections.IEnumerable enumerable)
        {
            var properties = typeof(T).GetProperties();
            var header = string.Join(",", properties.Select(p => p.Name));

            var rows = new List<string>();
            foreach (var item in enumerable)
            {
                var row = string.Join(",", properties.Select(p => p.GetValue(item)?.ToString() ?? ""));
                rows.Add(row);
            }

            File.WriteAllText(path, header + Environment.NewLine + string.Join(Environment.NewLine, rows));
        }
        else
        {
            var properties = typeof(T).GetProperties();
            var header = string.Join(",", properties.Select(p => p.Name));
            var row = string.Join(",", properties.Select(p => p.GetValue(data)?.ToString() ?? ""));

            File.WriteAllText(path, header + Environment.NewLine + row);
        }
    }

    private T ReadFromCsv<T>(string path) where T : new()
    {
        var lines = File.ReadAllLines(path);

        lines = lines.Where(line => !string.IsNullOrWhiteSpace(line)).ToArray();

        if (lines.Length == 0)
        {
            Debug.LogWarning("CSV 文件为空");
            return default;
        }

        var header = lines[0].Split(',');

        if (lines.Length > 1)
        {
            var data = new List<T>();
            for (int i = 1; i < lines.Length; i++)
            {
                var values = lines[i].Split(',');

                if (values.Length != header.Length)
                {
                    Debug.LogWarning($"跳过无效行: {lines[i]}");
                    continue;
                }

                var item = new T();

                var properties = typeof(T).GetProperties();
                for (int j = 0; j < header.Length; j++)
                {
                    var property = properties.FirstOrDefault(p => p.Name == header[j]);
                    if (property != null && j < values.Length)
                    {
                        try
                        {
                            var value = Convert.ChangeType(values[j], property.PropertyType);
                            property.SetValue(item, value);
                        }
                        catch (Exception ex)
                        {
                            Debug.LogWarning($"无法转换值 '{values[j]}' 到属性 '{property.Name}': {ex.Message}");
                        }
                    }
                }

                data.Add(item);
            }

            return data.FirstOrDefault();
        }
        else
        {
            Debug.LogWarning("CSV 文件缺少数据行");
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
        Debug.LogError($"文件访问失败: {path}\n错误报告: {ex}");
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
