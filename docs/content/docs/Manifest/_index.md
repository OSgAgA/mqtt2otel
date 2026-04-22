---
title: "The manifest"
weight: 30
bookCollapseSection: false
---

# The Manifest
The mapping between MQTT and Otel is configured via a file called `Manifest.yaml` (or whatever is configured in 
[Application Settings](../ApplicationSettings)).

The structure of the file is as following

## File structure

{{% steps %}}
1. ## Version
   The version must be set and must be the first line of the file. If not set, or not being on the first line, the file will be declined. Currently the following versions are supported: `1.0`.

2. ## MqttBroker
   In this section the available Mqtt broker connections will be configured. For details, see [Mqtt broker](MqttBroker). [{{< badge style="info" title="supports" value="ImportFrom" >}}](organize)

3. ## OtelServer
   In this section the available open telemetry connections will be configured. For details, see [Otel server](OtelServer). [{{< badge style="info" title="supports" value="ImportFrom" >}}](organize)

4. ## SubscriptionGroups
   A list of grouped subscriptions that can be referred later in the otel section. For details see [Subscription groups](subscription/#subscription-groups). [{{< badge style="info" title="supports" value="ImportFrom" >}}](organize)

5. ## Processors
   A list of processors, that will take mqtt payloads, processes them and then create otel logs or metrics. For details see [Processors](Processors). [{{< badge style="info" title="supports" value="ImportFrom" >}}](organize)

6. ## How to organize large manifests.
   Find out how you can organize complex scenarios in your manifest file. See [Organize manifest files](organize).

{{% /steps %}}

As a starting point, this is an example manifest using logs, and metrics:

```yaml
Version: 1.0

MqttBroker:
  - Name: "My broker"
    Endpoint:
      Port: 32007
      Address: "mymqtt-broker.net"
      EnableTls: false

OtelServer:
  - Name: "My Otel server"
    ServiceName: "my-service"
    ServiceNamespace: "my-service-namespace"
    Endpoint:
      Protocol: "http"
      Port: 32014
      Address: "my-otel-collector.net"
      EnableTls: false

Processors:
  - Name: "Processor Temperature"
    Description: "Provides the current processor temperature."
    Mqtt: 
      Subscriptions:
        - Name: "Processor information"
          Topic: "message-topic"
    Otel:
      Metrics:
        - Name: "Processor.Temperature"
          Description: "The current processor temperature."
          SignalDataType: Float
          Instrument: Gauge
          Value: "JSONPATH('$.Processor.Temperature')"

  - Name: "Server logs"
    Description: "Collect all log messages from the server."
    Mqtt:
      Subscriptions:
        - Name: "Server logs"
          Topic: "message-log-topic"
    Otel:
      Attributes:
        - Key: Location
          Value: MainServerRoom
      Logs:
        - Name: "Logging"
          PayloadType: Json
          Transform: "GROK('%{TIME:otel_timestamp} [%{WORD:otel_loglevel}] [%{WORD:server_name}] %{GREEDYDATA:otel_message}')"

```