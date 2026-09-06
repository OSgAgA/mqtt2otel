---
title: "Quickstart"
weight: 0
bookCollapseSection: false
---
# Quick start

## Installation

See [Installation](../../installation) overview.

## Connect to the MQTT Broker and Otel Server

The mapping between MQTT and Otel is configured via a file called `Manifest.yaml`. Here's an example of a simple configuration file that connects to an MQTT broker at `http://mymqtt-broker.net:32007` and an OpenTelemetry collector at `http://my-otel-collector.net:32014`:

{{< exampleCode id="doc-01" field="Manifest" lang="yaml">}}

This assumes no credentials are required to log into the broker or the Otel collector. For further configuration options, see [Configure MQTT Broker](todo) and [Configure Otel Server](todo).

### Breakdown of the configuration

* **MQTT Broker**:

  * `Name`: An identifier for the MQTT broker.
  * `Endpoint`: Describes the broker's address and port.
  
* **Otel Connections**:

  * `Name`: An identifier for the Otel connection.
  * `ServiceName`: The name of the service.
  * `ServiceNamespace`: The namespace for the service.
  * `Endpoint`: Describes the Otel collector's address and port.

## Subscribe to a Topic and Generate a Metric

Now that we have connected to the MQTT broker and Otel server, let's subscribe to an MQTT topic and generate an Otel metric from the payload.

Suppose the server sends messages to the topic `{{< exampleData id="doc-2" field="Topic">}}` in the following JSON format:

{{< exampleCode id="doc-02" field="Payload" lang="yaml" hl_lines="4">}}

To extract the temperature, we will use the [JSONPath](https://www.rfc-editor.org/rfc/rfc9535) syntax `$.Processor.Temperature`.
The corresponding YAML would look like this:

{{< exampleCode id="doc-02" field="Manifest" lang="yaml">}}

This configuration subscribes to the MQTT topic `{{< exampleData id="doc-2" field="Topic">}}` and creates an Otel metric called `Processor.Temperature` with 
a `float` data type and a `Gauge` instrument. Every time a message is received for the topic `{{< exampleData id="doc-2" field="Topic">}}`, the temperature value 
parsed from the provided json and sent to the Otel endpoint.

The syntax is as following:

* Processors contains a list of processors. A processor is able to receive mqtt messages, process them and send them to the
  configured otel endpoint.
* The Processor consists of two parts:
    * Mqtt
        * A list of mqtt topic subscriptions, consisting of
            - A name
            - The topic to which they subscribe.
    * Otel
        * A list of metrics, that should be generated from the message payload. It consists of
            - Name and description
            - The data type of the signal that will be send to the otel endpoint
            - The otel instrument
            - The value of the metric that will be send to the otel endpoint.
            
## Variables and Attributes

Subscriptions can have variables, which can be used later in the rules section. Here’s an example of how to define variables:

{{< exampleCode id="doc-03" field="Manifest" lang="yaml" hl_lines="7-9">}}

You can access variables in Otel rules by prefixing them with a `$` sign. For example, to access the `SensorName`, you would use
`$SensorName`.

Otel rules can also include attributes, which are added to the Otel signal for filtering or grouping. You can use variables
inside attributes where needed. Here’s an example of how to add attributes:

{{< exampleCode id="doc-04" field="Manifest" lang="yaml" hl_lines="4-8 12-14">}}

The attributes directly added under the Metrics section will be added to all metrics. The attributes added to the 
"Processor.Temperature" metric will only be added to this metric. 


### Resulting Signal Attributes:

| Attribute Name     | Attribute Value     |
| ------------------ | ------------------- |
| SensorName         | ProcessorServerA    |
| MeasurementQuality | 10                  |
| Location           | Main server room    |

## Working with Expressions

We’ve already used an expression to parse the payload with `JSONPATH('$.Processor.Temperature')`. However, you can also perform 
mathematical transformations. For example, to convert the temperature from Celsius to Fahrenheit, you can use this expression:

{{< exampleCode id="doc-05" field="Description" lang="yaml">}}

Standard mathematical operations like `+`, `-`, `*`, `/`, and functions such as `SQRT`, `Sin`, `Cos`, `Tan`, and constants 
like `[Pi]` are supported.

### Available Functions

| Function   | Example                   | Description                                                                                                                                          |
| ---------- | ------------------------- | ----------------------------------------                                                                                                             |
| `JSONPATH` | `JSONPATH('$.Root')`      | Extracts data using [JSONPATH](https://www.rfc-editor.org/rfc/rfc9535) syntax                                                                        |
| `XPATH`    | `XPATH('/root/child[1]')` | Extracts data using [XPath](https://www.w3.org/TR/xpath-31/) syntax                                                                                  |
| `REGEX`    | `REGEX('[0-9]+')`         | Extracts data using a [regular expression](https://learn.microsoft.com/en-us/dotnet/standard/base-types/regular-expression-language-quick-reference). If the regular expression returns more than one match, then the first match is used. |
| `VAR`      | `VAR('MyVariable')`       | Returns the variable with the given name. No `$` is needed before the variable name.                                                                 |
| `PAYLOAD`  | `PAYLOAD()`               | Returns the raw payload                                                                                                                              |
| `CONST`    | `CONST('42')`             | Returns a constant value                                                                                                                             |

For more details, please refer to the [documentation](/docs/expressions).

## Log Messages and Transformation

Log messages work similarly to metrics. Let's say you receive a log message payload in the following format from MQTT:

{{< exampleCode id="doc-06" field="Payload" lang="dissect">}}

Rather than sending the raw message to Otel, we can transform it into a structured log format using an extended 
[DISSECT](https://github.com/OSgAgA/Dissect.Extended.Net) expression:

{{< exampleCode id="doc-06" field="Description" lang="yaml">}}

This can be read as:

* Parse date and time and name it otel_timestamp
* Read a space and a [ and discard the information
* Read everything up until ] and name it otel_loglevel
* Read ] [ and discard the information
* Read everything up until ] and name it server_name
* Read ] [ and discard the information
* Read the remaining part of the message and name it otel_message

This expression can then be used in a `Transform` expression inside the `Logs` section:

{{< exampleCode id="doc-06" field="Manifest" lang="yaml" hl_lines="14-15">}}

You should notice, that we are using the `Logs` keyword now in the otel section to identify log messages. The `Transform` 
expression will convert the log into a JSON structure like this:

```json
{
  "otel_timestamp": "2026-02-26T10:28:34Z",
  "otel_loglevel": "Info",
  "server_name": "ServerA",
  "otel_message": "Temperature value read successfully."
}
```

Since we’ve specified `PayloadType: Json`, Otel will interpret the top-level keys 
(`otel_timestamp`, `otel_loglevel`, `server_name`, and `otel_message`) as attributes in the log message.
Attributes starting with "otel_" will have a special meaning so they are interpreted not as attributes but as the 
message body, timestamp and log level.

## Subscription Groups

To avoid repetition and reuse the same subscriptions across different metrics or logs, you can group them into 
**Subscription Groups** and refer to them later. This is useful when you have e.g. multiple devices or sensors sending data 
under the same topic structure but need to handle them differently in your rules.

### Example Scenario

Let’s say you have a device that sends both power consumption metrics (like current, power, voltage) and status information 
(like the microcontroller core temperature) in the same MQTT message. The message payload is structured as follows:

{{< exampleCode id="doc-07" field="Payload" lang="json">}}

You want to treat power metrics separately from the microcontroller status. To achieve this, you can group the subscriptions 
into a `SubscriptionGroup` for reuse:

### Defining a Subscription Group

{{< exampleCode id="doc-07" field="Description" lang="yaml">}}

Here, we define a **Subscription Group** called `Power sensors`, which includes two subscriptions: 
one for a washing machine and another for a dryer. Both subscriptions have associated variables that can be used later in the 
metrics or logs.

### Using Subscription Groups in Metrics and Logs

Once you’ve created the `Power sensors` group, you can refer to it in your **metrics** or **logs** as follows:

{{< exampleCode id="doc-07" field="Manifest" lang="yaml" hl_lines="22-23 38-39">}}

In this example:

* **Power Metrics**: We create a metric for power data (e.g., `Power`, `Voltage`), subscribing to the `Power sensors` group.
* **Processor Status**: We create another metric for processor data (e.g., `Temperature`), also subscribing to the same `Power sensors` group.

Both metrics will use different attributes.

### Grouping Devices with Different Topics

Sometimes, devices may use different MQTT topics but contain the same identifier. For example, you might have multiple topics 
for each sensor:

| Sensor | Topic              | Description                   |
| ------ | ------------------ | ----------------------------- |
| 1234   | `tele/1234/sensor` | Power metrics for sensor 1234 |
| 1234   | `stat/1234/logs`   | Logs for sensor 1234          |
| 9876   | `tele/9876/sensor` | Power metrics for sensor 9876 |
| 9876   | `stat/9876/logs`   | Logs for sensor 9876          |

To manage these different topics, you can group them under a common **Subscription Group** and then specify **ParentPath** 
and **SubPath** to correctly target the topics.

### Defining Subscription Groups with ParentPath and SubPath

{{< exampleCode id="doc-08" field="Manifest" lang="yaml" hl_lines="15-16 30-31">}}

### Explanation:

* **ParentPath**: This specifies the top-level directory or prefix of the topic. For example, `tele` for telemetry data or `stat` for status/log data.
* **SubPath**: This specifies the specific subtopic or suffix that targets a specific part of the topic.

#### Example with the above configuration:

* The **Power Metrics** rule will subscribe to topics like `tele_1234_sensor` and `tele_9876_sensor` using the `ParentPath` `tele` and `SubPath` `sensor`.
* The **Sensor Logs** rule will subscribe to topics like `stat_1234_logs` and `stat_9876_logs` using the `ParentPath` `stat` and `SubPath` `logs`.

### Final Thoughts

By using **Subscription Groups**, you can easily reuse configurations across different rules, making your setup more modular 
and scalable. Grouping devices and topics this way allows you to handle complex MQTT topic structures efficiently.

## Complete example manifest

Here is a complete minimal example manifest using logs, and metrics:

{{< exampleCode id="doc-09" field="Manifest" lang="yaml">}}
