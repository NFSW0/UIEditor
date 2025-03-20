// 通过委托与事件实现解耦
// 使用方法使用案例(以无参方法为例)
// 通过AddEventListener<T>添加事件,比如：EventManager.Instance.AddEventListener("打印1",()=>{print(1);});
// 通过EventTrigger<T>触发响应事件,比如：EventManager.Instance.EventTrigger("打印1");
// 通过RemoveEventListener<T>移除事件,比如：EventManager.Instance.RemoveEventListener("打印1",()=>{print(1);});

using System.Collections.Generic;
using UnityEngine.Events;

#region 辅助部分

public interface IEventInfo
{

}

public class EventInfo<T> : IEventInfo
{
    public UnityAction<T> actions;

    public EventInfo(UnityAction<T> action)
    {
        actions = action;
    }
}
public class EventInfo : IEventInfo
{
    public UnityAction actions;

    public EventInfo(UnityAction action)
    {
        actions = action;
    }
}

#endregion

#region 主体部分

public class EventManager : MonoSingleton<EventManager>
{
    // 核心 事件典
    private readonly Dictionary<string, IEventInfo> eventDictionary = new();
    // 访问 事件典
    public Dictionary<string, IEventInfo> EventDictionary => eventDictionary;

    /// <summary>
    /// 添加事件监听
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="eventName"></param>
    /// <param name="action"></param>
    public void AddEventListener<T>(string eventName, UnityAction<T> action)
    {
        // 事件已存在
        if (EventDictionary.ContainsKey(eventName))
        {
            // 添加监听者
            (EventDictionary[eventName] as EventInfo<T>).actions += action;
        }

        // 事件不存在
        else
        {
            // 添加事件和监听者
            EventDictionary.Add(eventName, new EventInfo<T>(action));
        }
    }
    /// <summary>
    /// 添加事件监听
    /// </summary>
    /// <param name="eventName"></param>
    /// <param name="action"></param>
    public void AddEventListener(string eventName, UnityAction action)
    {
        // 事件已存在
        if (EventDictionary.ContainsKey(eventName))
        {
            // 添加监听者
            (EventDictionary[eventName] as EventInfo).actions += action;
        }

        // 事件不存在
        else
        {
            // 添加事件和监听者
            EventDictionary.Add(eventName, new EventInfo(action));
        }
    }

    /// <summary>
    /// 触发事件
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="eventName"></param>
    /// <param name="trigger"></param>
    public void EventTrigger<T>(string eventName, T trigger)
    {
        if (EventDictionary.ContainsKey(eventName))
        {
            (EventDictionary[eventName] as EventInfo<T>).actions?.Invoke(trigger);
        }
    }
    /// <summary>
    /// 触发事件
    /// </summary>
    /// <param name="eventName"></param>
    public void EventTrigger(string eventName)
    {
        if (EventDictionary.ContainsKey(eventName))
        {
            (EventDictionary[eventName] as EventInfo).actions?.Invoke();
        }
    }

    /// <summary>
    /// 移除事件监听
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="eventName"></param>
    /// <param name="action"></param>
    public void RemoveEventListener<T>(string eventName, UnityAction<T> action)
    {
        // 事件已存在
        if (EventDictionary.ContainsKey(eventName))
        {
            // 移除监听者
            (EventDictionary[eventName] as EventInfo<T>).actions -= action;

            // 判断事件无监听者
            if (EventDictionary[eventName] == null)
            {
                // 移除事件
                EventDictionary.Remove(eventName);
            }
        }
    }
    /// <summary>
    /// 移除事件监听
    /// </summary>
    /// <param name="eventName"></param>
    /// <param name="action"></param>
    public void RemoveEventListener(string eventName, UnityAction action)
    {
        // 事件已存在
        if (EventDictionary.ContainsKey(eventName))
        {
            // 移除监听者
            (EventDictionary[eventName] as EventInfo).actions -= action;

            // 判断事件无监听者
            if (EventDictionary[eventName] == null)
            {
                // 移除事件
                EventDictionary.Remove(eventName);
            }
        }
    }

    /// <summary>
    /// 清空事件典
    /// </summary>
    public void Clear()
    {
        EventDictionary.Clear();
    }
}

#endregion
