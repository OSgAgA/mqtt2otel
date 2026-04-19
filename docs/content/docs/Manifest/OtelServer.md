---
title: "Open telemetry server"
weight: 20
bookCollapseSection: false
---
# The Open Telemetry server

## Configuration

Within the `OtelServer` section of the manifest file you can configure how to connect to an open telemetry endpoint. At the moment
mqtt2otel does not provide an otel collector so an external one has to be used.

A simple example for an otel connection would look like that:

```yaml
OtelServer:
  - Name: "My Otel server"
    ServiceName: "my-service"
    ServiceNamespace: "my-service-namespace"
    Endpoint:
      Protocol: "https"
      Port: 4317
      Address: "my-otel-collector.net"
```

It has the following parameters:

| Parameter                          | Description                                                                                               |
|------------------------------------|-----------------------------------------------------------------------------------------------------------|
| Name                               | An optional name that can be given to the otel server. With this the server can be refered to later.      |
| Description                        | An optional description of the server                                                                     |
| ServiceName                        | The otel service name                                                                                     |
| ServiceVersion                     | The otel service version                                                                                  |
| ServiceNamespace                   | The otel service namespace                                                                                |
| Eendpoint.Protocol                 | The optional protocol that will be used for connecting to the broker, e.g. https                          |
| Endpoint.Address                   | The address of the otel server                                                                            |
| Endpoint.Port                      | The optional port under which the broker can be reached. Default is 4317.                                 |
| Endpoint.Headers                   | Optional: The http headers, that will be send to the server on each request.                              |
| Endpoint.BatchTimeoutInMs          | Optional. The maximum waiting time for the server to process a batch.                                     |
| Endpoint.EnableTls                 | Set to false to disable transport level security (TLS). Default is true.                                  |
| Endpoint.ClientCertificatePath     | Optional. Set a file path for a client certificate file.                                                  |
| Endpoint.ClientCertificatePassword | Optional. The password to access the provided client certificate.                                         |
| OtlpExportProtocol                 | Optional. The export protocol that should be used: Grpc or HttpProtobuf. Default is HttpProtobuf.         |
| ExportProcessorType                | Optional. The export processor type that should be used: Batch or Simple. Default is Batch.               |
| ClientPrefix                       | A prefix that will be added to the client id when connecting to the server. Helps to identify the client. |

## The export processor type

{{% hint info %}}
**ExportProcessorType**  

It's important to understand the differences in the processor types to ensure the system behaves as you would expect:

* The batch type will collect messages in a batch and then send them together to the otel endpoint. This reduces network traffic, but increases
  latency. This is typically a good choice if you don`t need realtime data.
* The simple type will send the messages directly to the server. This may produce a lot of network traffic, but has a low latency. This may be 
  your best choice if you need real time data.
{{% /hint %}}

## Using multiple connections

The `OtelServer` section contains a list of servers. That means you can add multiple servers. The **first server** in this list is always the
**default server**, that will be used if nothing else is specified. If you want to address another broker, than the default you have to use the
provided name of the server.