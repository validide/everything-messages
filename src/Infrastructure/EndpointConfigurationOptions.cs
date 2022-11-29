using System;

namespace EverythingMessages.Infrastructure;

public class EndpointConfigurationOptions
{
    private readonly Lazy<bool> _inContainer = new(() =>
        Boolean.TryParse(Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER"), out var inContainer)
        && inContainer
    );

    public string Name { get; set; }
    public string SchedulerQueue { get; set; }
    public int? ConcurrentMessageLimit { get; set; }
    public bool WaitBusStart { get; set; }
    public string MessageBrokerUri { get; set; }
    public string DocumentStoreUri { get; set; }

    public bool InContainer => _inContainer.Value;

    public string GetMessageBrokerEndpoint()
    {
        if (!String.IsNullOrEmpty(MessageBrokerUri))
            return MessageBrokerUri;

        return InContainer ? "message-broker" : "localhost";
    }

    public string GetDocumentStoreEndpoint()
    {
        if (!String.IsNullOrEmpty(DocumentStoreUri))
            return DocumentStoreUri;

        return InContainer ? "mongodb://message-broker:27017" : "mongodb://localhost:27017";
    }
}
