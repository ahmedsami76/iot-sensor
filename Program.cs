using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.Devices.Client.Transport;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Specialized;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Azure.Storage.Blobs.Models;
using System.Diagnostics;

/// <summary>
/// Represents the main program for the IoT Sensor application.
/// </summary>
namespace IotSensor
{
    class Program
    {
        public static IConfiguration? Configuration { get; private set; }

        // Static field to hold the last C2D message received
        private static string _lastC2DMessage = string.Empty;

        // Static field to hold the current fan status
        private static bool _fanOn = false;

        static async Task Main(string[] args)
        {
            Configuration = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .Build();

            string? ioTHubName = Configuration["IoTHub:IoTHubName"];
            string? deviceId = Configuration["IoTHub:DeviceId"];
            string? key = Configuration["IoTHub:Key"];

            string connectionString = $"HostName={ioTHubName}.azure-devices.net;DeviceId={deviceId};SharedAccessKey={key}";
            using DeviceClient deviceClient = DeviceClient.CreateFromConnectionString(connectionString, TransportType.Mqtt);

            // Set up handlers for messages and direct methods
            await deviceClient.SetMethodHandlerAsync("TurnFanOn", TurnFanOn, null);
            await deviceClient.SetMethodHandlerAsync("TurnFanOff", TurnFanOff, null);
            await deviceClient.SetReceiveMessageHandlerAsync(ReceiveMessageHandler, deviceClient);

            // Run the file upload sample
            var fileUploadSample = new FileUploadSample(deviceClient);
            await fileUploadSample.RunSampleAsync();

            // Send telemetry
            var cts = new CancellationTokenSource();
            await SendTelemetryAsync(deviceClient, cts.Token);
        }

        static async Task SendTelemetryAsync(DeviceClient deviceClient, CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                var telemetryData = GetTelemetryData();
                string messageString = JsonConvert.SerializeObject(telemetryData);
                using var message = new Message(Encoding.UTF8.GetBytes(messageString))
                {
                    ContentType = "application/json",
                    ContentEncoding = "utf-8",
                };

                await deviceClient.SendEventAsync(message);
                Console.WriteLine($"Sent message: {messageString}");

                await Task.Delay(1000); // Send telemetry every second
            }
        }

        private static Task<MethodResponse> TurnFanOn(MethodRequest methodRequest, object userContext)
        {
            _fanOn = true;
            Console.WriteLine("Fan turned on.");

            string result = "{\"result\":\"Executed direct method: TurnFanOn\"}";
            return Task.FromResult(new MethodResponse(Encoding.UTF8.GetBytes(result), 200));
        }

        private static Task<MethodResponse> TurnFanOff(MethodRequest methodRequest, object userContext)
        {
            _fanOn = false;
            Console.WriteLine("Fan turned off.");

            string result = "{\"result\":\"Executed direct method: TurnFanOff\"}";
            return Task.FromResult(new MethodResponse(Encoding.UTF8.GetBytes(result), 200));
        }

        static async Task ReceiveMessageHandler(Message message, object userContext)
        {
            string messageData = Encoding.UTF8.GetString(message.GetBytes());
            Console.WriteLine($"Received message: {messageData}");

            // Store the last C2D message received
            _lastC2DMessage = messageData;

            var deviceClient = (DeviceClient)userContext;
            await deviceClient.CompleteAsync(message); // mark the message as processed
        }

        private static TelemetryData GetTelemetryData()
        {
            Random rnd = new Random();
            return new TelemetryData
            {
                Temperature = rnd.NextDouble() * 40,
                Humidity = rnd.NextDouble() * 100,
                Pressure = rnd.NextDouble() * 2000,
                Luminosity = rnd.NextDouble() * 10000,
                Motion = rnd.Next(0, 2) == 1,
                BatteryLevel = rnd.NextDouble() * 100,
                FanOn = _fanOn, // Use the current fan status
                LastC2DMessage = _lastC2DMessage // Include the last C2D message received
            };
        }

        // Telemetry data structure
        public class TelemetryData
        {
            public double Temperature { get; set; }
            public double Humidity { get; set; }
            public double Pressure { get; set; }
            public double Luminosity { get; set; }
            public bool Motion { get; set; }
            public double BatteryLevel { get; set; }
            public bool FanOn { get; set; } // Non-static property for serialization
            public string? LastC2DMessage { get; set; } // Property to include the last C2D message, nullable
        }

        // FileUploadSample class and methods
        public class FileUploadSample
        {
            private readonly DeviceClient _deviceClient;

            public FileUploadSample(DeviceClient deviceClient)
            {
                _deviceClient = deviceClient;
            }

            public async Task RunSampleAsync()
            {
                string filePath = Environment.GetEnvironmentVariable("FILE_UPLOAD_PATH") ?? "upload_me.txt";

                using var fileStreamSource = new FileStream(filePath, FileMode.Open);
                var fileName = Path.GetFileName(fileStreamSource.Name);

                Console.WriteLine($"Uploading file {fileName}");

                var fileUploadTime = Stopwatch.StartNew();

                var fileUploadSasUriRequest = new FileUploadSasUriRequest { BlobName = fileName };

                Console.WriteLine("Getting SAS URI from IoT Hub to use when uploading the file...");
                FileUploadSasUriResponse sasUri = await _deviceClient.GetFileUploadSasUriAsync(fileUploadSasUriRequest);
                Uri uploadUri = sasUri.GetBlobUri();

                Console.WriteLine($"Successfully got SAS URI ({uploadUri}) from IoT Hub");

                try
                {
                    Console.WriteLine($"Uploading file {fileName} using the Azure Storage SDK and the retrieved SAS URI for authentication");

                    var blockBlobClient = new BlockBlobClient(uploadUri);
                    await blockBlobClient.UploadAsync(fileStreamSource, new BlobUploadOptions());

                    Console.WriteLine("Successfully uploaded the file to Azure Storage");

                    var successfulFileUploadCompletionNotification = new FileUploadCompletionNotification
                    {
                        CorrelationId = sasUri.CorrelationId,
                        IsSuccess = true,
                        StatusCode = 200,
                        StatusDescription = "Success"
                    };

                    await _deviceClient.CompleteFileUploadAsync(successfulFileUploadCompletionNotification);
                    Console.WriteLine("Notified IoT Hub that the file upload succeeded and that the SAS URI can be freed.");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed to upload file to Azure Storage using the Azure Storage SDK due to {ex}");

                    var failedFileUploadCompletionNotification = new FileUploadCompletionNotification
                    {
                        CorrelationId = sasUri.CorrelationId,
                        IsSuccess = false,
                        StatusCode = 500,
                        StatusDescription = ex.Message
                    };

                    await _deviceClient.CompleteFileUploadAsync(failedFileUploadCompletionNotification);
                    Console.WriteLine("Notified IoT Hub that the file upload failed and that the SAS URI can be freed");
                }
                finally
                {
                    fileUploadTime.Stop();
                    Console.WriteLine($"Time to upload file: {fileUploadTime.Elapsed}.");
                }
            }
        }
    }
}
