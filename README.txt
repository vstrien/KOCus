Why
---
This is my shot on creating a Windows Phone app communicating with a cars On-Board Diagnostics system. Tried to download an app, but that didn't have the possibilities I wanted. iOS-OBD-apps are typically geared towards troubleshooting, and quite expensive as well, so I decided to build an app myself.

Goal
----
When finished, this project should consist of:
- a portable library for communicating with an OBD-device, regardless of the communication medium. The library should do requests and decode results.
- a Windows Phone 7 app using this library, displaying information in a dashboard and pushing data together with GPS sensor data into an online database for analysis purposes.
- everything covered with unit tests

Further goals
-------------
It would be great to have configuration files (XML) to be able to add / alter the description and handling of PID-codes without having to alter the code. Although it's always necessary to distinguish methods to handle the conversation, there are some general classes to divide the data into. It would be great if data inside those classes would be configurable without having to mess with the code.

Also great would be a feature to re-code the UI part for Xamarin (or another multi-platform framework), so only small parts need to be written for each phone model.

Current state
-------------
Currently, you'll need an OBD-II adapter with Wifi capabilities. You can get these for example at DealExtreme. For Windows Phone 7 or iOS you'll always need a Wifi-adapter. For Windows Phone 8, Android, or higher Windows versions, you should be able to use a bluetooth-adapter.