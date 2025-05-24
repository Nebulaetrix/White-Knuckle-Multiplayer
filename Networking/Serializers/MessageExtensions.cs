using System;
using System.Collections.Generic;
using Riptide;
using UnityEngine;


namespace White_Knuckle_Multiplayer.Networking.Serializers;

public static class MessageExtensions
{
    public static void AddVector3(this Riptide.Message msg, Vector3 vector)
    {
        msg.AddFloat(vector.x).Add(vector.y).Add(vector.z);
    }
    public static Vector3 GetVector3(this Riptide.Message msg) =>
        new Vector3(msg.GetFloat(), msg.GetFloat(), msg.GetFloat());

    public static void AddQuaternion(this Riptide.Message msg, Quaternion quaternion)
    {
        msg.AddFloat(quaternion.x).Add(quaternion.y).Add(quaternion.z).Add(quaternion.w);
    }
    public static Quaternion GetQuaternion(this Riptide.Message msg) =>
        new Quaternion(msg.GetFloat(), msg.GetFloat(), msg.GetFloat(), msg.GetFloat());

    public static void AddColor(this Riptide.Message msg, Color color)
    {
        msg.AddFloat(color.r).Add(color.g).Add(color.b).Add(color.a);
    }
    public static Color GetColor(this Riptide.Message msg) =>
        new Color(msg.GetFloat(), msg.GetFloat(), msg.GetFloat(), msg.GetFloat());

    
    #region AddList / GetList
    // Adding lists is a bit more work than usual
    // I tried to keep it as modular as possible
    // Here is an example how to send lists with this approach
    // 
    // writing
    // msg.AddList(stringList, (m, s) => m.AddString(s));
    // msg.AddList(vector3List, (m, v) => m.AddVector3(v));
    //
    // reading
    // var strings = msg.GetList(m => m.GetString());
    // var vectors = msg.GetList(m => m.GetVector3());
    
    public static void AddList<T>(
        this Riptide.Message msg,
        IList<T> list,
        Action<Riptide.Message, T> writeItem
    )
    {
        msg.AddInt(list.Count);
        foreach (var item in list)
            writeItem(msg, item);
    }
    
    public static List<T> GetList<T>(
        this Riptide.Message msg,
        Func<Riptide.Message, T> readItem
    )
    {
        int count = msg.GetInt();
        var list  = new List<T>(count);
        for (int i = 0; i < count; i++)
            list.Add(readItem(msg));
        return list;
    }
    
    #endregion
    
}