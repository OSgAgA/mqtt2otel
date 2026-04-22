---
title: "Organize manifest file"
weight: 60
bookCollapseSection: false
---
# Organize the manifest file

Manifest files tend to get huge over time. To organize these files, many parameters have the possibility to be read from an external file. To 
do this, you just have to identify the filename via the `ImportFrom` parameter. The file can contain multiple objects. The objects need to be 
organized as a yaml list. Parameters that support this can be identified via the tag {{< badge style="info" title="supports" value="ImportFrom" >}}.

## Example

```yaml {hl_lines=[9, 12]}
Version: 1.0

MqttBroker:
  - Name: "My broker"
    Endpoint: 
      Port: 32014
      Address: "192.168.1.92"
      EnableTls: false
  - ImportFrom: "Another_Broker.yaml"

OtelServer:
  - ImportFrom: "OtelServers.yaml"
```

As can be seen here other mqtt brokers will be imported via the file `Another_Broker.yaml` and other otel servers will be imported from the 
file `OtelServers.yaml`.

`Another_Broker.yaml` may then look like this:
```yaml
- Name: "Another broker"
  Endpoint: 
    Port: 32028
    Address: "192.168.1.93"
    EnableTls: false
- Name: "Another another broker"
  Endpoint: 
    Port: 32029
    Address: "192.168.1.94"
    EnableTls: false
```

{{% hint info %}}
Please be aware, that the import files are lists. Even if you want to only import one object, you have to provide it as a list and not a single
object. So the `-` are important!
{{% /hint %}}

## Additional functions

It is possible to use the usual wildcards like this:

```yaml
  - ImportFrom: "Servers/OtelServers_*.yaml"
```