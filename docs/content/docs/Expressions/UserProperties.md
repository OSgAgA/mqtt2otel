---
title: "Mqtt user properties"
weight: 55
bookCollapseSection: false
---

# Parsing of MQTT user properties

MQTT user properties are translated to open telemetry attributes per default. If you want to change this behaviour you can turn it off by setting the 
`CreateAttributesFromUserProperties` property to false.

MQTT user properties can be assessed inside expressions by using the `UserProperty` function. This function returns the value of the user property and expects the 
name of the property as an argument. If the name does not exist, the function returns an empty string.

> [!Warning] 
> **Warning**
>
> If the name exists multiple times, only the first match is returned. 

{{< exampleCode id="misc-06" field="Manifest" lang="yaml">}}