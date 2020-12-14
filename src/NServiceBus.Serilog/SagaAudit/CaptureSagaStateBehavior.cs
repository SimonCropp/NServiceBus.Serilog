﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NServiceBus;
using NServiceBus.Pipeline;
using NServiceBus.Sagas;
using NServiceBus.Serilog;
using Serilog.Events;
using Serilog.Parsing;

class CaptureSagaStateBehavior :
    Behavior<IInvokeHandlerContext>
{
    bool useFullTypeName;
    MessageTemplate messageTemplate;

    public CaptureSagaStateBehavior(bool useFullTypeName)
    {
        this.useFullTypeName = useFullTypeName;
        MessageTemplateParser templateParser = new();
        messageTemplate = templateParser.Parse("Saga execution '{SagaType}' '{SagaId}'.");
    }

    public override async Task Invoke(IInvokeHandlerContext context, Func<Task> next)
    {
        if (!(context.MessageHandler.Instance is Saga))
        {
            // Message was not handled by the saga
            await next();
            return;
        }

        var logger = context.Logger();
        if (!logger.IsEnabled(LogEventLevel.Information))
        {
            await next();
            return;
        }

        SagaUpdatedMessage sagaAudit = new(DateTimeOffset.UtcNow);
        context.Extensions.Set(sagaAudit);
        await next();

        if (context.Extensions.TryGet(out ActiveSagaInstance activeSagaInstance))
        {
            sagaAudit.SagaType = activeSagaInstance.Instance.GetType().Name;

            sagaAudit.FinishTime = DateTimeOffset.UtcNow;
            AuditSaga(activeSagaInstance, context, sagaAudit);
        }
    }

    void AuditSaga(ActiveSagaInstance activeSagaInstance, IInvokeHandlerContext context, SagaUpdatedMessage sagaAudit)
    {
        var saga = activeSagaInstance.Instance;

        if (saga.Entity == null)
        {
            //this can happen if it is a timeout or for invoking "saga not found" logic
            return;
        }

        var headers = context.Headers;
        if (!headers.TryGetValue(Headers.MessageId, out var messageId))
        {
            return;
        }

        var intent = context.MessageIntent();

        sagaAudit.IsNew = activeSagaInstance.IsNew;
        sagaAudit.IsCompleted = saga.Completed;
        sagaAudit.SagaId = saga.Entity.Id;

        AssignSagaStateChangeCausedByMessage(context, sagaAudit);

        List<LogEventProperty> properties = new()
        {
            new("SagaType", new ScalarValue(sagaAudit.SagaType)),
            new("SagaId", new ScalarValue(sagaAudit.SagaId)),
            new("StartTime", new ScalarValue(sagaAudit.StartTime)),
            new("FinishTime", new ScalarValue(sagaAudit.FinishTime)),
            new("IsCompleted", new ScalarValue(sagaAudit.IsCompleted)),
            new("IsNew", new ScalarValue(sagaAudit.IsNew))
        };

        var logger = context.Logger();
        var messageType = context.MessageType();
        if (!useFullTypeName)
        {
            messageType = TypeNameConverter.GetName(messageType);
        }

        Dictionary<ScalarValue, LogEventPropertyValue> initiator = new()
        {
            {new ScalarValue("IsSagaTimeout"), new ScalarValue(context.IsTimeoutMessage())},
            {new ScalarValue("MessageId"), new ScalarValue(messageId)},
            {new ScalarValue("OriginatingMachine"), new ScalarValue(context.OriginatingMachine())},
            {new ScalarValue("OriginatingEndpoint"), new ScalarValue(context.OriginatingEndpoint())},
            {new ScalarValue("MessageType"), new ScalarValue(messageType)},
            {new ScalarValue("TimeSent"), new ScalarValue(context.TimeSent())},
            {new ScalarValue("Intent"), new ScalarValue(intent)}
        };
        properties.Add(new LogEventProperty("Initiator", new DictionaryValue(initiator)));

        if (sagaAudit.ResultingMessages.Any())
        {
            if (logger.BindProperty("ResultingMessages", sagaAudit.ResultingMessages, out var resultingMessagesProperty))
            {
                properties.Add(resultingMessagesProperty);
            }
        }

        if (logger.BindProperty("Entity", saga.Entity, out var sagaEntityProperty))
        {
            properties.Add(sagaEntityProperty);
        }

        logger.WriteInfo(messageTemplate, properties);
    }

    static void AssignSagaStateChangeCausedByMessage(IInvokeHandlerContext context, SagaUpdatedMessage sagaAudit)
    {
        if (!context.Headers.TryGetValue("NServiceBus.Serilog.SagaStateChange", out var sagaStateChange))
        {
            sagaStateChange = string.Empty;
        }

        var stateChange = "Updated";
        if (sagaAudit.IsNew)
        {
            stateChange = "New";
        }

        if (sagaAudit.IsCompleted)
        {
            stateChange = "Completed";
        }

        if (!string.IsNullOrEmpty(sagaStateChange))
        {
            sagaStateChange += ";";
        }

        sagaStateChange += $"{sagaAudit.SagaId}:{stateChange}";

        context.Headers["NServiceBus.Serilog.SagaStateChange"] = sagaStateChange;
    }

    public class Registration :
        RegisterStep
    {
        public Registration(bool useFullTypeName) :
            base(
                stepId: $"Serilog{nameof(CaptureSagaStateBehavior)}",
                behavior: typeof(CaptureSagaStateBehavior),
                description: "Records saga state changes",
                factoryMethod: _ => new CaptureSagaStateBehavior(useFullTypeName))
        {
            InsertBefore("InvokeSaga");
        }
    }
}