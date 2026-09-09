---
title: "Quickstart"
weight: 0
bookCollapseSection: false
---

# Quick start

## Installation

See the [Installation](../../installation) overview.

## Connect to the MQTT Broker and Otel Server

The mapping between MQTT and OpenTelemetry is defined in a configuration file called `Manifest.yaml`.  
Below is an example of a minimal configuration that connects to an MQTT broker at  
`http://mymqtt-broker.net:32007` and an OpenTelemetry collector at  
`http://my-otel-collector.net:32014`:

{{< exampleCode id="doc-01" field="Manifest" lang="yaml">}}

This example assumes that neither the MQTT broker nor the Otel collector requires authentication.  
For additional configuration options, see [Configure MQTT Broker](todo) and [Configure Otel Server](todo).

### Breakdown of the configuration

* **MQTT Broker**  
  * `Name`: A unique identifier for the MQTT broker.  
  * `Endpoint`: The broker’s address and port.

* **Otel Connections**  
  * `Name`: A unique identifier for the Otel connection.  
  * `ServiceName`: The name of the service.  
  * `ServiceNamespace`: The namespace of the service.  
  * `Endpoint`: The collector’s address and port.

## Subscribe to a Topic and Generate a Metric

After connecting to the MQTT broker and Otel server, you can subscribe to an MQTT topic and generate an Otel metric from incoming messages.

Assume the server publishes messages to the topic `{{< exampleData id="doc-2" field="Topic">}}` in the following JSON format:

{{< exampleCode id="doc-12" field="Payload" lang="yaml">}}

You can parse this payload using the processor below, which automatically creates metric signals based on the JSON structure:

{{< exampleCode id="doc-12" field="Manifest" lang="yaml">}}

This configuration subscribes to the MQTT topic `{{< exampleData id="doc-12" field="Topic">}}` and generates two Otel `Gauge` metrics:  
`Data.Temperature` and `Data.Angle`. The data types are detected automatically.

The syntax works as follows:

* `Processors` contains a list of processors. Each processor receives MQTT messages, processes them, and sends the results to the configured Otel endpoint.
* A processor consists of two parts:
  * **Mqtt**
    * A list of MQTT topic subscriptions, each with:
      - A name  
      - The topic to subscribe to
  * **Otel**
    * `ParseAs`: Defines how the processor interprets the MQTT payload:
      - `Type`: The payload type, e.g., `Json`
      - `NameOnly`: If `true`, hierarchy is removed from the signal name.  
        For example, instead of `Data.Temperature`, the metric becomes `Temperature`.
      - `Separator`: Defines the separator for hierarchy levels.  
        For example, `_` turns `Data.Temperature` into `Data_Temperature`.

> [!NOTE]
>  **Did you know?**
>
>  Have you noticed the explorer icon on the upper right corner of the example code? If you click on it, you will be redirected to the
>  **[mqtt2otel explorer](https://explorer.mqtt2otel.org/)**, where you can play around with the examples and inspect the generated output.

You can further adjust names using `NameFormatter`, and you can convert values—for example, to a different unit.

Given the following payload:

{{< exampleCode id="doc-13" field="Payload" lang="yaml">}}

You can convert the value from °F to °C and format the metric name in camel case using this processor:

{{< exampleCode id="doc-13" field="Manifest" lang="yaml">}}

For more details, refer to the [documentation](/docs/expressions/converter-and-formatter).

## Working with Expressions

In the previous example, we used expressions to convert values and format names.  
Expressions are a central and powerful concept in the application, allowing you to adjust generated signals in many ways.

Expressions support standard mathematical operations (`+`, `-`, `*`, `/`), functions such as `SQRT`, `Sin`, `Cos`, `Tan`, and constants like `[Pi]` or `[e]`.

For more details, see the [documentation](/docs/expressions).

If you need to adjust signals based on complex conditions, refer to [conditional actions](/docs/expressions/actions), 
which are beyond the scope of this quickstart.

## Manually creating a metric

If your payload cannot be parsed automatically, or if you need fine‑grained control over the generated signal, you can define metrics manually.

Assume the server publishes messages to the topic `{{< exampleData id="doc-2" field="Topic">}}` in this JSON format:

{{< exampleCode id="doc-02" field="Payload" lang="yaml" hl_lines="4">}}

To extract the temperature, use the [JSONPath](https://www.rfc-editor.org/rfc/rfc9535) expression `$.Processor.Temperature`.  
The corresponding YAML looks like this:

{{< exampleCode id="doc-02" field="Manifest" lang="yaml">}}

This configuration subscribes to the MQTT topic `{{< exampleData id="doc-2" field="Topic">}}` and creates an Otel metric called `Processor.Temperature` with:

* a `float` signal data type is explicitly set, instead of the autodetected integer.  
* a `Gauge` instrument  
* a `Unit` of type `C`
* a value extracted from the JSON payload

Each time a message arrives on the topic, the temperature is parsed and sent to the Otel endpoint.

The syntax works as follows:

* `Processors` contains a list of processors.  
* A processor consists of:
  * **Mqtt**
    * A list of topic subscriptions, each with:
      - A name  
      - The topic
  * **Otel**
    * A list of metrics to generate from the payload, each with:
      - Name and description  
      - Data type  
      - Instrument  
      - Value expression  
      - Unit

## Variables and Attributes

Subscriptions can define variables that you can later use in rules.  
Here is an example:

{{< exampleCode id="doc-03" field="Manifest" lang="yaml" hl_lines="7-9">}}

Access variables in Otel rules by prefixing them with `$`. For example, `$SensorName`.

Otel rules can also include attributes, which are added to the generated signals for filtering or grouping. Variables can 
be used inside attributes as needed. 

Example:

{{< exampleCode id="doc-04" field="Manifest" lang="yaml" hl_lines="4-8 12-14">}}

Attributes defined directly under `Processor` apply to all metrics inside the processor. Attributes defined under a 
specific metric apply only to that metric.

### Resulting Signal Attributes:

| Attribute Name     | Attribute Value     |
| ------------------ | ------------------- |
| SensorName         | ProcessorServerA    |
| MeasurementQuality | 10                  |
| Location           | Main server room    |

### Attributes via message topic

In addition to manual attributes and variable‑based attributes, you can extract attributes from the MQTT topic itself.

Given a topic like:

{{< exampleCode id="doc-14" field="Topic" lang="yaml">}}

You can see that the location (`germany`) and device ID (`1234`) are encoded in the topic.  
The following processor extracts them using the TopicAttributes property:

{{< exampleCode id="doc-14" field="Manifest" lang="yaml">}}

This produces:

| Attribute Name     | Attribute Value     |
| ------------------ | ------------------- |
| location           | germany             |
| device.Id          | 1234                |

More information about topic parsing is available [here](/docs/expressions/topicparsing).

### Attributes via MQTT user properties

MQTT user properties are automatically converted into OpenTelemetry attributes. To disable this behavior, 
set `CreateAttributesFromUserProperties` to `false`.

To use user properties inside expressions, access them via `UserProperty(name)`. More information about available 
functions can be found [here](/docs/expressions/).

## Log Messages and Transformation

Log messages work similarly to metrics.  
Assume you receive a log message payload in this format:

{{< exampleCode id="doc-06" field="Payload" lang="dissect">}}

Instead of forwarding the raw message to Otel, you can transform it into structured log data using an extended  
[DISSECT](https://github.com/OSgAgA/Dissect.Extended.Net) expression:

{{< exampleCode id="doc-06" field="Description" lang="yaml">}}

This expression performs the following steps:

* Parse date and time → `otel_timestamp`  
* Read a space and `[` → discard  
* Read everything until `]` → `otel_loglevel`  
* Read `] [` → discard  
* Read everything until `]` → `server_name`  
* Read `] [` → discard  
* Read the remaining message → `otel_message`

You can use this expression inside a `Transform` rule in the `Logs` section:

{{< exampleCode id="doc-06" field="Manifest" lang="yaml" hl_lines="14-15">}}

Note that the `Logs` keyword is used to identify log messages.  
The `Transform` expression converts the log into a JSON structure like:

```json
{
  "otel_timestamp": "2026-02-26T10:28:34Z",
  "otel_loglevel": "Info",
  "server_name": "ServerA",
  "otel_message": "Temperature value read successfully."
}
```


Since PayloadType: Json is specified, Otel interprets the top‑level keys as log attributes.
Attributes starting with otel_ have special meaning and are interpreted as the message body, timestamp, and log level.

You can also explicitly set the timezone of the parsed log entry:

{{< exampleCode id="doc-15" field="Manifest" lang="yaml" hl_lines="14">}}

More information about transformations can be found [here](/docs/expressions/#transformations).

# Complete example manifest

Below is a complete minimal example manifest using logs and metrics:

{{< exampleCode id="doc-09" field="Manifest" lang="yaml">}}
