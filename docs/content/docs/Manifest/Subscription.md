---
title: "Subscriptions and subscription groups"
weight: 40
bookCollapseSection: false
---

# Mqtt subscriptions and subscription groups

## Configure subscriptions

To subscribe to a mqtt topic you can use a `Subscription` section inside a manifst file. It consists of a name and the topic you want to 
subscribe to.

Example:

```yaml
      Subscriptions:
        - Name: "My subscription name"
          Topic: "message-topic"
          Variables:
            - Key: "SensorName"
              Value: "MySensor"
```

It consists of the following parameters:

| Parameter                          | Description                                                                                                  |
|------------------------------------|--------------------------------------------------------------------------------------------------------------|
| Name                               | A name that must be given to the subscription. With this it can be refered to later.                         |
| Description                        | An optional description of the subscription.                                                                 |
| Variables                          | A list of [variables](Variables) that will be set for each procssing that is triggered via this subscription.|
| Topic                              | The topic to subscribe to.                                                                                   |
| Broker                             | Optional. Set the broker to be used. If not set, the default broker will be used.                            | 
| Transform                          | A transform expression that will be applied to the message before it is send to a processor [{{< badge style="info" title="supports" value="transformations" >}}](/docs/expressions/#transformations)    |

## Subscription Groups

To avoid repetition and reuse the same subscriptions across different metrics or logs, you can group them into 
**Subscription Groups** and refer to them later. This is useful when you have e.g. multiple devices or sensors sending data 
under the same topic structure but need to handle them differently in your rules.

### Example Scenario

Let’s say you have a device that sends both power consumption metrics (like current, power, voltage) and status information 
(like the microcontroller core temperature) in the same MQTT message. The message payload is structured as follows:

```json
{
  "Time": "2026-04-12T09:07:04",
  "ENERGY": {
    "Power": 0.000,
    "Voltage": 227,
    "Current": 0.000
  },
  "ESP32": {
    "Temperature": 37.4
  }
}
````

You want to treat power metrics separately from the microcontroller status. To achieve this, you can group the subscriptions 
into a `SubscriptionGroup` for reuse:

### Defining a Subscription Group

Example: 

```yaml
SubscriptionGroups:
  - Name: "Power sensors"
    Subscriptions:
      - Name: "Power Sensor washing machine"
        Topic: "1234"
        Variables:
          - Key: "SensorName"
            Value: "WashingMachine"
      - Name: "Power Sensor dryer"
        Topic: "sensor_9876"
        Variables:
          - Key: "SensorName"
            Value: "Dryer"

```

A subscription group has the following parameters:

| Parameter                          | Description                                                                                                                                   |
|------------------------------------|-----------------------------------------------------------------------------------------------------------------------------------------------|
| Name                               | A name that must be given to the subscription group. With this it can be refered to later.                                                    |
| Description                        | An optional description of the group                                                                                                          |
| Subscriptions                      | A list of subscriptions belonging to this group                                                                                               |
| Variables                          | Optional. A list of [variables](Variables) that will be set for each procssing that is triggered via any subscription in this group.          |
| Broker                             | Optional. Set the broker to be used. If not set, the default broker will be used. Can be overriden in a subscription.                         | 
| Transform                          | A transform expression that will be applied to the message before it is send to a processor. Can be overriden in a subscription. [{{< badge style="info" title="supports" value="transformations" >}}](/docs/expressions/#transformations) |

Here, we define a **Subscription Group** called `Power sensors`, which includes two subscriptions: 
one for a washing machine and another for a dryer. Both subscriptions have associated variables that can be used later in the 
metrics or logs.

### Defining Subscription Groups with ParentPath and SubPath

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

Example:

```yaml {hl_lines=[15,16,31,32]}
SubscriptionGroups:
  - Name: "Power sensors"
    Subscriptions:
      - Name: "Power Sensor 1"
        Topic: "1234"
      - Name: "Power Sensor 2"
        Topic: "9876"

Processors:
  - Name: "Power Metrics"
    Description: "Provides power information from a power sensor."
    Mqtt: 
      SubscriptionGroups:
        - Name: "Power sensors"
          ParentPath: "tele"
          SubPath: "sensor"
    Otel:
      Metrics:
        - Name: "Energy_Power_W"
          Description: "The current power consumption at the time of measurement in Watt."
          SignalDataType: Float
          Instrument: Gauge
          Value: "JSONPATH('$.ENERGY.Power')"
        - ...

  - Name: "Sensor Logs"
    Description: "Collect all log messages from the sensors."
    Mqtt:
      SubscriptionGroups:
        - Name: "Power sensors"
          ParentPath: "stat"
          SubPath: "logs"
    Otel:
      Logs:
        - Name: "Logging"
          PayloadType: Json
          Transform: "GROK('%{TIME:otel_timestamp} %{WORD:category}: %{GREEDYDATA:otel_message}')"
```

#### Explanation:

* **ParentPath**: This specifies the top-level directory or prefix of the topic. For example, `tele` for telemetry data or `stat` for status/log data.
* **SubPath**: This specifies the specific subtopic or suffix that targets a specific part of the topic.

#### Result:

* The **Power Metrics** rule will subscribe to topics like `tele_1234_sensor` and `tele_9876_sensor` using the `ParentPath` `tele` and `SubPath` `sensor`.
* The **Sensor Logs** rule will subscribe to topics like `stat_1234_logs` and `stat_9876_logs` using the `ParentPath` `stat` and `SubPath` `logs`.
