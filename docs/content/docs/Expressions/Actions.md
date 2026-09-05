---
title: "Metric Actions"
weight: 70
bookCollapseSection: false
---

# Metric actions

Metric processors support the concept of actions, that can interfere with how (and if) a signal is created.
A transformation consists of two parts:

1. The condition (`When`) - Checks whether a generated metric signal matches certain conditions
1. The action (`Then`) - Executes an action when the condition is met.

Transformations will be executed after the signal is generated, but before a value converter or a name formatter is applied..

## The condition

A condition consists of an expression that must evaluate to true to pass the test. The expression gets the following variables set:

| Variable                           | Description                                                                                |
|------------------------------------|--------------------------------------------------------------------------------------------|
| Name                               | The signal name, that has been evaluated in the previous step               				  |
| Value                              | The signal value, that has been evaluated in the previous step     						  |
| Type                               | The signal type, that has been evaluated in the previous step                             .|

## The action

The transformation action, will set properties on the signal, when the condition returned true. 
It consists of the following properties, properties that are not set, will keep their original state:

| Parameter         | Description                                                                          |
|-------------------|--------------------------------------------------------------------------------------|
| Name              | The name of the created signal.                                                      |
| Unit              | The unit.											                                |
| Description       | The description.                                                                     |
| NameFormatter     | The name formatter.                                                                  |
| ValueConverter    | The value converter.                                                                 |
| SignalDataType    | The signal data type.                                                                |
| Instrument        | The otel instrument.                                                                 |
| Ignore            | If set to true, then the signal will be skipped and not further processed.           |
| Output            | An output message that will be written to the standard log.                          |
| Output.Message    | The message that should be written                                                   |
| Output.Level      | The log level of the message: Debug, Trace, Information, Warninbg, Error, Critical   |
| Output.Attributes | A dictionary of additional attributes that will be added to the log message.         |

## Example

Let's have a look at the following example. We get a message that contains different information from different kind of sensors and 
additionaly a unit for the temperature measurement:

{{< exampleCode id="metric-12" field="Payload" lang="yaml">}}

We want this to be automatically parsed using a `ParseAs` command, but we want all temperatures to be in °C. So we set the unit accordingly.
In case the temperature unit is reported as °F we will convert the value to °C using a `ValueConverter`:

{{< exampleCode id="metric-12" field="Manifest" lang="yaml">}}