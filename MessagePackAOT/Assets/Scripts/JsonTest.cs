using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Newtonsoft.Json;

namespace JsonItem2
{
    public struct StructItem
    {
        public int Level { get; set; }
        public int Exp { get; set; }
        public int DeltaExp { get; set; }
    }

    public class ItemsArray
    {
        public StructItem[] Items { get; set; }
    }
}

public class JsonTest : MonoBehaviour
{
    [SerializeField] Text _text;

    private void Start()
    {
        InjectMod();
    }


    public void Test()
    {
        var json = string.Format("{{\"Level\":{0},\"Exp\":12,\"DeltaExp\":2}}", (int)(Time.time * 1000));
        var obj2 = JsonConvert.DeserializeObject<JsonItem.StructItem>(json);
        _text.text = obj2.Level.ToString();
    }

    public void Test2()
    {
        var json = string.Format("{{\"Items\":[{{\"Level\":{0},\"Exp\":12,\"DeltaExp\":2}}]}}", (int)(Time.time * 1000));
        var obj2 = JsonConvert.DeserializeObject<JsonItem.ItemsArray>(json);
        _text.text = obj2.Items[0].Level.ToString();
    }

    public void Test3()
    {
        var json = string.Format("{{\"Items\":[{{\"Level\":{0},\"Exp\":12,\"DeltaExp\":2}}]}}", (int)(Time.time * 1000));
        var obj2 = JsonConvert.DeserializeObject<JsonItem2.ItemsArray>(json);
        _text.text = obj2.Items[0].Level.ToString();
    }

    private void InjectMod()
    {
        var json = string.Format("{{\"Items\":[{{\"Level\":{0},\"Exp\":12,\"DeltaExp\":2}}]}}", (int)(Time.time * 1000));
        var obj2 = JsonConvert.DeserializeObject<JsonItem2.ItemsArray>(json);
        _text.text = obj2.Items[0].Level.ToString();
    }

    /*
public void Test4()
{
    var json = string.Format("{{\"Level\":{0},\"Exp\":12,\"DeltaExp\":2}}", (int)(Time.time * 1000));
    var obj2 = JsonConvert.DeserializeObject<Vector2>(json);
    _text.text = obj2.Level.ToString();
}*/
}
