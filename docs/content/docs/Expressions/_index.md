---
title: "Expressions and transformations"
weight: 40
bookCollapseSection: false
---

# Expressions and transformations

Expressions and transformations are the tools with which a message payload, received from mqtt can be processed. The difference between the two is:

* Expressions are used to extract or calculate a value from a payload
* Transformations are used to transform a payload into a different format, e.g. from text to json.

Parameters supporting expressions or transformation can be identified by the tag {{< badge style="info" title="supports" value="expressions" >}} or 
{{< badge style="info" title="supports" value="transformation" >}}.

## Expressions

### The basics

Expressions are mainly used at the Value parameter inside a `Processors.Otel.Metrics.Metric.Value` are build on top of the 
[NCalc library](https://github.com/ncalc/ncalc) where you can find additional information on capabilities not covered in this document. 

A simple expression returning the constant value 42 would be:

```yaml
Value: "42"
```

### Functions
Most of the time you want to process a payload as delivered by a mqtt subscription. Lets take the following example json payload:

```json {hl_lines=[4]}
{
    "Processor": 
    {
        "Temperature": 42.5
    },
    "TempUnit": "C"
}
```

To access the temperature we use the [JSONPATH](https://www.rfc-editor.org/rfc/rfc9535) syntax: `$.Processor.Temperature` that gets the `Temperature`
parameter inside the `Processor` parameter. To do this we have to use a function called `JSONPATH`:

```yaml
Value: "JSONPATH('$.Processor.Temperature')"
```

That will return the value 42.5. The data type returned will be the data type defined in `Processors.Otel.Metrics.Metric.SignalDataType`. If you want to
change the data type to e.g. `int` you can add another parameter to the function stating the data type:

```yaml
Value: "JSONPATH('int', '$.Processor.Temperature')"
```

This will return the value 42.

### Available Functions

| Function   | Example                   | Description                                                                                                                                          |
| ---------- | ------------------------- | ----------------------------------------                                                                                                             |
| `JSONPATH` | `JSONPATH('$.Root')`      | Extracts data using [JSONPATH](https://www.rfc-editor.org/rfc/rfc9535) syntax                                                                        |
| `XPATH`    | `XPATH('/root/child[1]')` | Extracts data using [XPath](https://www.w3.org/TR/xpath-31/) syntax                                                                                  |
| `REGEX`    | `REGEX('[0-9]+')`         | Extracts data using a [regular expression](https://learn.microsoft.com/en-us/dotnet/standard/base-types/regular-expression-language-quick-reference). If the regular expression returns more than one match, then the first match is used. |
| `PAYLOAD`  | `PAYLOAD()`               | Returns the raw payload                                                                                                                              |
| `CONST`    | `CONST('42')`             | Returns a constant value                                                                                                                             |

### Calculations

We’ve already used an expression to parse the payload with `JSONPATH('$.Processor.Temperature')`. However, you can also perform 
mathematical calculations. For example, to convert the temperature from Celsius to Fahrenheit, you can use this expression:

```yaml
Value: "(JSONPATH('$.Processor.Temperature') * 1.8) + 32.0"
```

Standard mathematical operations like `+`, `-`, `*`, `/`, and functions such as `SQRT`, `Sin`, `Cos`, `Tan`, and constants 
like `[Pi]` are also supported. Further details can be found at the [NCalc library](https://github.com/ncalc/ncalc).


## Transformations

### The basics

Transformations work similar than expressions, but instead of extracting the needed value they transform a payload in another form for further processing. 

Let's say you receive a log message payload in the following format from MQTT:

```
2026-02-26T10:28:34Z [Info] [ServerA] Temperature value read successfully.
```

Rather than sending the raw message to Otel, we can transform it into a structured log format using a 
[GROK](https://www.elastic.co/docs/reference/logstash/plugins/plugins-filters-grok) expression. 

The grok expression for parsing the payload is:

```grok
%{TIMESTAMP_ISO8601:otel_timestamp} \[%{WORD:otel_loglevel}\] \[%{WORD:server_name}\] %{GREEDYDATA:otel_message}')
```

This can be read as:

* Parse an ISO8061 timestamp and name it otel_timestamp
* Read a space and a [ (needs to be escaped as \[) adn discard the information
* Read a word and name it otel_loglevel
* Read ] [ and discard the information
* Read a word and name it server_name
* Read ] [ and discard the information
* Read the remaining part of the message and name it otel_message

With that the payload will be transformed in a log message that looks like this:

```json
{
  "otel_timestamp": "2026-02-26T10:28:34Z",
  "otel_loglevel": "Info",
  "server_name": "ServerA",
  "otel_message": "Temperature value read successfully."
}
```

This message can than be passed to the log processor. Be careful to set `PayloadType: Json` for getting the expected results.
The usage is similar to expressions:

```yaml {hl_lines=[3,4]}
      Logs:
        - Name: "Logging"
          PayloadType: Json
          Transform: "GROK('%{TIMESTAMP_ISO8601:otel_timestamp} \[%{WORD:otel_loglevel}\] \[%{WORD:server_name}\] %{GREEDYDATA:otel_message}')"
```

### Available Functions

| Function   | Example                              | Description                                                                                                                   |
| ---------- | ------------------------------------ | ------------------------------------------------------------------------------------------------------------------------------|
| `GROK`     | `GROK('%{GREEDYDATA:otel_message}')` | Converts a payload to json using [GROK](https://www.elastic.co/docs/reference/logstash/plugins/plugins-filters-grok) syntax   |
