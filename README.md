# Ingestinator

The ingestinator is a tool aimed at automating data tracking and ingesting of the output of a Virtual Production Shoot.
The program collects data and events during the shooting day by interfacing with the camera (via a [Bluetooth Relay for a Blackmagic Ursa](https://github.com/TeraNaidja/ESP32_BlackmagicBluetoothRelay)), and once the shooting day has wrapped all removable media can be plugged into the machine after which all relevant files will be copied to a configured folder and optionally tracked in a tracking system such as ShotGrid.

