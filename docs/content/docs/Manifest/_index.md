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

2. ## General settings
   General setting that will be applied to the full manifest.

3. ## MqttConnections
   In this section the available Mqtt broker connections will be configured. For details, see [Mqtt broker](mqttbroker). [{{< badge style="info" title="supports" value="ImportFrom" >}}](organize)

4. ## OtelConnections
   In this section the available open telemetry connections will be configured. For details, see [Otel connections](otelserver). [{{< badge style="info" title="supports" value="ImportFrom" >}}](organize)

5. ## SubscriptionGroups
   A list of grouped subscriptions that can be referred later in the otel section. For details see [Subscription groups](subscription/#subscription-groups). [{{< badge style="info" title="supports" value="ImportFrom" >}}](organize)

6. ## Processors
   A list of processors, that will take mqtt payloads, processes them and then create otel logs or metrics. For details see [Processors](processors). [{{< badge style="info" title="supports" value="ImportFrom" >}}](organize)

7. ## How to organize large manifests.
   Find out how you can organize complex scenarios in your manifest file. See [Organize manifest files](organize).

{{% /steps %}}

As a starting point, this is an example manifest using logs, and metrics:

{{< exampleCode id="doc-9" field="Manifest" lang="yaml">}}

## General settings

The Manifest supports the following general settings:

| Parameter                          | Description                                                                                                  |
|------------------------------------|--------------------------------------------------------------------------------------------------------------|
| CreateAttributesFromUserProperties | A value indicating, whether attributes should be created for all mqtt user attributes. Leave null to use parent settings. |
