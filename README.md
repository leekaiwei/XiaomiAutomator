# XiaomiAutomator

Allows automation of Xiaomi devices from PC. Uses https://github.com/Koli96/Miio.NET to communicate with devices. Getting device tokens is possible via https://github.com/Maxmudjon/Get_MiHome_devices_token/tree/1.0.2.

## Current Functionality

<ul>
  <li>It will turn on lights 30 minutes from sunset based on latitude and longitude.</li>
  <li>It will turn off lights after a configured `Delay` value which is in milliseconds after you lock your PC.</li>
  <li>It will tun on lights immediately after unlocking PC.</li>
</ul>

Only tested with Mi LED Smart Bulb but it should work with Yeelight devices too.
