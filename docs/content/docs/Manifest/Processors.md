---
title: "Processors"
weight: 50
bookCollapseSection: false
---

# Processors

A processor is responsible for 

* parsing a mqtt message payload
* enriching and transforming the data
* translating the data to metrics or logs
* sending the data to an otel endpoint.

## The basic structure

{{% steps %}}
1. **General data**

   Here you can give your processor a name and a desciption. Additionally you can define a default otel server to be used inside the processor.

2. **Mqtt**

   Here you can configure the mqtt subscriptions that should be processed by the processor.

3. **Otel**

   The otel section contains of general otel parameters, like a name and a description and then defines the metrics and logs that will be created
   by the processor.

4. **Otel.Metrics**

   In this section you can define how to process a mqtt payload and generate open telemetry metrics out of it.

5. **Otel.Logs**

   In this section you can define how to process a mqtt payload and generate open telemetry logs out of it.
{{% /steps %}}

A simple processor will look like this:

```yaml
Processors:
  - Name: "Example processor"
    Description: "Provides sensor info from power sensors."
    Mqtt: 
      ...
    Otel:
      ...
```


The Processor consists of the following parameters:

| Parameter                          | Description                                                                                                  |
|------------------------------------|--------------------------------------------------------------------------------------------------------------|
| Name                               | An optional name for the processor.                                                                          |
| Description                        | An optional description of the processor.                                                                    |
| OtelServerName                     | A reference to an otel server, if not set the default server will be used.                                   |
| Mqtt                               | A section containing mqtt relevant parameters.                                                               |
| Otel                               | A section containint open telemetry relevant parameters.                                                     | 



## The Mqtt section

The `Mqtt`section defines the payloads that should be processed by the processor. It subscribes to mqtt topics can add variables to the subscriptions and
can apply transformation expressions to the payload, before it reaches the otel processor.

A simple example for the mqtt would look like this:

```yaml
    Mqtt: 
      Name: "My mqtt configuration"
      Variables:
        - Key: "MyVariable"
          Value: "42"
      Subscriptions:
        - Name: "My subscription"
          Topic: "sub-topic"
      SubscriptionGroups:
        - Name: "Tasmota Plugs"
```

It consists of the following parameters:

| Parameter             | Description                                                                                                                     |
|-----------------------|---------------------------------------------------------------------------------------------------------------------------------|
| Name                  | An optional name for the mqtt configuration.                                                                                    |
| Description           | An optional description.                                                                                                        |
| Variables             | A list of [variables](../variables) that will be applied to all subscriptions.                                                  |
| Subscriptions         | A list of [subscriptions](../subscription/#configure-subscriptions) to which the processor will subscribe                       |
| SubscriptionGroups    | A list of [subscription groups](../subscription/#subscription-groups) to which the processor will subscribe                     |
| Broker                | The (optional) broker that will be applied to all subscriptions and subscription groups that do not have a specific broker set. | 
| Transform             | An optional transform expression that will be applied to all received message payloads. [{{< badge style="info" title="supports" value="transformations" >}}](/docs/expressions/#transformations)                                        | 

The processor will subscribe to all subscriptions (and subscription groups) in the `Mqtt` section. When a message for one of the subscriptions is received 
the message is transformed (if `Transform` is set) and afterwards the `Otel` section will be executed. 

## The Otel section

### Basics

The `Otel`section defines how the payloads will be processed, enriched and then send as logs or metrics to an open telemetry endpoint.

A simple example for the Otel would look like this:

```yaml
    Otel:
      Name: "My otel section"
      Attributes:
        - Key: SensorName
          Value: $SensorName
        - Key: DeviceName
          Value: $DeviceName
      Metrics:
        ...
      Logs:
        ...
```

It consists of the following parameters:

| Parameter       | Description                                                                                                                             |
|-----------------|-----------------------------------------------------------------------------------------------------------------------------------------|
| Name            | An optional name for the otel configuration.                                                                                            |
| Description     | An optional description.                                                                                                                |
| Attributes      | A list of otel attributes that will be added to the otel signal. [{{< badge style="info" title="supports" value="variables" >}}](/docs/manifest/variables)          |
| OtelServerName  | The (optional) otel server name that will be applied to all `Metrics` and `Logs` sections that do not explicitly state the server name. | 
| Metrics         | An optional list of `Metrics` that will describe how the payload of a subscription message will be parsed into an otel metric signal.   | 
| Logs            | An optional list of `Logs` that will describe how the payload of a subscription message will be parsed into an otel log message.        | 

## The Otel metrics section

When a mqtt message payload is received and a `Metric` section exists it will create an open telemetry metric for the given payload.

It consists of the following parameters:

| Parameter                  | Description                                                                                                                    |
|----------------------------|--------------------------------------------------------------------------------------------------------------------------------|
| Name                       | The name of the created metric that will be send to the open telemetry endpoint.                                               |
| Description                | The optional description that will be send to the open telemetry endpoint.                                                     |
| Attributes                 | A list of otel attributes that will be added to the otel metric. [{{< badge style="info" title="supports" value="variables" >}}](/docs/manifest/variables) |
| OtelServerName             | The (optional) otel server name that will be applied to the metric. If not set the default server is used.                     | 
| Instrument                 | Defines the otel metric instruments to be used. See [otel instruments](#otel-instruments) for details.                         | 
| SignalDataType             | The data type of the metric. See [otel data types](#otel-data-types) for details.                                              | 
| Unit                       | The optional unit that will be sent to the open telemetry endpoint as part of the metric.                                      | 
| Value                      | The value of the metric. Must be of type `SignalDataType`. [{{< badge style="info" title="supports" value="expressions" >}}](/docs/expressions/#expressions)     | 
| HistogramBucketBoundaries  | A list of bucket values for the `Histogram` instrument. See [histogram bucket boundaries](#histogram-bucket-boundaries).       | 

### Example:

```yaml
      Metrics:
        - Name: "Energy_Power_W"
          Description: "The current power consumption at the time of measurement in Watt."
          SignalDataType: Float
          Instrument: Gauge
          Unit: "W"
          Value: "JSONPATH('$.ENERGY.Power')"
```

### Otel instruments

Most instruments support a synchronous and an asynchronous mode. Synchronous instruments will be send directly to the otel endpoint, while
asynchronous instruments will be collected by the endpoint at the configured sample rate.

The following open telemetry instruments are supported:

| Otel Instrument       | Synchronous name | Asynchronous name         |
|-----------------------|------------------|---------------------------|
| Gauge                 | Gauge            | AsynchronousGauge         |
| Counter               | Counter          | AsynchronousCounter       |
| UpDownCounter         | UpDownCounter    | AsynchronousUpDownCounter |
| Histogram             | Histogram        |                           |

### Otel data types

The otel processor supports the following metric data types:

* Float
* Int
* Double
* Long
* Decimal
* String

### Histogram bucket boundaries

When using a histogram instrument you can explicitly set the bucket boundaries for the instrument.

Example:

```yaml
          HistogramBucketBoundaries:
            - 0
            - 0.5
            - 0.8
            - 1
```

## The Otel logs section

### Basics

When a mqtt message payload is received and a `Logs` section exists it will create an open telemetry log entry for the given payload.

It consists of the following parameters:

| Parameter      | Description                                                                                                                                               |
|----------------|-----------------------------------------------------------------------------------------------------------------------------------------------------------|
| Name           | The optional name of the log processor.                                                                                                                   |
| Description    | The optional description.                                                                                                                                 |
| Attributes     | A list of otel attributes that will be added to the otel log entry. [{{< badge style="info" title="supports" value="variables" >}}](/docs/manifest/variables)                         |
| OtelServerName | The (optional) otel server name of the otel endpoint where the log entry should be send. If not set the default server is used.                           | 
| Filter         | Defines the filter expression that will be applied if the `PayloadType` is set to `Text`. [{{< badge style="info" title="supports" value="expressions" >}}](/docs/expressions/#expressions) | 
| PayloadType    | The type of the payload, that the processor will process. Must be one of the following: `Text` or `Json`                                                  | 
| CategoryName   | The category name, that will be send with the open telemetry log entry. Default is `mqtt2otel`                                                            | 
| Transform      | An optional transform expression that will be applied to the message payloads. [{{< badge style="info" title="supports" value="transformations" >}}](/docs/expressions/#transformations)            | 
| MessageKey     | If `PayloadType`is `Json` this is the key that will be used for identifying the message body. Default is: `otel_message`                                  | 
| LogLevelKey    | If `PayloadType`is `Json` this is the key that will be used for identifying the log level. Default is: `otel_loglevel`                                    | 

### Example:

```yaml {hl_lines=[3,4]}
      Logs:
        - Name: "Logging"
          PayloadType: Json
          Transform: "GROK('%{TIME:otel_timestamp} %{WORD:category}: %{GREEDYDATA:otel_message}')"
```

### PayloadType Json

Whe using the payload type `Json` the created json parameters will be interpreted as attributes, that are added to the log message. Some parameters (starting
with `otel_`) have a special meaning and are not treated as attributes:

| Parameter       | Description                                                                                              |
|-----------------|----------------------------------------------------------------------------------------------------------|
| otel_timestamp  | Will be send as the log timestamp.                                                                       |
| otel_loglevel   | Will be send as the log level of the message.                                                            |
| otel_message    | Will be send as the message body.                                                                        |

The following json message:

```json
{
  "otel_timestamp": "2026-02-26T10:28:34Z",
  "otel_loglevel": "Info",
  "server_name": "ServerA",
  "otel_message": "Temperature value read successfully."
}
```

will be send as a log message send with timestamp `2026-02-26T10:28:34Z`, loglevel `Info` and message body `Temperature value read successfully`. The
attribute `server_name` with the value `ServerA` will be added to the message.
