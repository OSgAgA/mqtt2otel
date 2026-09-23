---
title: "Mappings"
weight: 27
bookCollapseSection: false
---
# Mappings

The Mappings section defines reusable lookup tables that translate raw input values (typically coming from MQTT payloads or topics) into human‑readable or semantically meaningful values.
Mappings are primarily used inside expressions through the Map(from, name) function.

Mappings allow you to:

 * convert numeric IDs into descriptive labels
 * normalize inconsistent device identifiers
 * enrich OpenTelemetry attributes with meaningful values
 * avoid hard‑coding lookup logic inside processors
	
## Structure
A Mappings block contains one or more named mapping tables, consisting of the following fields:

| Parameter                          | Description                                                                                                                   |
|------------------------------------|-------------------------------------------------------------------------------------------------------------------------------|
| Name                               | The name of the mapping table. Used when calling Map().                                                                       |
| Entries                            | A list of key/value pairs. Each entry defines one mapping rule.                                                               |
| Entries[].From                     | The raw value to match (e.g., device ID, payload field).                                                                      |
| Entries[].To                       | The translated value returned by Map().                                                                                       |

## How Mapping Works
Mappings are accessed through the `Map(from, mappingName)` function inside expressions.

* The function searches the mapping table with the given mappingName. If mappingName is not found, the original value is returned.

* It compares the from value with each Entries[].From. If no matching From value is found, the original value is returned.

If a match is found, the corresponding Entries[].To value is returned.

## Example

{{< exampleCode id="misc-08" field="Manifest" lang="yaml">}}
