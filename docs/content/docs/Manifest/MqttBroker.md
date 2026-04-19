---
title: "Mqtt broker"
weight: 10
bookCollapseSection: false
---
# The Mqtt broker

## Configuration

Within the `MqttBroker` section of the manifest file you can configure how to connect to mqtt brokers. At the moment
mqtt2otel does not provide a broker itself so an external one has to be used.

A simple example for a mqtt broker connection would look like that:

```yaml
MqttBroker:
  - Name: "My broker"
    Endpoint:
      Port: 1813
      Protocol: tcp
      Address: "mymqtt-broker.net"
      EnableTls: false
```

It has the following parameters:

| Parameter                       | Description                                                                                               |
|---------------------------------|-----------------------------------------------------------------------------------------------------------|
| Name                            | An optional name that can be given to the broker. With this the broker can be refered to later.           |
| Description                     | An optional description of the broker                                                                     |
| Eendpoint.Protocol              | The optional protocol that will be used for connecting to the broker, e.g. tcp.                           |
| Endpoint.Address                | The address of the mqtt broker                                                                            |
| Endpoint.Port                   | The optional port under which the broker can be reached. Default is 1813.                                 |
| Endpoint.ConnectionType         | One of the following values: Tcp, WebSockets. Default is tcp.                                             |
| Endpoint.MqttProtocollVersion   | Optional. You can set an explicit mqtt protocol version in case of compatibility issues.                  |
| Endpoint.EnableTls              | Set to false to disable transport level security (TLS). Default is true.                                  |
| Endpoint.TlsSslProtocol         | Optional. Choose the ssl protocol: Tls, Tls11, Tls12, Tls13, Ssl2, Ssl3, Default                          |
| Endpoint.TlsCaFilePath          | Optional. Set a file path for a certificate authority (CA) file.                                          |
| Endpoint.UsePacketFragmentation | Set to false to disable packet fragmentation (may be needed to connect to AWS broker.                     |
| Endpoint.Username               | The credentials username for basic authentication.                                                        |
| Endpoint.Password               | The credentials password for basic authentication.                                                        |
| ReconnectDelayInMs              | Sets the delay intervall on reconnect in milliseconds. Default: 5000                                      |
| ClientPrefix                    | A prefix that will be added to the client id when connecting to the broker. Helps to identify the client. |

## Using multiple connections

The `MqttBroker` section contains a list of brokers. That means you can add multiple brokers. The first broker in this list is always the
default broker, that will be used if nothing else is specified. If you want to address another broker, than the default you have to use the
provided name of the broker.