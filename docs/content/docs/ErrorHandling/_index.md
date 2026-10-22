---
title: "Error handling"
weight: 45
bookCollapseSection: false
---

# Error handling

## Introduction

mqtt2otel provides a multistep error handling mechanism that handles the following issues:

### Preventing Log Flooding from High‑Frequency Topics

A single MQTT topic can easily emit dozens or hundreds of messages per second.
If every failed message produced a log entry:

* The logger becomes a hot path, consuming CPU and I/O bandwidth.
* Log storage grows uncontrollably, causing disk pressure and retention issues.
* Downstream log processors experience backpressure.
* The actual error signal becomes diluted by noise, making diagnostics harder.


### Protecting System Performance from Continuously Failing Rules

A rule that fails continuously is not just noisy — it is expensive:

* Each failure still requires full rule evaluation.
* Exceptions trigger stack unwinding, allocations, and possibly expensive logging.
* If multiple rules fail at high frequency, the system spends more time handling errors than processing valid messages.

By deactivating rules or items when their error rate exceeds a threshold:

* The system avoids repeatedly executing known‑bad logic.
* Resources are preserved for healthy rules and items.
* The system stabilizes under load instead of degrading.

This is a self‑protection mechanism: the system isolates failing components to maintain global throughput.

### Topic-Level Isolation Improves Fault Tolerance

Errors are often topic‑specific:

* A single device may send malformed JSON.
* A single sensor may produce out‑of‑range values.
* A single publisher may violate schema.

Blocking only the affected topic:

* Prevents one bad publisher from impacting all others.
* Keeps the rule/item operational for healthy topics.
* Allows operators to identify problematic sources quickly.

Only when multiple topics fail (exceeding BlockedTopicLimit) does the system conclude:

* “This is not a topic-specific issue; the rule/item itself is broken.”

## Error Propagation Model

Errors can occur at two distinct processing layers:

* **Rule level** – a logging or metric rule 

* **Item level** – an item represents a specific sub‑operation inside a rule (e.g., extracting a JSON or XML property).

Each layer maintains its own error‑tracking and deactivation logic.

## Item-Level Error Handling

When an item fails for a given topic:

* The item will be blocked for this **topic only**.
* Other items inside the same rule continue to operate normally for this topic.
* Other topics will still process this item.

### Topic Blocking Threshold

Each item tracks how many distinct topics have been blocked due to errors. When the number of blocked topics exceeds `BlockedTopicLimit` (default: 3), then 
the entire item is deactivated for all topics.

## Rule-Level Error Handling

The same mechanism applied at item level also applies at rule level:

* If a rule fails for a topic, that topic may be blocked from executing the rule.
* If the number of blocked topics exceeds `BlockedTopicLimit`, the entire rule is deactivated.

Rule-level deactivation is independent of item-level deactivation and is only triggered if the error is on the rule itself and not on a specific item of the rule.

## Handling of sensitive data inside logs

The `LogMessageDetails` switch can be set to false to prevent accidental exposure of sensitive information contained in MQTT payloads. 
MQTT messages often include device identifiers, personal data, configuration secrets, or operational metadata. When an item or rule 
fails, logging the full message would replicate this sensitive content into the system’s log pipeline, where it may be stored, 
forwarded, indexed, or viewed by operators who should not have access to the raw payload.

By allowing message‑detail logging to be disabled, the system:

* Avoids leaking sensitive or regulated data into logs.
* Ensures compliance with privacy and data‑minimization requirements.
* Reduces the risk of exposing confidential MQTT payloads during error bursts.
* Allows operators to choose between detailed diagnostics and strict data‑protection behavior.

## Configuration Properties

The following properties control error-handling behavior:

| Property                      | Default   | Description                                                                                                                                                          |
|-------------------------------|-----------|----------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| LogMessageDetails             | True      | Indicates whether MQTT message details should be logged when an error occurs. Set to false, if the message contains sensitive information that should not be logged. |
| AllowRuleDeactivation         | True      | Determines whether a rule may be deactivated.                                                                                                                        |
| AllowRuleDeactivationByTopic  | True      | Controls whether individual topics may be deactivated for a rule                                                                                                     |
| AllowRuleDeactivationByItem   | True      | Controls whether individual topics may be deactivated for a specific item inside a rule.                                                                             |
| BlockedTopicLimit             | 3         | Defines the maximum number of topics that may be blocked for an item or rule before the entire item or rule becomes globally deactivated.                            |

## Reset Behavior

Whenever a manifest is reloaded:

* All rules and items are unblocked.
* All topic‑level and global deactivation counters are reset.
* This ensures that error states do not persist across manifest updates.