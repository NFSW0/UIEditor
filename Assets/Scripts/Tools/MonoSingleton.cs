// 可被挂载的单例基类，防止转场销毁，调试期间可直接创建对象并挂载使用

using UnityEngine;

/// <summary>
/// 泛型单例模式的MonoBehaviour基类
/// </summary>
/// <typeparam name="T">单例类型</typeparam>
public class MonoSingleton<T> : MonoBehaviour where T : MonoBehaviour
{
    // 核心 单例对象
    private static T instance;

    // 安全 线程锁
    private static object lockObject = new object();

    // 获取单例对象的属性
    public static T Instance
    {
        get
        {
            // 使用双重检查锁定确保线程安全
            if (instance == null)
            {
                lock (lockObject)
                {
                    if (instance == null)
                    {
                        // 尝试获取已存在的单例对象
                        instance = FindObjectOfType<T>();

                        if (instance == null)
                        {
                            // 创建新单例对象
                            GameObject singletonObject = new GameObject();
                            instance = singletonObject.AddComponent<T>();
                            singletonObject.name = typeof(T).ToString() + " (Singleton)";
                            DontDestroyOnLoad(singletonObject);
                        }
                    }
                }
            }

            return instance;
        }
    }

    // 用于在加载场景或对象时处理已有此类对象的情况
    protected virtual void Awake()
    {
        // instance没有此类单例对象的记录
        if (instance == null)
        {
            // 设置此类单例对象为自身
            instance = this as T;
            DontDestroyOnLoad(gameObject);
        }
        // instance已有此类单例对象的记录
        else
        {
            // 销毁自身
            Destroy(gameObject);
        }
    }

    // 在销毁对象时处理重新创建单例对象的情况
    protected virtual void OnDestroy()
    {
        // 如果当前销毁的对象是单例对象
        if (instance == this)
        {
            // 将单例对象置空(为创建单例对象留出空间)
            instance = null;
        }
    }
}
