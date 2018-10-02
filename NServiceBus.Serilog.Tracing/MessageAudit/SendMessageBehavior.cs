﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NServiceBus.Pipeline;
using Serilog;
using Serilog.Events;
using Serilog.Parsing;

class SendMessageBehavior : Behavior<IOutgoingLogicalMessageContext>
{
    ILogger logger;
    MessageTemplate messageTemplate;

    public SendMessageBehavior(LogBuilder logBuilder)
    {
        var templateParser = new MessageTemplateParser();
        logger = logBuilder.GetLogger("NServiceBus.Serilog.MessageSent");
        messageTemplate = templateParser.Parse("Sent message {MessageType} {MessageId}.");
    }

    public override Task Invoke(IOutgoingLogicalMessageContext context, Func<Task> next)
    {
        var message = context.Message;
        var properties = new List<LogEventProperty>
        {
            new LogEventProperty("MessageType", new ScalarValue(message.MessageType))
        };

        if (logger.BindProperty("Message", message.Instance, out var messageProperty))
        {
            properties.Add(messageProperty);
        }

        if (logger.BindProperty("MessageId", context.MessageId, out var messageId))
        {
            properties.Add(messageId);
        }

        properties.AddRange(logger.BuildHeaders(context.Headers));
        logger.WriteInfo(messageTemplate, properties);
        return next();
    }

    public class Registration : RegisterStep
    {
        public Registration(LogBuilder logBuilder)
            : base(
                stepId: "SerilogSendMessage",
                behavior: typeof(SendMessageBehavior),
                description: "Logs outgoing messages",
                factoryMethod: builder => new SendMessageBehavior(logBuilder))
        {
        }
    }
}