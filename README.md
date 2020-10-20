[![Build Status](https://dev.azure.com/zdenek-jelinek/home-room-condition-monitor/_apis/build/status/zdenek-jelinek.home-room-condition-monitor?branchName=master)](https://dev.azure.com/zdenek-jelinek/home-room-condition-monitor/_build/latest?definitionId=1&branchName=master)

# Room Condition Monitor (rcm)
A pet project for room temperature, pressure and humidity monitoring and reporting.

The project consists of device, back-end and planned front-end parts.

Please note that this is an effort with limited resources focusing on exploring technologies.
Some parts may unfortunately not be cross-platform or may be somewhat more complex than necessary.

## Device
The device part focuses on collecting data locally and optionally uploading them to remote storage of back end.

The collected data are also available over a locally served ASP.NET Core Razor Pages web UI.

The implementation currently supports Linux with BME280 sensor connected over I2C.

### Prerequisities
* [.NET Core 3.1 SDK](https://dotnet.microsoft.com/download)
* [Node.js and npm](https://nodejs.org/en/) (npm is auto-included in Node.js)

### Configuration
The application is configured according to [ASP.NET Core configuration](https://docs.microsoft.com/en-us/aspnet/core/fundamentals/configuration/?view=aspnetcore-3.1).

The following additional properties are supported:

| Property | Possible values | Required | Description |
| --- | --- | --- | --- |
| Measurements:Access:Mode | STUB, I2C | Required | Defines the data access mode for periodic condition measurements.<br>In STUB mode, measurements are generated randomly within sensible ranges.<br>In I2C mode, measurements are read from I2C bus. |
| DataStorage:Path | path string | Required | Specifies a directory used to store measurements and backend connection parameters. |


#### I2C
When configured with Measurement:Access:Mode = I2C, the following options are also applied:

| Property | Possible values | Required | Description |
| --- | --- | --- | --- |
| Measurements:Access:BusAddress | string | Required | I2C bus address, e.g. `/dev/i2c-1` from `ls /dev/*i2c*` |
| Measurements:Access:DeviceAddress | byte | Required | I2C device address, e.g. `0x76` from `i2cdetect -y 1` |

### Running
#### Development
Executing  `dotnet run --project src/Device/Rcm.Device.Web --measurements:access:mode=STUB --dataStorage:path=../../data --console` will run the data collection and web UI service with basic development configuration.  

The `--console` switch causes the application to run in console. If the switch is omitted, the application will asume it is running as a Linux systemd service.

Note that the `--console` switch is required to come last in order to avoid misinterpretation by ASP.NET Core configuration.

Except `--console`, the parameters may also be specified in appsettings.json files or environment variables. See [configuration section](#configuration) for more details.

#### Device
For actual device deployment, it is suggested to publish the application binary and run it as a systemd service on the target system.

For a self-contained release publish, run `dotnet publish src/Device/Rcm.Device.Web/Rcm.Device.Web.csproj --runtime linux-arm --configuration Release --self-contained`

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

## Back end
TODO

## Front end
This component is not implemented yet.
