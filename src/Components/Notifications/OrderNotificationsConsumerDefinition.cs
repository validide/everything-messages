﻿using System;
using EverythingMessages.Infrastructure;
using MassTransit;

namespace EverythingMessages.Components.Notifications;

public class OrderNotificationsConsumerDefinition: ConsumerDefinition<OrderNotificationsConsumer>
{
    private readonly EndpointConfigurationOptions _endpointConfigurationOptions;
    private readonly IEndpointNameFormatter _endpointNameFormatter;
    public OrderNotificationsConsumerDefinition(EndpointConfigurationOptions endpointConfigurationOptions, IEndpointNameFormatter endpointNameFormatter)
    {
        _endpointConfigurationOptions = endpointConfigurationOptions;
        _endpointNameFormatter = endpointNameFormatter;
        ConcurrentMessageLimit = _endpointConfigurationOptions.ConcurrentMessageLimit ?? 1;
        Endpoint(e =>
        {
            e.InstanceId = _endpointNameFormatter.SanitizeName($"{_endpointConfigurationOptions.Name}_{Guid.NewGuid():N}");
            e.Temporary = true;
        });
    }
}
