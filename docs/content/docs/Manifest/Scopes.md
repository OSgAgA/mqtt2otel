---
title: "Instrumentation scopes"
weight: 25
bookCollapseSection: false
---
# Instrumentation scopes

Instrumentation scopes are a logical unit of software with which the emitted telemetry can be associated. It is typically the developer’s 
choice to decide what denotes a reasonable instrumentation scope. 

## Declaration

An instrument scope can be defined on the manifest top level and has the following parameters:


| Parameter                          | Description                                                                                                                   |
|------------------------------------|-------------------------------------------------------------------------------------------------------------------------------|
| Name                               | The mandatory name of the scope. Must be unique inside an OtelConnection.                                                     |
| Version                            | An optional version.                                                                                                          |
| Attributes                         | Optional attributes.                                                                                                          |
| OtelConnection                     | The otel connection, to which this scope belongs. If none is set, the default connection is used.                             |

> [!Note]
> ### Information
>
> If no scope is declared a default scope is declared and applied automatically to all metrics, that do not explicitly state otherwise.

## Usage

The instrument scope can be referred to inside meters using the `OtelScope` parameter. The scope is identified via its name and the connection that belongs
to the corresponding metric or log entry.

## Example

To add a scope with the name "My new scope" to a metric, you can use the following code:

{{< exampleCode id="doc-21" field="Manifest" lang="yaml">}}

> [!Note]
> ### Usage of default scope
> 
> Please notice, that in this example the scope inside the metric is explicitly set. This is not necessary, as only one scope exists
> and so this scope is automatically the default and would have been applied. If you have multiple scopes, the scope which is declared
> first is the default and all other scopes must be explicitly referred to via the `OtelScope` parameter.