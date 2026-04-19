---
title: "Application settings"
weight: 20
bookCollapseSection: false
---

# Application settings

The behavior of the application can be controlled via a file called `ApplicationSettings.yaml` which must be located
in the `/config` directory. If no file is found the default values are used.

A settings file has the following structure:

```yaml
PollIntervallInSeconds: 5
ManifestPath: "/data/Manifest.yaml"
Logging:
  LogToConsole: true
  LogToOtel: false
  MinimumLogLevel: Information  
  LogToFile: false
  LogFilePath: "logs"
  LogFileKeepMax: 5
  Otel:
    ServiceName: "mqtt2otel-internal"
    ServiceNamespace: "prod-mqtt2otel"
    Endpoint:
      Protocol: "http"
      Port: 32042
      Address: "192.168.1.8"
```

It consists of the following settings:

| setting                 | default value       | description                                                                                                            |
|-------------------------|---------------------|------------------------------------------------------------------------------------------------------------------------|
| PollIntervallInSeconds  | 5                   | The interval in seconds for polling the manifest file for changes.                                                     |
| ManifestPath            | /data/Manifest.yaml | The path for the manifest file.                                                                                        |
| Logging.LogToConsole    | true                | Enables logging internal log messages to the console                                                                   |
| Logging.LogToOtel       | false               | Enables logging internal log messages to en otel endpoint                                                              |
| Logging.LogToFile       | false               | Enables logging internal log messages to files                                                                         |
| Logging.LogFilePath     | logs                | Sets the root directory for created log files                                                                          |
| Logging.LogFileKeepMax  | 5                   | The maximum amount of log files that should be kept before deleting.                                                   |
| Logging.MinimumLogLevel | Information         | The minimum log level that will be logged. Must be one of the following: Debug, Information, Warning, Error, Critical. |
| Logging.Otel            | empty               | The connection data for the otel endpoint. [see otel server connection](../Manifest/otelserver)                       |

                                                  