---
title: "Variables"
weight: 30
bookCollapseSection: false
---

# Variables

Many sections in the manifest support variables. A variable is a simple structure consisting of a key and a value. 

Example

```yaml
Variables:
    - Key: DeviceName
      Value: "MyDevice"
```

The value is usually a string, but may be any other data type. 

To use a variable you will address it with `$key. So the variable as defined above can be used as:

```yaml
Processors:
  - Name: "My processor"
    ...    
    Otel:
      Attributes:
        - Key: DeviceName
          Value: $DeviceName
      Metrics:
        - Name: "My $DeviceName metric"
         ...
```

Variables can then be referred to in parts of the processors section. The parameters that can use variables are tagged accordingly in the documentation.

Currently the following parameters are supported:

* Processors.Otel.Attributes.Value
* Processors.Otel.Metrics.Name
* Processors.Otel.Metrics.Attributes.Value
* Processors.Otel.Logs.Name
* Processors.Otel.Logs.Attributes.Value

