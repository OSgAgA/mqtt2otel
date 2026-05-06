---
title: "Observability"
weight: 50
bookCollapseSection: false
---

# Observability

To observe the **internals of mqtt2otel** or for troubleshooting, there are different possibilites, that are provided by mqtt2otel.

## Logging and tracing

mqtt2otel provides extensive logging messages, that can be either written to the console, a file, or an open telemetry endpoint. This can
be configured via the [ApplicationSettings](../applicationsettings) file.

A typical log output for a successful application start will look like this:

```
2026-05-06T18:24:52Z INFORMATION  Starting application with version: 0.9.3
2026-05-06T18:24:52Z INFORMATION  Application started. Press Ctrl+C to shut down.
2026-05-06T18:24:52Z INFORMATION  ApplicationSettings.yaml read.
2026-05-06T18:24:52Z INFORMATION  Hosting environment: Production
2026-05-06T18:24:52Z INFORMATION  Content root path: C:\repos\mqtt2otel\mqtt2otel.Server
2026-05-06T18:24:52Z INFORMATION  Reading C:\repos\mqtt2otel\mqtt2otel.Server\Manifest.yaml
2026-05-06T18:24:52Z INFORMATION  Importing Processor from C:\repos\mqtt2otel\mqtt2otel.Server\Manifest-Processors-EnergyMetrics.yaml
2026-05-06T18:24:52Z INFORMATION  Importing Processor from C:\repos\mqtt2otel\mqtt2otel.Server\Manifest-Processors-StatusMetrics.yaml
2026-05-06T18:24:53Z INFORMATION  Validation of Manifest.yaml successful.
2026-05-06T18:24:53Z INFORMATION      Otel endpoint:              http://192.168.11.29:12345
2026-05-06T18:24:53Z INFORMATION      Otel export protocoll:      Grpc
2026-05-06T18:24:53Z INFORMATION      Otel export processor type: Simple
2026-05-06T18:24:53Z INFORMATION      Otel service name:          mqtt2otel
2026-05-06T18:24:53Z INFORMATION      Otel service version:       1.0.0
2026-05-06T18:24:53Z INFORMATION      Otel service namespace:     dev-mqtt2otel
2026-05-06T18:24:53Z INFORMATION  Initializing otel meters...
2026-05-06T18:24:53Z INFORMATION  Otel meters initialization successful.
2026-05-06T18:24:53Z INFORMATION  Initializing otel logging...
2026-05-06T18:24:53Z INFORMATION  Otel logging initialization successful.
2026-05-06T18:24:53Z INFORMATION  Connecting to mqtt broker at tcp://192.168.11.17:32007...
2026-05-06T18:24:53Z INFORMATION      Mqtt broker client id:                  mqtt2otel-dev-0b0a7520-946e-42c9-b72c-ef616b9b76e3
2026-05-06T18:24:53Z INFORMATION      Mqtt broker information:                (null)
2026-05-06T18:24:53Z INFORMATION      Mqtt broker reason:                     (null)
2026-05-06T18:24:53Z INFORMATION      Mqtt broker retain available:           True
2026-05-06T18:24:53Z INFORMATION      Mqtt broker maximum QoS:                ExactlyOnce
2026-05-06T18:24:53Z INFORMATION      Mqtt broker receive max:                20
2026-05-06T18:24:53Z INFORMATION      Mqtt broker assigned client id:         (null)
2026-05-06T18:24:53Z INFORMATION      Mqtt broker authentication data:        (null)
2026-05-06T18:24:53Z INFORMATION      Mqtt broker authentication method:      (null)
2026-05-06T18:24:53Z INFORMATION      Mqtt broker session present:            False
2026-05-06T18:24:53Z INFORMATION      Mqtt broker maximum packet size:        2000000
2026-05-06T18:24:53Z INFORMATION      Mqtt broker result code:                Success
2026-05-06T18:24:53Z INFORMATION      Mqtt broker server keep alive (s):      0
2026-05-06T18:24:53Z INFORMATION      Mqtt broker server reference:           (null)
2026-05-06T18:24:53Z INFORMATION      Mqtt broker session expiry interval:    0
2026-05-06T18:24:53Z INFORMATION      Mqtt broker shared sub available:       True
2026-05-06T18:24:53Z INFORMATION      Mqtt broker subscription ids available: True
2026-05-06T18:24:53Z INFORMATION      Mqtt broker topic alias max:            10
2026-05-06T18:24:53Z INFORMATION      Mqtt broker wildcard subs available:    True
2026-05-06T18:24:53Z INFORMATION      Mqtt broker user properties:            (null)
2026-05-06T18:24:53Z INFORMATION  Successfully connected to mqtt broker.
2026-05-06T18:24:53Z INFORMATION  Subscribing to mqtt events...
2026-05-06T18:24:53Z INFORMATION          Subscribed to topic tele/tasmota_071C74/SENSOR.
2026-05-06T18:24:53Z INFORMATION          Subscribed to topic tele/tasmota_071264/SENSOR.
2026-05-06T18:24:53Z INFORMATION          Subscribed to topic tele/tasmota_071C74/STATE.
2026-05-06T18:24:53Z INFORMATION          Subscribed to topic tele/tasmota_071264/STATE.
2026-05-06T18:24:53Z INFORMATION          Subscribed to topic stat/tasmota_071C74/LOGGING.
2026-05-06T18:24:53Z INFORMATION          Subscribed to topic stat/tasmota_071264/LOGGING.
2026-05-06T18:24:53Z INFORMATION  Successfully subscribed to all events.
2026-05-06T18:24:53Z INFORMATION  Watching for manifest changes at C:\repos\mqtt2otel\mqtt2otel.Server\Manifest.yaml.
2026-05-06T18:24:53Z INFORMATION  Polling intervall for manifest updates is set to 5s.
2026-05-06T18:24:53Z INFORMATION  Application up and running!
```

The application is successfully started, when you see the log entry: `Application up and running`.  Look closely inside the log, if anything is
behaving different than expected. It will give you the connections, that it made to the mqtt broker and the otel servers and it will tell you all
topics it has subscribed to. 

And very important it will give you all paths, where it is looking for data, e.g. the Manifest.yaml and how often it is polled for changes.

Typically if no errors appear logs will not contain any additional information. Have a look at the internal metrics, for more insights of the
running application.

You can increase the log level via the [ApplicationSettings](../applicationsettings) file. But be careful, this should only be used for debugging
purposes as it can produce a lot of logging output.

## Internal metrics

mqtt2otel tracks some metrics of its internal workings, that you can monitor via an open telemetry endpoint. This can be configured via 
the [ApplicationSettings](../applicationsettings) file.

The following metrics will be provided:

### Manifest metrics

<table>
  <tr>
	<th>metric</th>
    <th>type</th>
    <th>unit</th>
	<th>attributes</th>
	<th>description</th>
  </tr>

  <tr>
  	<td>mqtt2otel.manifest.processing_duration_in_us</td>
  	<td>Gauge&lt;double&gt;</td>
  	<td>&micro;s</td>
  	<td></td>
  	<td>The time needed for reading, parsing, validating and initializing the manifest file.</td>
  </tr>

  <tr>
  	<td>mqtt2otel.manifest.procesors.count</td>
  	<td>Gauge&lt;int&gt;</td>
  	<td></td>
  	<td></td>
  	<td>The amount of processors read from manifest file.</td>
  </tr>

  <tr>
  	<td>mqtt2otel.manifest.subscription_groups.count</td>
  	<td>Gauge&lt;int&gt;</td>
  	<td></td>
  	<td></td>
  	<td>The amount of subscription groups read from the manifest file.</td>
  </tr>

  <tr>
  	<td>mqtt2otel.manifest.unsuccessful_read</td>
  	<td>Counter&lt;int&gt;</td>
  	<td></td>
  	<td></td>
  	<td>Counts the number of times a manifest could not be parsed successfully.</td>
  </tr>

</table>

### Mqtt connection metrics

<table>
  <tr>
	<th>metric</th>
    <th>type</th>
    <th>unit</th>
	<th>attributes</th>
	<th>description</th>
  </tr>

  <tr>
	<td>mqtt2otel.mqtt.subscriptions.count</td>
  	<td>Gauge&lt;int&gt;</td>
  	<td></td>
	<td>        
		mqtt.connection
	</td>
	<td>This is the sum of all mqtt subscriptions. A wildcard subscription counts as one subscription.</td>
	<td></td>
  </tr>

  <tr>
  	<td>mqtt2otel.mqtt.messages.received</td>
  	<td>Counter&lt;int&gt;</td>
  	<td></td>
  	<td></td>
  	<td>The number of mqtt messages received.</td>
  </tr>

  <tr>
  	<td>mqtt2otel.mqtt.connection.count</td>
  	<td>Gauge&lt;int&gt;</td>
  	<td></td>
  	<td></td>
  	<td>The total amount of mqtt broker connections.</td>
  </tr>

  <tr>
  	<td>mqtt2otel.mqtt.payload_size_in_bytes</td>
  	<td>Histogram&lt;long&gt;</td>
  	<td>bytes</td>
  	<td></td>
  	<td>The size of the mqtt payloads in bytes.</td>
  </tr>

</table>

## Otel connection metrics

<table>
  <tr>
	<th>metric</th>
    <th>type</th>
    <th>unit</th>
	<th>attributes</th>
	<th>description</th>
  </tr>

  <tr>
  	<td>mqtt2otel.otel.connections.sum</td>
  	<td>Gauge&lt;int&gt;</td>
  	<td></td>
  	<td></td>
  	<td>This is the sum of all active connections to open telemetry endpoints.</td>
  </tr>

</table>

## Processor metrics

<table>
  <tr>
	<th>metric</th>
    <th>type</th>
    <th>unit</th>
	<th>attributes</th>
	<th>description</th>
  </tr>

  <tr>
  	<td>mqtt2otel.processor.total.duration_in_us</td>
  	<td>Histogram&lt;double&gt;</td>
  	<td>&micro;s</td>
  	<td>
      subscription.name<br/>
      subscription.id<br/>
      subscription.connection<br/>
      processor.name<br/>
      processor.id<br/>
      processor.otel.connection<br/>
    </td>
  	<td>The total duration of processing all metric and otel rules of a single processor.</td>
  </tr>

  <tr>
  	<td>mqtt2otel.processor.metrics.rule.duration_in_us</td>
  	<td>Histogram&lt;double&gt;</td>
  	<td>&micro;s</td>
  	<td>
      subscription.name<br/>
      subscription.id<br/>
      subscription.connection<br/>
      processor.name<br/>
      processor.id<br/>
      processor.otel.connection<br/>
      rule.name<br/>
      rule.id<br/>
      rule.connection<br/>
      rule.instrument<br/>
    </td>
  	<td>The duration of processing a single metrics rule inside a processor.</td>
  </tr>

  <tr>
  	<td>mqtt2otel.processor.logging.rule.duration_in_us</td>
  	<td>Histogram&lt;double&gt;</td>
  	<td>&micro;s</td>
  	<td>
      subscription.name<br/>
      subscription.id<br/>
      subscription.connection<br/>
      processor.name<br/>
      processor.id<br/>
      processor.otel.connection<br/>
      rule.name<br/>
      rule.id<br/>
      rule.connection<br/>
      rule.instrument<br/>
      rule.category<br/>
      rule.payload_type<br/>
    </td>
  	<td>The duration of processing a single logging rule inside a processor.</td>
  </tr>

  <tr>
  	<td>mqtt2otel.processor.processing_errors</td>
  	<td>Counter&lt;int&gt;</td>
  	<td></td>
  	<td>
      subscription.name<br/>
      subscription.id<br/>
      subscription.connection<br/>
      processor.name<br/>
      processor.id<br/>
      processor.otel.connection<br/>
    </td>
  	<td>A counter for measuring the amount of errors, that appeared while processing incoming data.</td>
  </tr>

  <tr>
  	<td>mqtt2otel.processor.log_entries.count</td>
  	<td>Counter&lt;int&gt;</td>
  	<td></td>
  	<td>
      subscription.name<br/>
      subscription.id<br/>
      subscription.connection<br/>
      processor.name<br/>
      processor.id<br/>
      processor.otel.connection<br/>
      rule.name<br/>
      rule.id<br/>
      rule.connection<br/>
      rule.category<br/>
      rule.payload_type<br/>
    </td>
  	<td>This is the count of created log entries.</td>
  </tr>

  <tr>
  	<td>mqtt2otel.processor.metrics.count</td>
  	<td>Counter&lt;int&gt;</td>
  	<td></td>
  	<td>
      subscription.name<br/>
      subscription.id<br/>
      subscription.connection<br/>
      processor.name<br/>
      processor.id<br/>
      processor.otel.connection<br/>
      rule.name<br/>
      rule.id<br/>
      rule.connection<br/>
      rule.instrument<br/>
    </td>
  	<td>This is the count of all created metrics.</td>
  </tr>

  <tr>
  	<td></td>
  	<td></td>
  	<td></td>
  	<td></td>
  	<td></td>
  </tr>

</table>