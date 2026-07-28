---
title: "Topic parsing"
weight: 50
bookCollapseSection: false
---

# Topic parsing

When working with topics, you can choose between two different parsers:

* The **TopicAttribute** is provided by metrics and the otel processor and enables the translation of a topic to attributes.
* The **TopicPath** syntax can be used inside any parameter that supports expressions by using the 'TopicPath' function.

The TopicPath syntax is meant for complex scenarios, where you need detailed control over what is happening, or when mixing different sources, 
like payload and topic information. For simpler use cases please use the TopicAttribute.

# The TopicAttribute syntax

Metrics and otel processors support the 'TopicAttribute' parameter, that is able to map topics to open telemetry attributes. 

The syntax consists of a path separator character "/", a wildcard character "%" and the any character "_". 
All characters between the a separator that is not the wildcard or the any character are identified as attribute keys for 
the part of the path matching the segment.

An attribute key cannot be empty or whitespace only. If this rule is violated, the pattern will not be further processed.

The wildcard matches any (including 0) further topic segments and must be the last segment of a pattern. If the wildcard is used inside a 
pattern only the attributes, that matched until this segment will be returned.

If no result is found, the pattern is ignored. If the same attribute key is used multiple time it overwrites the previous one.

This usually becomes clearer when looking at some examples:

| Topic                                 | Pattern                | Generated attributes          | Description
|---------------------------------------|------------------------|-------------------------------|---------------------------
| "logs/sensor/1234/temperature"        | "\_/\_/Device/\_"      |  Key: "Device", Value: "1234" | Ignore the first two segments and then read the next as Device
| "logs/sensor/1234/temperature/valueA" | "\_/\_/Device/%"       |  Key: "Device", Value: "1234" | Ignore the first segment and then read the next as Device and then ignore the rest of the topic.
| "logs/sensor/1234"                    | "\_/\_/Device/%"       |  Key: "Device", Value: "1234" | Ignore the first segment and then read the next as Device and then ignore the rest of the topic.
| "logs/sensor/1234/temperature"        | "\_/Device/Device/\_"  |  Key: "Device", Value: "1234" | Ignore the first segment and then read the next as Device. Then read the next as Device overriding the first value.
| "logs/sensor/1234"                    | "\_/\_/Device/\_"      |  <NONE> 						 | Ignore the first two segments, read next as Device, then ignore next segment, which does not exist, so an empty string is returned.
| "logs/sensor/1234/temperature"        | "\_/\  /Device/\_"     |  <NONE> 						 | Ignore the first segment, then read the next segment as an attribute that only contains whitespace -> this is not allowed, so an empty string is returned.

# The TopicPath syntax

The topic path pattern consists of two possible token:

  * A named segment token: MUST NOT start with a '[' AND MUST NOT end with a ']'
  * A skip token: MUST start with a '[' AND MUST end with a ']'. The value between the brackets MUST be a positive integer.
  
For matching a pattern a topic is split into its segments, so the topic parent/child/subchild is split into the segments: "parent", "child" and 
"subchild".

A named token matches the first segment after the first occurance of the provided segment name and the skip token
skips the provided amount of segments.

An empty string is returned, if the pattern does not match the topic.

This usually becomes clearer when looking at some examples:

| Topic            | Pattern         | Result    | Description
|------------------|-----------------|-----------|---------------------------------------------------
| "this/is/a/test" | "[0]"           | "this"	 | Skips the first 0 (so none) segments => Reads first segment.
| "this/is/a/test" | "[1]"           | "is"		 | Skips first segment and reads second one.
| "this/is/a/test" | "[4]"           | ""		 | Skips first four segments. As there are not enough segments to skip an empty string is returned.
| "this/is/a/test" | "a"             | "test"	 | Skips the first segment that has the key "a" and reads the following segment.
| "this/is/a/test" | "a/"            | "test"	 | Skips the first segment that has the key "a" and reads the following segment.
| "this/is/a/test" | "nonExisting/"  | ""		 | Skips the first segment that has the key "nonExisting", which is not found, so an empty string is returned.
| "this/is/a/test" | "is/[0]"        | "a"		 | Skips the first segment that has the key "is" and reads the following segment.
| "this/is/a/test" | "is/[1]"        | "test"	 | Skips the first segment that has the key "is", skips one segment and reads the following segment.
| "this/is/is/test"| "is"            | "is"		 | Skips the first segment that has the key "is" and reads the following segment.
| "this/is/is/test"| "[2]/is"        | "test"	 | Skips the first two segments and then skips the first segment that has the key "is" and reads the following segment.
