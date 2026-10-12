---
title: "Usage telemetry"
weight: 50
bookCollapseSection: false
---

# Usage telemetry

This application is sending telemetry signals to help the developers understand how the application is used. No personal information is tracked. If you do not want 
this, you can turn it off via the `DisableUsageTelemetry` setting (see [ApplicationSettings](../../applicationsettings)).

## What is send

The following data will be send to the server:

* The unique instance id (this is a random id that cannot be associated to a person).
* The operating system name
* The operating system version
* The mqtt2otel version
* The processor architecture
* The amount of logical cpu cores
* The total available RAM
* The amount of otel connections inside the manifest (not the connection itself, only a count)
* The amount of mqtt connections inside the manifest (not the connection itself, only a count)
* The amount of processors inside the manifest
* The amount of log rules inside the manifest
* The amount of metric rules inside the manifest
* The amount of mqtt subscriptions used in parallel
* The total message count
* The logging rule processing time (as histogram data)
* The metric rule processing time (as histogram data)

## Telemetry and Privacy

> [!Note]
> ### Please support the project
>
> We completely understand and respect that you do not want to be tracked. Your privacy matters to us and we design our telemetry with that principle first.
>
> The data helps us find crashes, prioritize fixes, improve performance and provide features that matter to real users. Small, anonymous signals from many users 
> let us spend time where it benefits the community most.
>
> If you value the project and want to support its continued improvement, please consider leaving telemetry enabled. The data is anonymous, documented, 
> communicated openly and directly helps us **making the application better for everyone**.
>
> Thanks for your support.
