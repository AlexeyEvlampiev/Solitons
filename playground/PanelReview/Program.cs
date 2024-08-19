using Azure.Core;
using Azure.Identity;
using Azure.Messaging.EventGrid;
using Azure.Messaging;
using Solitons;
using Solitons.Diagnostics;
using System.Diagnostics;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using System.Text;
using Newtonsoft.Json.Linq;

namespace PanelReview;

internal class Program
{
    static async Task Main(string[] args)
    {
        string topicEndpoint = "https://jazzy-logs-topic.westeurope-1.eventgrid.azure.net/api/events";
        var logger = DummyAsyncLogger.Create(
            topicEndpoint,
            new DefaultAzureCredential(),
            options => options
                .BufferErrors(TimeSpan.FromMilliseconds(2000), 1000)
                .BufferWarnings(TimeSpan.FromMilliseconds(2000), 1000)
                .BufferWarnings(TimeSpan.FromMilliseconds(2000), 1000));
        await Observable
            .Range(0, 1)
            .SelectMany(_ => logger
                .ErrorAsync(new OutOfMemoryException("Oops..."))
                .ToObservable());

        Console.WriteLine("Sent");
        await Task.Delay(TimeSpan.FromHours(1));
    }
}


public sealed class DummyAsyncLogger : BufferedAsyncLogger
{
    record Location(string Source, string File, int Line, string Level, string Message);
    [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
    private readonly EventGridPublisherClient _client;

    private readonly Location[] _locations;


    private DummyAsyncLogger(EventGridPublisherClient client, Action<Options> config)
        : base(config)
    {
        _client = client;
        var sources =
            from @case in new[]
            {
        new
        {
            source = "ImageProcessors",
            locations = new[]
            {
                new { file = "Contoso.ImageProcessors.ImageLoader.cs", line = 124, level = LogLevel.Info, message = "Image loaded successfully." },
                new { file = "Contoso.ImageProcessors.ImageSaver.cs", line = 87, level = LogLevel.Info, message = "Image saved to disk." },
                new { file = "Contoso.ImageProcessors.ImageResizer.cs", line = 156, level = LogLevel.Info, message = "Image resized to new dimensions." },
                new { file = "Contoso.ImageProcessors.ImageFilter.cs", line = 209, level = LogLevel.Info, message = "Applied filter to image." },
                new { file = "Contoso.ImageProcessors.ImageCropper.cs", line = 45, level = LogLevel.Info, message = "Image cropped to specified area." },
                new { file = "Contoso.ImageProcessors.ImageCompressor.cs", line = 312, level = LogLevel.Info, message = "Image compressed to reduce file size." },
                new { file = "Contoso.ImageProcessors.ImageConverter.cs", line = 67, level = LogLevel.Info, message = "Image converted to desired format." },
                new { file = "Contoso.ImageProcessors.ImageLoader.cs", line = 142, level = LogLevel.Info, message = "Loaded image metadata." },
                new { file = "Contoso.ImageProcessors.ImageSaver.cs", line = 104, level = LogLevel.Info, message = "Saved image with updated metadata." },
                new { file = "Contoso.ImageProcessors.ImageResizer.cs", line = 186, level = LogLevel.Info, message = "Resized image for thumbnail creation." },
                new { file = "Contoso.ImageProcessors.ImageFilter.cs", line = 239, level = LogLevel.Info, message = "Applied color correction filter." },
                new { file = "Contoso.ImageProcessors.ImageCropper.cs", line = 65, level = LogLevel.Info, message = "Cropped image to aspect ratio." },
                new { file = "Contoso.ImageProcessors.ImageCompressor.cs", line = 332, level = LogLevel.Info, message = "Compressed image for web optimization." },
                new { file = "Contoso.ImageProcessors.ImageConverter.cs", line = 88, level = LogLevel.Info, message = "Converted image from PNG to JPEG." },
                new { file = "Contoso.ImageProcessors.ImageProcessorBase.cs", line = 23, level = LogLevel.Info, message = "Initialized image processing base." },
                new { file = "Contoso.ImageProcessors.ImageProcessorBase.cs", line = 45, level = LogLevel.Info, message = "Base class configured for image processing." },
                new { file = "Contoso.ImageProcessors.ImageLoader.cs", line = 168, level = LogLevel.Info, message = "Image loaded for editing." },
                new { file = "Contoso.ImageProcessors.ImageSaver.cs", line = 130, level = LogLevel.Info, message = "Final image saved after processing." },
                new { file = "Contoso.ImageProcessors.ImageResizer.cs", line = 215, level = LogLevel.Info, message = "Resized image for high-resolution display." },
                new { file = "Contoso.ImageProcessors.ImageFilter.cs", line = 273, level = LogLevel.Info, message = "Applied vintage filter to image." },
                new { file = "Contoso.ImageProcessors.ImageEditor.cs", line = 184, level = LogLevel.Info, message = "Opened image in editor." },
                new { file = "Contoso.ImageProcessors.ImageMetadataReader.cs", line = 78, level = LogLevel.Info, message = "Read image EXIF data." },
                new { file = "Contoso.ImageProcessors.ImageOptimizer.cs", line = 120, level = LogLevel.Info, message = "Optimized image for faster load times." },
                new { file = "Contoso.ImageProcessors.ImagePipeline.cs", line = 243, level = LogLevel.Info, message = "Image processing pipeline executed successfully." },

                // Warning Locations
                new { file = "Contoso.ImageProcessors.ImageResizer.cs", line = 203, level = LogLevel.Warning, message = "Image resize operation took longer than expected." },
                new { file = "Contoso.ImageProcessors.ImageLoader.cs", line = 151, level = LogLevel.Warning, message = "Potential memory issue detected while loading image." },
                new { file = "Contoso.ImageProcessors.ImageSaver.cs", line = 114, level = LogLevel.Warning, message = "Disk space running low during image save operation." },
                new { file = "Contoso.ImageProcessors.ImageCropper.cs", line = 72, level = LogLevel.Warning, message = "Image crop dimensions are outside the expected range." },
                new { file = "Contoso.ImageProcessors.ImageCompressor.cs", line = 340, level = LogLevel.Warning, message = "Image compression resulted in quality loss." },
                new { file = "Contoso.ImageProcessors.ImageOptimizer.cs", line = 132, level = LogLevel.Warning, message = "Optimization skipped due to file corruption." },
                new { file = "Contoso.ImageProcessors.ImagePipeline.cs", line = 267, level = LogLevel.Warning, message = "Processing pipeline encountered an unexpected delay." },

                // Error Locations
                new { file = "Contoso.ImageProcessors.ImageResizer.cs", line = 220, level = LogLevel.Error, message = "Failed to resize image due to unsupported format." },
                new { file = "Contoso.ImageProcessors.ImageLoader.cs", line = 180, level = LogLevel.Error, message = "Image failed to load due to insufficient memory." },
                new { file = "Contoso.ImageProcessors.ImageSaver.cs", line = 142, level = LogLevel.Error, message = "Error saving image to disk." },
                new { file = "Contoso.ImageProcessors.ImageMetadataReader.cs", line = 99, level = LogLevel.Error, message = "Error reading image metadata." },
                new { file = "Contoso.ImageProcessors.ImagePipeline.cs", line = 289, level = LogLevel.Error, message = "Image processing pipeline failed to complete." }
            }
        },
        new
        {
            source = "UserPortal",
            locations = new[]
            {
                new { file = "Contoso.UserPortal.HomePage.cs", line = 342, level = LogLevel.Info, message = "Home page loaded successfully." },
                new { file = "Contoso.UserPortal.LoginPage.cs", line = 102, level = LogLevel.Info, message = "User login form displayed." },
                new { file = "Contoso.UserPortal.ProfilePage.cs", line = 88, level = LogLevel.Info, message = "User profile page displayed." },
                new { file = "Contoso.UserPortal.Dashboard.cs", line = 278, level = LogLevel.Info, message = "Dashboard data loaded for user." },
                new { file = "Contoso.UserPortal.SettingsPage.cs", line = 134, level = LogLevel.Info, message = "User settings loaded." },
                new { file = "Contoso.UserPortal.Notifications.cs", line = 189, level = LogLevel.Info, message = "User notifications retrieved." },
                new { file = "Contoso.UserPortal.UserController.cs", line = 41, level = LogLevel.Info, message = "UserController initialized." },
                new { file = "Contoso.UserPortal.HomePage.cs", line = 360, level = LogLevel.Info, message = "Home page refreshed successfully." },
                new { file = "Contoso.UserPortal.LoginPage.cs", line = 118, level = LogLevel.Info, message = "Login attempt initiated." },
                new { file = "Contoso.UserPortal.ProfilePage.cs", line = 105, level = LogLevel.Info, message = "User profile updated." },
                new { file = "Contoso.UserPortal.Dashboard.cs", line = 296, level = LogLevel.Info, message = "Dashboard refreshed with latest data." },
                new { file = "Contoso.UserPortal.SettingsPage.cs", line = 152, level = LogLevel.Info, message = "User settings saved." },
                new { file = "Contoso.UserPortal.Notifications.cs", line = 207, level = LogLevel.Info, message = "Notifications marked as read." },
                new { file = "Contoso.UserPortal.UserController.cs", line = 56, level = LogLevel.Info, message = "User data loaded into controller." },
                new { file = "Contoso.UserPortal.UserController.cs", line = 68, level = LogLevel.Info, message = "User session validated." },
                new { file = "Contoso.UserPortal.UserProfile.cs", line = 49, level = LogLevel.Info, message = "User profile data retrieved." },
                new { file = "Contoso.UserPortal.SettingsPage.cs", line = 162, level = LogLevel.Info, message = "Loaded settings for user preferences." },
                new { file = "Contoso.UserPortal.Notifications.cs", line = 218, level = LogLevel.Info, message = "New notifications fetched from server." },

                // Warning Locations
                new { file = "Contoso.UserPortal.Dashboard.cs", line = 286, level = LogLevel.Warning, message = "Dashboard load time exceeded threshold." },
                new { file = "Contoso.UserPortal.HomePage.cs", line = 365, level = LogLevel.Warning, message = "Home page encountered a slow network response." },
                new { file = "Contoso.UserPortal.ProfilePage.cs", line = 115, level = LogLevel.Warning, message = "Profile update partially completed." },
                new { file = "Contoso.UserPortal.SettingsPage.cs", line = 167, level = LogLevel.Warning, message = "Settings save operation took longer than expected." },
                new { file = "Contoso.UserPortal.Notifications.cs", line = 221, level = LogLevel.Warning, message = "Notification fetch request timed out." },
                new { file = "Contoso.UserPortal.UserProfile.cs", line = 60, level = LogLevel.Warning, message = "User profile data load was incomplete." },

                // Error Locations
                new { file = "Contoso.UserPortal.Dashboard.cs", line = 310, level = LogLevel.Error, message = "Failed to load dashboard data for user." },
                new { file = "Contoso.UserPortal.LoginPage.cs", line = 137, level = LogLevel.Error, message = "User login failed due to invalid credentials." },
                new { file = "Contoso.UserPortal.UserController.cs", line = 72, level = LogLevel.Error, message = "User session could not be created." },
                new { file = "Contoso.UserPortal.SettingsPage.cs", line = 175, level = LogLevel.Error, message = "Error saving user settings." }
            }
        },
        new
        {
            source = "RestApi",
            locations = new[]
            {
                new { file = "Contoso.RestApi.ImagesController.cs", line = 543, level = LogLevel.Info, message = "ImagesController received a request for image list." },
                new { file = "Contoso.RestApi.UserController.cs", line = 65, level = LogLevel.Info, message = "UserController initialized." },
                new { file = "Contoso.RestApi.AuthController.cs", line = 91, level = LogLevel.Info, message = "Authentication request received." },
                new { file = "Contoso.RestApi.DataController.cs", line = 237, level = LogLevel.Info, message = "Data request processed successfully." },
                new { file = "Contoso.RestApi.PaymentsController.cs", line = 147, level = LogLevel.Info, message = "Payment transaction started." },
                new { file = "Contoso.RestApi.NotificationsController.cs", line = 198, level = LogLevel.Info, message = "Notifications sent to user." },
                new { file = "Contoso.RestApi.ReportsController.cs", line = 284, level = LogLevel.Info, message = "Report generated successfully." },
                new { file = "Contoso.RestApi.ImagesController.cs", line = 563, level = LogLevel.Info, message = "ImagesController served a request for image details." },
                new { file = "Contoso.RestApi.UserController.cs", line = 84, level = LogLevel.Info, message = "User details retrieved successfully." },
                new { file = "Contoso.RestApi.AuthController.cs", line = 115, level = LogLevel.Info, message = "User authenticated successfully." },
                new { file = "Contoso.RestApi.DataController.cs", line = 263, level = LogLevel.Info, message = "DataController responded with data." },
                new { file = "Contoso.RestApi.PaymentsController.cs", line = 176, level = LogLevel.Info, message = "Payment processed successfully." },
                new { file = "Contoso.RestApi.NotificationsController.cs", line = 223, level = LogLevel.Info, message = "Notification status updated." },
                new { file = "Contoso.RestApi.ReportsController.cs", line = 307, level = LogLevel.Info, message = "Scheduled report executed successfully." },
                new { file = "Contoso.RestApi.ReportsController.cs", line = 328, level = LogLevel.Info, message = "ReportsController processed report download request." },
                new { file = "Contoso.RestApi.ImagesController.cs", line = 580, level = LogLevel.Info, message = "Image upload completed." },
                new { file = "Contoso.RestApi.UserController.cs", line = 101, level = LogLevel.Info, message = "UserController created a new user profile." },
                new { file = "Contoso.RestApi.AuthController.cs", line = 133, level = LogLevel.Info, message = "User session token generated." },
                new { file = "Contoso.RestApi.DataController.cs", line = 283, level = LogLevel.Info, message = "DataController executed query successfully." },
                new { file = "Contoso.RestApi.PaymentsController.cs", line = 195, level = LogLevel.Info, message = "Payment refund issued." },
                new { file = "Contoso.RestApi.LoggingController.cs", line = 211, level = LogLevel.Info, message = "Log entry created successfully." },
                new { file = "Contoso.RestApi.HealthCheckController.cs", line = 55, level = LogLevel.Info, message = "Health check returned healthy status." },
                new { file = "Contoso.RestApi.MonitoringController.cs", line = 89, level = LogLevel.Info, message = "System metrics collected." },

                // Warning Locations
                new { file = "Contoso.RestApi.ImagesController.cs", line = 590, level = LogLevel.Warning, message = "Image request took longer than expected." },
                new { file = "Contoso.RestApi.UserController.cs", line = 120, level = LogLevel.Warning, message = "UserController encountered a delay in response." },
                new { file = "Contoso.RestApi.AuthController.cs", line = 140, level = LogLevel.Warning, message = "Authentication request took too long." },
                new { file = "Contoso.RestApi.MonitoringController.cs", line = 99, level = LogLevel.Warning, message = "Monitoring data collection is slower than usual." },

                // Error Locations
                new { file = "Contoso.RestApi.ImagesController.cs", line = 600, level = LogLevel.Error, message = "Failed to process image upload." },
                new { file = "Contoso.RestApi.UserController.cs", line = 137, level = LogLevel.Error, message = "UserController failed to retrieve user details." },
                new { file = "Contoso.RestApi.PaymentsController.cs", line = 210, level = LogLevel.Error, message = "Payment processing error occurred." },
                new { file = "Contoso.RestApi.DataController.cs", line = 300, level = LogLevel.Error, message = "DataController encountered a database error." },
                new { file = "Contoso.RestApi.AuthController.cs", line = 155, level = LogLevel.Error, message = "User authentication failed due to expired token." },
                new { file = "Contoso.RestApi.ReportsController.cs", line = 340, level = LogLevel.Error, message = "Report generation failed due to missing data." },
                new { file = "Contoso.RestApi.NotificationsController.cs", line = 240, level = LogLevel.Error, message = "Failed to send notification to user." },
                new { file = "Contoso.RestApi.UserController.cs", line = 150, level = LogLevel.Error, message = "Failed to update user profile." },
                new { file = "Contoso.RestApi.ImagesController.cs", line = 620, level = LogLevel.Error, message = "Image delete operation failed." },
                new { file = "Contoso.RestApi.ImagesController.cs", line = 635, level = LogLevel.Error, message = "Error occurred during image processing." },
                new { file = "Contoso.RestApi.HealthCheckController.cs", line = 69, level = LogLevel.Error, message = "Health check reported unhealthy status." },
                new { file = "Contoso.RestApi.LoggingController.cs", line = 230, level = LogLevel.Error, message = "Failed to create log entry." }
            }
        }
            }
            from location in @case.locations
            select new Location(@case.source, location.file, location.line, location.level.ToString().ToLower(), location.message);


        _locations = sources.ToArray();
    }




    [DebuggerStepThrough]
    public static IAsyncLogger Create(
        string topicEndpoint,
        TokenCredential tokenCredential,
        Action<Options> config)
    {
        if (string.IsNullOrWhiteSpace(topicEndpoint))
            throw new ArgumentNullException(nameof(topicEndpoint));


        var client = new EventGridPublisherClient(new Uri(topicEndpoint), tokenCredential);
        return new DummyAsyncLogger(client, config);
    }



    protected override async Task LogAsync(IList<LogEventArgs> args)
    {
        ThrowIf.ArgumentNull(args);
        if (args.Count == 0)
        {
            return;
        }

        Console.WriteLine($"Sending {args.Count}");

        var events = args
            .Select(arg =>
            {
                dynamic content = JObject.Parse(arg.Content);
                var location = IRandom.System.Choice(_locations);
                content.source.name = location.Source;
                content.source.file = location.File;
                content.source.line = location.Line;
                content.level = location.Level;
                content.message = location.Message;
                if (location.Level != "error")
                {
                    content.exception = null;
                }

                content.createdUtc =
                    (DateTime.UtcNow - TimeSpan.FromDays(5 * 30) * IRandom.System.NextDouble()).ToString("O");
                var result = content.ToString();
                return result;
            })
            .Select(content => new CloudEvent(
            "/ap/logs",
            "log",
            new BinaryData(Encoding.UTF8.GetBytes(content)),
            "application/json"));

        try
        {
            // Send the pre-encoded CloudEvents directly to Event Grid
            //await _client.SendEventsAsync(events);
            _client.SendEventsAsync(events).GetAwaiter().GetResult();
        }
        catch (Exception ex)
        {
            Trace.TraceError($"Failed to send log to Event Grid: {ex.Message}");
        }
    }
}