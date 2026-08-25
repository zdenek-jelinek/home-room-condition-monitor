[![Build Status](https://dev.azure.com/zdenek-jelinek/home-room-condition-monitor/_apis/build/status/zdenek-jelinek.home-room-condition-monitor?branchName=master)](https://dev.azure.com/zdenek-jelinek/home-room-condition-monitor/_build/latest?definitionId=1&branchName=master)

# Room Condition Monitor (rcm)
A pet project for room temperature, pressure and humidity monitoring and reporting.

_Please note that this is an effort with limited resources focusing on exploring technologies.
Some parts may not be cross-platform or may be more complex than necessary in order to facilitate experimentation._

## Technical introduction
This is a .NET application intended to run as a service on a device equipped with sensors for collecting room condition data.
The sensor data are collected periodically and stored on the device's local filesystem.

A simple UI for accessing the data is served locally using ASP.NET Core Razor Pages framework.

The implementation currently supports Linux with BME280 sensor connected over I2C.

It can also run locally on any platform, using generated data instead of sensors.

## Development prerequisities
* [.NET 10 SDK](https://dotnet.microsoft.com/download)
* [Node.js and npm](https://nodejs.org/en/) (npm is auto-included in Node.js)

## Configuration
The application uses [ASP.NET Core configuration](https://docs.microsoft.com/en-us/aspnet/core/fundamentals/configuration).

In addition, the following properties are supported:

| Property | Possible values | Required | Description |
| --- | --- | --- | --- |
| Measurements:Access:Mode | STUB, I2C | Required | Defines the data access mode for periodic condition measurements.<br>In STUB mode, measurements are generated randomly within sensible ranges.<br>In I2C mode, measurements are read from I2C bus. |
| DataStorage:Path | path string | Required | Specifies a directory used to store measurements. If a relative path is supplied, it is evaluated based on the working directory of the executing binary. |


### I2C
When configured with Measurement:Access:Mode = I2C, the following options are also applied:

| Property | Possible values | Required | Description |
| --- | --- | --- | --- |
| Measurements:Access:BusAddress | string | Required | I2C bus address, e.g. `/dev/i2c-1` from `ls /dev/*i2c*` |
| Measurements:Access:DeviceAddress | byte | Required | I2C device address, e.g. `0x76` from `i2cdetect -y 1` |

Note that the above requires I2C interface to be enabled on the device through `raspi-config` to detect I2C device and i2c-tools to be installed (`sudo apt-get install i2c-tools`) to run i2cdetect.

## Running
### Development
Executing  `dotnet run --project src/Rcm.Web --measurements:access:mode=STUB --dataStorage:path=../../../data --console` will run the data collection and web UI service with basic development configuration.  

The `--console` switch causes the application to run in console. If the switch is omitted, the application will asume it is running as a Linux systemd service.

Note that the `--console` switch is required to come last in order to avoid misinterpretation by ASP.NET Core configuration.

Except `--console`, the parameters may also be specified in appsettings.json files or environment variables. See [configuration section](#configuration) for more details.

### Device
For actual device deployment, it is suggested to publish the application binary and run it as a systemd service on the target system.

For a self-contained release publish, run `dotnet publish src/Rcm.Web/Rcm.Web.csproj --runtime linux-arm --configuration Release --self-contained`

The following unit definition can be used as a baseline, it assumes the application was deployed to `/home/pi/apps/rcm/bin` and binds to port 80 (HTTP).
```
[Unit]
Description=Room Condition Monitor

[Service]
Type=notify

# Configure data storage location and I2C bus
Environment=DATASTORAGE__PATH=/home/pi/apps/rcm/data MEASUREMENTS__ACCESS__MODE=I2C MEASUREMENTS__ACCESS__BUSADDRESS=/dev/i2c-1 MEASUREMENTS__ACCESS__DEVICEADDRESS=0x76

# Start the self-contained application with binding on port 80
ExecStart=/home/pi/apps/rcm/bin/Rcm.Web --urls="http://0.0.0.0:80"

# Restart on error after 60s timeout
Restart=on-failure
RestartSec=60

[Install]
# Start before non-GUI shells
WantedBy=multi-user.target
```

## Addendum
Originally, I also planned adding a serverless back-end to store the sensor data, and a publicly accessible front-end for presentation.
However, given this is a low-priority effort, I never fully completed these parts and they became a maintenance burden over the years.
