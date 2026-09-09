<div style="text-align:center;">

  ![logo](docs/static/logo.png)

</div>

# mqtt2otel

`mqtt2otel` is a powerful yet lightweight bridge between the MQTT messaging protocol—commonly used in the IoT 
(Internet of Things) context—and OpenTelemetry (Otel) protocol, which is typically used for professional application 
and infrastructure monitoring. The tool can subscribe to MQTT broker topics, process and enrich messages with 
additional information, and then generate Otel metrics or logs for further analysis using standard tools.

# Overview

```mermaid
---
config:
  flowchart:
    curve: stepBefore
---

flowchart LR
  mqttBroker[mqtt broker]
  subgraph mqtt2otel
    mqtt2otelmetric[metric<br/>attributes:<br/> device = 'sensor A'<br/>temp = 42°C]
    mqtt2otellog[log<br/>attributes:<br/> device = 'sensor A'<br/>timestamp = 10:23:15<br/>loglevel = Info<br/> message = 'operation completed']
  end
  otelCollector[otelCollector]
  metricsDashboard[metrics dashboard]
  logDashboard[log dashboard]
  
  mqttBroker -->|temp: 107.6°F| mqtt2otelmetric
  mqttBroker -->|10:23:15 Info operation completed| mqtt2otellog
  mqtt2otel -->|logs| logDashboard
  mqtt2otel -->|metrics| metricsDashboard
  mqtt2otel -->|metrics and logs| otelCollector
```

# Homepage

You can find the official homepage of the project [here](https://mqtt2otel.org).

# Installation

Installation instructions can be found in the [documentation](https://mqtt2otel.org/docs/installation/).

# Quickstart

If you want to get started fast, have a look at our [quickstart guide](https://mqtt2otel.org/docs/introduction/quickstart/).

# Documentation

More detailed information is available in the official [documentation](https://mqtt2otel.org/docs/introduction/).

# Background

To learn more about the underlying technologies, check out the following resources:

* [Official OpenTelemetry page](https://opentelemetry.io/)
* [Official MQTT page](https://mqtt.org/)
