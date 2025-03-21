// Unity在2023版本中不存在独立的AB包的资源管理

using System.Collections;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// 资源管理器
/// </summary>
public class ResourcesManager : MonoSingleton<ResourcesManager>
{
    /// <summary>
    /// 同步加载资源
    /// </summary>
    /// <typeparam name="T">资源类型</typeparam>
    /// <param name="path">资源路径</param>
    /// <returns>加载的资源</returns>
    public T Load<T>(string path) where T : Object
    {
        T resource = Resources.Load<T>(path);
        if (resource == null)
        {
            Debug.LogError($"资源路径不存在: {path}");
        }
        return resource;
    }

    /// <summary>
    /// 异步加载资源
    /// </summary>
    /// <typeparam name="T">资源类型</typeparam>
    /// <param name="path">资源路径</param>
    /// <param name="callBack">完成加载后的回调方法</param>
    public void LoadAsync<T>(string path, UnityAction<T> callBack = null) where T : Object
    {
        StartCoroutine(LoadAsyncCoroutine(path, callBack));
    }

    /// <summary>
    /// 异步核心 异步加载资源
    /// </summary>
    /// <typeparam name="T">资源类型</typeparam>
    /// <param name="path">资源相对路径</param>
    /// <param name="callBack">完成加载后的回调方法</param>
    /// <returns></returns>
    private IEnumerator LoadAsyncCoroutine<T>(string path, UnityAction<T> callBack) where T : Object
    {
        ResourceRequest resourceRequest = Resources.LoadAsync<T>(path);
        yield return resourceRequest;

        if (resourceRequest == null || resourceRequest.asset == null)
        {
            Debug.LogError($"资源路径不存在或加载失败: {path}");
            yield break;
        }

        callBack?.Invoke(resourceRequest.asset as T);
    }
}
