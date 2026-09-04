---
title: "Converter and Formatter"
weight: 60
bookCollapseSection: false
---

# Converter and Formatters

After a metric is prepared, it is possible to convert the created value (e.g. to another unit) or to format a name in a different way.

To do this the Otel.Metric section inside a processor has two properties:

* ValueConverter: Used to convert a metric value.
* NameFormatter: Formats a metric name.

These properties use [expressions](/docs/expressions) to fullfill there needs. Additional to the usual functions a variable containing the 
current value (either \[Name\] or \[Value\] is provided.

Often these functions are used in the context of `ParseAs` methods, as these are able to parse e.g. a full json and automatically create 
metrics out of them, but you may want to interfere with the naming of your signals, or convert values to other units.

## Converter

A converter is meant to take a metric value and convert it to another value (typically another unit). The converter is executed, after the
expression of the Value property is executed.

A typical example would look like this:

```
ValueConverter: "[Value] * 100"
```

This will take the provided value in cm and convert it to meters.

## NameFormatter

The name formatter is meant to format a signal name. The formatter is executed after the expression for the Name property is executed.

A typical example would look like this:

```
NameFormatter: "ToLower([Name])"
```

This will convert the name to lower case.

Important functions for formatting names are:

| Function       | Example                   | Description                                                                                                                                          |
| ----------     | ------------------------- | ----------------------------------------                                                                                                             |
| `ToLower`      | `ToLower('My Signal')` => my signal        | Returns lower case value                                                                                                                             |
| `ToUpper`      | `ToUpper('My Signal')` => MY SIGNAL        | Returns upper case value                                                                                                                             |
| `ToPascalCase` | `ToPascalCase('My Signal')` => MySignal    | Returns pascal case value                                                                                                                             |
| `ToCamelCase`  | `ToCamelCase('My Signal')` => mySignal     | Returns camel case value                                                                                                                             |
| `ToSnakeCase`  | `ToSnakeCase('My Signal')` => my_signal    | Returns snake case value                                                                                                                             |
| `ToKebabCase`  | `ToKebabCase('My Signal')` => my-signal    | Returns kebab or hyphen case value                                                                                                                             |
| `ToTrainCase`  | `ToTrainCase('My Signal')` => My-Signal    | Returns train case value           