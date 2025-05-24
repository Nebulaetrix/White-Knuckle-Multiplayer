using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace White_Knuckle_Multiplayer.Networking.Routing;

public static class MessageRouter
{
    private static readonly Dictionary<(ushort, byte), Delegate> Handlers;

    static MessageRouter()
    {
        Handlers = typeof(MessageHandler)
            .GetMethods(BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public)
            .Where(method => method.GetCustomAttributes(typeof(WKMessageHandlerAttribute), false).Length > 0)
            .SelectMany(method =>
                {
                    var attrs = method.GetCustomAttributes<WKMessageHandlerAttribute>();
                    var parameters = method.GetParameters();
                    
                    Delegate del = null;

                    try
                    {
                        switch (parameters.Length)
                        {
                            case 2 when
                                parameters[0].ParameterType == typeof(ushort) &&
                                parameters[1].ParameterType == typeof(Riptide.Message):
                                del = Delegate.CreateDelegate(typeof(Action<ushort, Riptide.Message>), method);
                                break;
                            case 1 when
                                parameters[0].ParameterType == typeof(Riptide.Message):
                                del = Delegate.CreateDelegate(typeof(Action<Riptide.Message>), method);
                                break;
                            default:
                                throw new InvalidOperationException(
                                    $"Unsupported parameter signature for {method.Name}");
                        }
                    }
                    catch (Exception e)
                    {
                        LogManager.Net.Error($"Failed to bind {method.Name}: {e.Message}");
                    }
                    
                    return del == null 
                        ? Enumerable.Empty<((ushort, byte), Delegate)>()
                        : attrs.Select(attr => ((attr.messageID, attr.groupID), del));
                }
            )
            .ToDictionary(t => t.Item1, t => t.Item2);
    }
    
    /// <summary>
    /// Routes an incoming message to the correct handler.
    /// </summary>
    /// <param name="messageID">The Riptide message ID.</param>
    /// <param name="groupID">0 = server handlers, 1 = client handlers.</param>
    /// <param name="fromClientID">Who sent it (for server‐side).</param>
    /// <param name="msg">The Riptide.Message to dispatch.</param>
    public static void Route(ushort messageID, byte groupID, ushort fromClientID, Riptide.Message msg)
    {
        var key = (messageId: messageID, groupId: groupID);

        if (!Handlers.TryGetValue(key, out var handler))
        {
            LogManager.Net.Warn($"[MessageRouter] No handler for MessageID={GetHandlerName(messageID)}, Group={GetGroupName(groupID)}");
            return;
        }
        
        try
        {
            switch (groupID)
            {
                case (byte)GroupID.Server when handler is Action<ushort, Riptide.Message> serverHandler:
                    serverHandler(fromClientID, msg);
                    break;
                case (byte)GroupID.Client when handler is Action<Riptide.Message> clientHandler:
                    clientHandler(msg);
                    break;
                default:
                    throw new InvalidOperationException($"Handler signature mismatch for MessageID={messageID}, Group={groupID}");
            }
        }
        catch (Exception ex)
        {
            var groupName = GetGroupName(groupID);
            var handlerName = GetHandlerName(messageID);
            LogManager.Net.Error($"[MessageRouter] Handler {handlerName}/{groupName} Exception: {ex.Message}");
        }

    }
    
    private static string GetHandlerName(ushort messageID)
    {
        string handlerName;
        try
        {
            handlerName = ((MessageID)messageID).ToString();
        }
        catch
        {
            handlerName = $"{messageID}";
        }
        return handlerName;
    }

    private static string GetGroupName(byte groupID)
    {
        string groupName;
        try
        {
            groupName = groupID == (byte)GroupID.Server ? "Server" : "Client";
        }
        catch
        {
            groupName = $"{groupID}";
        }

        return groupName;
    }
}

[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
public class WKMessageHandlerAttribute : Attribute
{
    public ushort messageID { get; }
    public byte groupID { get; }

    public WKMessageHandlerAttribute(ushort _messageID, byte _groupID)
    {
        messageID = _messageID;
        groupID = _groupID;
    }
}