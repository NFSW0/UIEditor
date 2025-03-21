using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UIAttributes : MonoBehaviour
{
    private Dictionary<string, string> attributes = new Dictionary<string, string>();
    public void SetAttribute(string key, string value)
    {
        attributes[key] = value;
    }
    public Dictionary<string, string> GetAttributes()
    {
        return attributes;
    }

    private void Start()
    {
        DynamicPropertyApplier.Instance.ApplyProperties(gameObject, attributes);
    }
}


