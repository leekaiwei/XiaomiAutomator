using Microsoft.Extensions.Configuration;
using Microsoft.Win32;
using Miio.Devices.Exceptions;
using Miio.Devices.Implementations.Yeelight;
using Polly;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace SmartLed
{
    public class Program
    {
        private static DateTime timeToActivate;
        private static IList<GenericYeelightDevice> _devices = new List<GenericYeelightDevice>();
        private static readonly IAsyncPolicy _policy = Policy
            .Handle<DeviceCommunicationException>()
            .WaitAndRetryForeverAsync(retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)));
        private static readonly IConfiguration _configuration = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json", false, true)
            .Build();

        static async Task Main()
        {
            SystemEvents.SessionSwitch += new SessionSwitchEventHandler(DetectSessionSwitch);

            var tasks = new List<Task>
            {
                GetSunset(),
                InitialiseDevices(),
            };

            await Task.WhenAll(tasks);

            if (DateTime.UtcNow >= timeToActivate)
            {
                await TurnOn();
            }
            else
            {
                var timeToActive = DateTime.UtcNow - timeToActivate;
                await Task.Delay(timeToActive);

                await TurnOn();
            }

            Console.ReadLine();

            SystemEvents.SessionSwitch -= DetectSessionSwitch;
        }

        private static async void DetectSessionSwitch(object sender, SessionSwitchEventArgs eventArguments)
        {
            if (eventArguments.Reason == SessionSwitchReason.SessionLock)
            {
                await TurnOff();
            }
            else if (eventArguments.Reason == SessionSwitchReason.SessionUnlock)
            {
                await TurnOn();
            }
        }

        private static async Task TurnOn()
        {
            Console.WriteLine("Turning on...");

            var tasks = new List<Task>();
            foreach (var device in _devices)
            {
                tasks.Add(_policy.ExecuteAsync(() => device.TurnOnSmoothly(1000)));
            }

            await Task.WhenAll(tasks);

            Console.WriteLine("Turned on!");
        }

        private static async Task TurnOff()
        {
            Console.WriteLine("Turning off...");

            if (!_configuration.GetValue<bool>("Testing"))
            {
                await Task.Delay(_configuration.GetValue<int>("Delay"));
            }

            var tasks = new List<Task>();
            foreach (var device in _devices)
            {
                tasks.Add(_policy.ExecuteAsync(() => device.TurnOffSmoothly(1000)));
            }

            await Task.WhenAll(tasks);

            Console.WriteLine("Turned off!");
        }

        private static async Task GetSunset()
        {
            Console.WriteLine("Getting sunset data...");

            if (_configuration.GetValue<bool>("Testing"))
            {
                timeToActivate = DateTime.UtcNow;
            }
            else
            {
                var http = new HttpClient();
                var location = _configuration.GetSection("Location");
                var response = await http.GetAsync("https://api.sunrise-sunset.org/json?lat=" + location.GetValue<string>("Latitude") + "&lng=" + location.GetValue<string>("Longitude"));
                var data = JsonSerializer.Deserialize<JsonElement>(await response.Content.ReadAsStringAsync());
                var sunsetProperty = data.GetProperty("results").GetProperty("sunset").ToString();
                timeToActivate = DateTime.ParseExact(sunsetProperty, "h:mm:ss tt", null).AddMinutes(-30);
            }

            Console.WriteLine("Active Time: {0}", timeToActivate);
        }

        private static async Task InitialiseDevices()
        {
            foreach (var device in _configuration.GetSection("Devices").GetChildren())
            {
                var yeelightDevice = new GenericYeelightDevice(device["Ip"], device["Token"], device["Name"]);

                bool handshaked;
                do
                {
                    Console.WriteLine("Initialising...");
                    handshaked = await yeelightDevice.MakeHandshake();
                }
                while (!handshaked);

                _devices.Add(yeelightDevice);
            }

            Console.WriteLine("Initialised!");
        }
    }
}