---
title: "Metric Transformations"
weight: 70
bookCollapseSection: false
---

# Metric transformations

Metric processors support the concept of transformations, that can interfere with how (and if) a signal is created.. 
A transformation consists of two parts:

1. The condition (`When`) - Checks whether a generated metric signal matches certain conditions
1. The action (`Then`) - Executes an action when the condition is met.

Transformations will be executed after the signal is generated, but before a value converter or a name formatter is applied..

## The condition

A condition consists of the following parameters, that must be all true to match the condition. An parameter that is not
set will allways be true.

| Parameter                          | Description                                                                                |
|------------------------------------|--------------------------------------------------------------------------------------------|
| Name                               | The name of the created signal. Can use `*` as a wildcard parameter       				  |
| SignalDataType                     | The signal data type, must be an exact match.											  |
| IgnoreCase                         | If set to true, then the `Name` parameter will ignore the case when matching the condition.|

## The action

The transformation action, will set properties on the signal, when the condition returned true. 
It consists of the following properties, properties that are not set, will keep their original state:

| Parameter      | Description                                                                |
|----------------|----------------------------------------------------------------------------|
| Name           | The name of the created signal.                                            |
| Unit           | The unit.											                      |
| Description    | The description.                                                           |
| NameFormatter  | The name formatter.                                                        |
| ValueConverter | The value converter.                                                       |
| SignalDataType | The signal data type.                                                      |
| Ignore         | If set to true, then the signal will be skipped and not further processed. |

## Example

