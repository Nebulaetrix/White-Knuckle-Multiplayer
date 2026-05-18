using System;
using System.Collections.Generic;
using Riptide;
using UnityEngine;

namespace WhiteKnuckleMP.Networking;

public class NetworkPacket : IDisposable
{
    public Riptide.Message RawMessage { get; private set; }
 
    // Track if the packet is being read or written
    public bool IsReading { get; private set; }

    /// <summary>
    /// Outgoing Constructor: Creates a packet ready to be packed with data to send.
    /// </summary>
    public NetworkPacket(ushort messageId, MessageSendMode sendMode = MessageSendMode.Reliable)
    {
        RawMessage = Riptide.Message.Create(sendMode, messageId);
        IsReading = false;
    }
    
    /// <summary>
    /// Incoming Constructor: Creates a packet from an incoming Riptide message to unpack.
    /// </summary>
    public NetworkPacket(Riptide.Message incomingMessage)
    {
        RawMessage = incomingMessage ?? throw new ArgumentNullException(nameof(incomingMessage));
        IsReading = true;
    }

    #region Write API

    public NetworkPacket Write(bool value)
    {
        if (IsReading) throw new InvalidOperationException("Cannot write to an incoming packet.");
        RawMessage.AddBool(value);
        return this;
    }
    
    public NetworkPacket Write(int value)
    {
        if (IsReading) throw new InvalidOperationException("Cannot write to an incoming packet.");
        RawMessage.AddInt(value);
        return this;
    }
    
    public NetworkPacket Write(float value)
    {
        if (IsReading) throw new InvalidOperationException("Cannot write to an incoming packet.");
        RawMessage.AddFloat(value);
        return this;
    }

    public NetworkPacket Write(string value)
    {
        if (IsReading) throw new InvalidOperationException("Cannot write to an incoming packet.");
        RawMessage.AddString(value);
        return this;
    }

    public NetworkPacket Write(ushort value)
    {
        if (IsReading) throw new InvalidOperationException("Cannot write to an incoming packet.");
        RawMessage.AddUShort(value);
        return this;
    }

    public NetworkPacket Write(Vector3 value)
    {
        if (IsReading) throw new InvalidOperationException("Cannot write to an incoming packet.");
        RawMessage.AddFloat(value.x);
        RawMessage.AddFloat(value.y);
        RawMessage.AddFloat(value.z);
        return this;
    }

    public NetworkPacket Write(Quaternion value)
    {
        if (IsReading) throw new InvalidOperationException("Cannot write to an incoming packet.");
        RawMessage.AddFloat(value.x);
        RawMessage.AddFloat(value.y);
        RawMessage.AddFloat(value.z);
        RawMessage.AddFloat(value.w);
        return this;
    }

    public NetworkPacket Write(Color value)
    {
        if (IsReading) throw new InvalidOperationException("Cannot write to an incoming packet.");
        RawMessage.AddFloat(value.r);
        RawMessage.AddFloat(value.g);
        RawMessage.AddFloat(value.b);
        RawMessage.AddFloat(value.a);
        return this;
    }
    
    #endregion
    
    #region Read API

    public bool ReadBool()
    {
        if (!IsReading) throw new InvalidOperationException("Cannot read from an outgoing packet.");
        return RawMessage.GetBool();
    }

    public int ReadInt()
    {
        if (!IsReading) throw new InvalidOperationException("Cannot read from an outgoing packet.");
        return RawMessage.GetInt();
    }

    public float ReadFloat()
    {
        if (!IsReading) throw new InvalidOperationException("Cannot read from an outgoing packet.");
        return RawMessage.GetFloat();
    }

    public string ReadString()
    {
        if (!IsReading) throw new InvalidOperationException("Cannot read from an outgoing packet.");
        return RawMessage.GetString();
    }

    public ushort ReadUShort()
    {
        if (!IsReading) throw new InvalidOperationException("Cannot read from an outgoing packet.");
        return RawMessage.GetUShort();
    }

    public Vector3 ReadVector3()
    {
        if (!IsReading) throw new InvalidOperationException("Cannot read from an outgoing packet.");
        return new Vector3(RawMessage.GetFloat(), RawMessage.GetFloat(), RawMessage.GetFloat());
    }

    public Quaternion ReadQuaternion()
    {
        if (!IsReading) throw new InvalidOperationException("Cannot read from an outgoing packet.");
        return new Quaternion(RawMessage.GetFloat(), RawMessage.GetFloat(), RawMessage.GetFloat(), RawMessage.GetFloat());
    }

    public Color ReadColor()
    {
        if (!IsReading) throw new InvalidOperationException("Cannot read from an outgoing packet.");
        return new Color(RawMessage.GetFloat(), RawMessage.GetFloat(), RawMessage.GetFloat(), RawMessage.GetFloat());
    }
    
    #endregion
    
    public void Dispose()
    {
        RawMessage.Release();
    }
}