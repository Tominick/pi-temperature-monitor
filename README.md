# pi-temperature-monitor
Raspberry Pi-based temperature monitor.

The project is divided into parts:
* **WebMonitor**
An ASP.Net Core web API to read and produce data, plus a web page to see the data
* **TempReader**
A .Net Core console application to read the temperature and humidity from a Dht sensor
* **TempReaderService**
A systemd version of TempReader

![DatesRange](https://user-images.githubusercontent.com/20950618/65949096-d6d50980-e43b-11e9-8a1b-31c1f0dfcde0.png)

# Example of .service file
```
[Unit]
Description=Temperature Reader Service
After=time-sync.target

[Service]
Type=simple
User=pi
WorkingDirectory=/home/pi/TempReaderService
ExecStart=/home/pi/TempReaderService/TempReaderService

[Install]
WantedBy=multi-user.target
```
