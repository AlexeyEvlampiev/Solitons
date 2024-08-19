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
using System.Data;
using System.Security;

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
            .Range(0, 10000000)
            .SelectMany(_ => logger
                .ErrorAsync(new OutOfMemoryException("Oops..."))
                .ToObservable());

        Console.WriteLine("Sent");
        await Task.Delay(TimeSpan.FromHours(1));
    }
}


public sealed class DummyAsyncLogger : BufferedAsyncLogger
{
    record Location(string Source, string File, int Line, string Level, string Message, string? Exception);

    record xwz
    {

        public string file { get; init; }
        public int line { get; init; }
        public LogLevel level { get; init; }
        public string message { get; init; }
        public Exception? exception { get; init; }

    }

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
            locations = new xwz[]
            {
                new xwz { file = "Contoso.ImageProcessors.ImageLoader.cs", line = 124, level = LogLevel.Info, message = "Image loaded successfully." },
                new xwz { file = "Contoso.ImageProcessors.ImageSaver.cs", line = 87, level = LogLevel.Info, message = "Image saved to disk." },
                new xwz { file = "Contoso.ImageProcessors.ImageResizer.cs", line = 156, level = LogLevel.Info, message = "Image resized to new dimensions." },
                new xwz { file = "Contoso.ImageProcessors.ImageFilter.cs", line = 209, level = LogLevel.Info, message = "Applied filter to image." },
                new xwz { file = "Contoso.ImageProcessors.ImageCropper.cs", line = 45, level = LogLevel.Info, message = "Image cropped to specified area." },
                new xwz { file = "Contoso.ImageProcessors.ImageCompressor.cs", line = 312, level = LogLevel.Info, message = "Image compressed to reduce file size." },
                new xwz { file = "Contoso.ImageProcessors.ImageConverter.cs", line = 67, level = LogLevel.Info, message = "Image converted to desired format." },
                new xwz { file = "Contoso.ImageProcessors.ImageLoader.cs", line = 142, level = LogLevel.Info, message = "Loaded image metadata." },
                new xwz { file = "Contoso.ImageProcessors.ImageSaver.cs", line = 104, level = LogLevel.Info, message = "Saved image with updated metadata." },
                new xwz { file = "Contoso.ImageProcessors.ImageResizer.cs", line = 186, level = LogLevel.Info, message = "Resized image for thumbnail creation." },
                new xwz { file = "Contoso.ImageProcessors.ImageFilter.cs", line = 239, level = LogLevel.Info, message = "Applied color correction filter." },
                new xwz { file = "Contoso.ImageProcessors.ImageCropper.cs", line = 65, level = LogLevel.Info, message = "Cropped image to aspect ratio." },
                new xwz { file = "Contoso.ImageProcessors.ImageCompressor.cs", line = 332, level = LogLevel.Info, message = "Compressed image for web optimization." },
                new xwz { file = "Contoso.ImageProcessors.ImageConverter.cs", line = 88, level = LogLevel.Info, message = "Converted image from PNG to JPEG." },
                new xwz { file = "Contoso.ImageProcessors.ImageProcessorBase.cs", line = 23, level = LogLevel.Info, message = "Initialized image processing base." },
                new xwz { file = "Contoso.ImageProcessors.ImageProcessorBase.cs", line = 45, level = LogLevel.Info, message = "Base class configured for image processing." },
                new xwz { file = "Contoso.ImageProcessors.ImageLoader.cs", line = 168, level = LogLevel.Info, message = "Image loaded for editing." },
                new xwz { file = "Contoso.ImageProcessors.ImageSaver.cs", line = 130, level = LogLevel.Info, message = "Final image saved after processing." },
                new xwz { file = "Contoso.ImageProcessors.ImageResizer.cs", line = 215, level = LogLevel.Info, message = "Resized image for high-resolution display." },
                new xwz { file = "Contoso.ImageProcessors.ImageFilter.cs", line = 273, level = LogLevel.Info, message = "Applied vintage filter to image." },
                new xwz { file = "Contoso.ImageProcessors.ImageEditor.cs", line = 184, level = LogLevel.Info, message = "Opened image in editor." },
                new xwz { file = "Contoso.ImageProcessors.ImageMetadataReader.cs", line = 78, level = LogLevel.Info, message = "Read image EXIF data." },
                new xwz { file = "Contoso.ImageProcessors.ImageOptimizer.cs", line = 120, level = LogLevel.Info, message = "Optimized image for faster load times." },
                new xwz { file = "Contoso.ImageProcessors.ImagePipeline.cs", line = 243, level = LogLevel.Info, message = "Image processing pipeline executed successfully." },

                // Warning Locations
                new xwz { file = "Contoso.ImageProcessors.ImageResizer.cs", line = 203, level = LogLevel.Warning, message = "Image resize operation took longer than expected." },
                new xwz { file = "Contoso.ImageProcessors.ImageLoader.cs", line = 151, level = LogLevel.Warning, message = "Potential memory issue detected while loading image." },
                new xwz { file = "Contoso.ImageProcessors.ImageSaver.cs", line = 114, level = LogLevel.Warning, message = "Disk space running low during image save operation." },
                new xwz { file = "Contoso.ImageProcessors.ImageCropper.cs", line = 72, level = LogLevel.Warning, message = "Image crop dimensions are outside the expected range." },
                new xwz { file = "Contoso.ImageProcessors.ImageCompressor.cs", line = 340, level = LogLevel.Warning, message = "Image compression resulted in quality loss." },
                new xwz { file = "Contoso.ImageProcessors.ImageOptimizer.cs", line = 132, level = LogLevel.Warning, message = "Optimization skipped due to file corruption." },
                new xwz { file = "Contoso.ImageProcessors.ImagePipeline.cs", line = 267, level = LogLevel.Warning, message = "Processing pipeline encountered an unexpected delay." },

                // Error Locations
                new xwz { file = "Contoso.ImageProcessors.ImageResizer.cs", line = 220, level = LogLevel.Error, message = "Failed to resize image due to unsupported format.", exception = new NotSupportedException("The image format is not supported.") },
                new xwz { file = "Contoso.ImageProcessors.ImageLoader.cs", line = 180, level = LogLevel.Error, message = "Image failed to load due to insufficient memory.", exception = new OutOfMemoryException("Insufficient memory to load the image.") },
                new xwz { file = "Contoso.ImageProcessors.ImageSaver.cs", line = 142, level = LogLevel.Error, message = "Error saving image to disk.", exception = new IOException("Failed to save the image to disk.") },
                new xwz { file = "Contoso.ImageProcessors.ImageMetadataReader.cs", line = 99, level = LogLevel.Error, message = "Error reading image metadata.", exception = new InvalidDataException("Failed to read image metadata.") },
                new xwz { file = "Contoso.ImageProcessors.ImagePipeline.cs", line = 289, level = LogLevel.Error, message = "Image processing pipeline failed to complete.", exception = new InvalidOperationException("Pipeline execution failed.") }
            }
        },
        new
        {
            source = "UserPortal",
            locations = new[]
            {
                new xwz { file = "Contoso.UserPortal.HomePage.cs", line = 342, level = LogLevel.Info, message = "Home page loaded successfully." },
                new xwz { file = "Contoso.UserPortal.LoginPage.cs", line = 102, level = LogLevel.Info, message = "User login form displayed." },
                new xwz { file = "Contoso.UserPortal.ProfilePage.cs", line = 88, level = LogLevel.Info, message = "User profile page displayed." },
                new xwz { file = "Contoso.UserPortal.Dashboard.cs", line = 278, level = LogLevel.Info, message = "Dashboard data loaded for user." },
                new xwz { file = "Contoso.UserPortal.SettingsPage.cs", line = 134, level = LogLevel.Info, message = "User settings loaded." },
                new xwz { file = "Contoso.UserPortal.Notifications.cs", line = 189, level = LogLevel.Info, message = "User notifications retrieved." },
                new xwz { file = "Contoso.UserPortal.UserController.cs", line = 41, level = LogLevel.Info, message = "UserController initialized." },
                new xwz { file = "Contoso.UserPortal.HomePage.cs", line = 360, level = LogLevel.Info, message = "Home page refreshed successfully." },
                new xwz { file = "Contoso.UserPortal.LoginPage.cs", line = 118, level = LogLevel.Info, message = "Login attempt initiated." },
                new xwz { file = "Contoso.UserPortal.ProfilePage.cs", line = 105, level = LogLevel.Info, message = "User profile updated." },
                new xwz { file = "Contoso.UserPortal.Dashboard.cs", line = 296, level = LogLevel.Info, message = "Dashboard refreshed with latest data." },
                new xwz { file = "Contoso.UserPortal.SettingsPage.cs", line = 152, level = LogLevel.Info, message = "User settings saved." },
                new xwz { file = "Contoso.UserPortal.Notifications.cs", line = 207, level = LogLevel.Info, message = "Notifications marked as read." },
                new xwz { file = "Contoso.UserPortal.UserController.cs", line = 56, level = LogLevel.Info, message = "User data loaded into controller." },
                new xwz { file = "Contoso.UserPortal.UserController.cs", line = 68, level = LogLevel.Info, message = "User session validated." },
                new xwz { file = "Contoso.UserPortal.UserProfile.cs", line = 49, level = LogLevel.Info, message = "User profile data retrieved." },
                new xwz { file = "Contoso.UserPortal.SettingsPage.cs", line = 162, level = LogLevel.Info, message = "Loaded settings for user preferences." },
                new xwz { file = "Contoso.UserPortal.Notifications.cs", line = 218, level = LogLevel.Info, message = "New notifications fetched from server." },

                // Warning Locations
                new xwz { file = "Contoso.UserPortal.Dashboard.cs", line = 286, level = LogLevel.Warning, message = "Dashboard load time exceeded threshold." },
                new xwz { file = "Contoso.UserPortal.HomePage.cs", line = 365, level = LogLevel.Warning, message = "Home page encountered a slow network response." },
                new xwz { file = "Contoso.UserPortal.ProfilePage.cs", line = 115, level = LogLevel.Warning, message = "Profile update partially completed." },
                new xwz { file = "Contoso.UserPortal.SettingsPage.cs", line = 167, level = LogLevel.Warning, message = "Settings save operation took longer than expected." },
                new xwz { file = "Contoso.UserPortal.Notifications.cs", line = 221, level = LogLevel.Warning, message = "Notification fetch request timed out." },
                new xwz { file = "Contoso.UserPortal.UserProfile.cs", line = 60, level = LogLevel.Warning, message = "User profile data load was incomplete." },

                // Error Locations
                new xwz { file = "Contoso.UserPortal.Dashboard.cs", line = 310, level = LogLevel.Error, message = "Failed to load dashboard data for user.", exception = new DataException("Dashboard data could not be loaded.") },
                new xwz { file = "Contoso.UserPortal.LoginPage.cs", line = 137, level = LogLevel.Error, message = "User login failed due to invalid credentials.", exception = new UnauthorizedAccessException("Invalid user credentials provided.") },
                new xwz { file = "Contoso.UserPortal.UserController.cs", line = 72, level = LogLevel.Error, message = "User session could not be created.", exception = new InvalidOperationException("Unable to create a user session.") },
                new xwz { file = "Contoso.UserPortal.SettingsPage.cs", line = 175, level = LogLevel.Error, message = "Error saving user settings.", exception = new IOException("Failed to save user settings.") }
            }
        },
        new
        {
            source = "RestApi",
            locations = new[]
            {
                new xwz { file = "Contoso.RestApi.ImagesController.cs", line = 543, level = LogLevel.Info, message = "ImagesController received a request for image list." },
                new xwz { file = "Contoso.RestApi.UserController.cs", line = 65, level = LogLevel.Info, message = "UserController initialized." },
                new xwz { file = "Contoso.RestApi.AuthController.cs", line = 91, level = LogLevel.Info, message = "Authentication request received." },
                new xwz { file = "Contoso.RestApi.DataController.cs", line = 237, level = LogLevel.Info, message = "Data request processed successfully." },
                new xwz { file = "Contoso.RestApi.PaymentsController.cs", line = 147, level = LogLevel.Info, message = "Payment transaction started." },
                new xwz { file = "Contoso.RestApi.NotificationsController.cs", line = 198, level = LogLevel.Info, message = "Notifications sent to user." },
                new xwz { file = "Contoso.RestApi.ReportsController.cs", line = 284, level = LogLevel.Info, message = "Report generated successfully." },
                new xwz { file = "Contoso.RestApi.ImagesController.cs", line = 563, level = LogLevel.Info, message = "ImagesController served a request for image details." },
                new xwz { file = "Contoso.RestApi.UserController.cs", line = 84, level = LogLevel.Info, message = "User details retrieved successfully." },
                new xwz { file = "Contoso.RestApi.AuthController.cs", line = 115, level = LogLevel.Info, message = "User authenticated successfully." },
                new xwz { file = "Contoso.RestApi.DataController.cs", line = 263, level = LogLevel.Info, message = "DataController responded with data." },
                new xwz { file = "Contoso.RestApi.PaymentsController.cs", line = 176, level = LogLevel.Info, message = "Payment processed successfully." },
                new xwz { file = "Contoso.RestApi.NotificationsController.cs", line = 223, level = LogLevel.Info, message = "Notification status updated." },
                new xwz { file = "Contoso.RestApi.ReportsController.cs", line = 307, level = LogLevel.Info, message = "Scheduled report executed successfully." },
                new xwz { file = "Contoso.RestApi.ReportsController.cs", line = 328, level = LogLevel.Info, message = "ReportsController processed report download request." },
                new xwz { file = "Contoso.RestApi.ImagesController.cs", line = 580, level = LogLevel.Info, message = "Image upload completed." },
                new xwz { file = "Contoso.RestApi.UserController.cs", line = 101, level = LogLevel.Info, message = "UserController created a new user profile." },
                new xwz { file = "Contoso.RestApi.AuthController.cs", line = 133, level = LogLevel.Info, message = "User session token generated." },
                new xwz { file = "Contoso.RestApi.DataController.cs", line = 283, level = LogLevel.Info, message = "DataController executed query successfully." },
                new xwz { file = "Contoso.RestApi.PaymentsController.cs", line = 195, level = LogLevel.Info, message = "Payment refund issued." },
                new xwz { file = "Contoso.RestApi.LoggingController.cs", line = 211, level = LogLevel.Info, message = "Log entry created successfully." },
                new xwz { file = "Contoso.RestApi.HealthCheckController.cs", line = 55, level = LogLevel.Info, message = "Health check returned healthy status." },
                new xwz { file = "Contoso.RestApi.MonitoringController.cs", line = 89, level = LogLevel.Info, message = "System metrics collected." },

                // Warning Locations
                new xwz { file = "Contoso.RestApi.ImagesController.cs", line = 590, level = LogLevel.Warning, message = "Image request took longer than expected." },
                new xwz { file = "Contoso.RestApi.UserController.cs", line = 120, level = LogLevel.Warning, message = "UserController encountered a delay in response." },
                new xwz { file = "Contoso.RestApi.AuthController.cs", line = 140, level = LogLevel.Warning, message = "Authentication request took too long." },
                new xwz { file = "Contoso.RestApi.MonitoringController.cs", line = 99, level = LogLevel.Warning, message = "Monitoring data collection is slower than usual." },

                // Error Locations
                new xwz { file = "Contoso.RestApi.ImagesController.cs", line = 600, level = LogLevel.Error, message = "Failed to process image upload.", exception = new IOException("Image upload processing failed.") },
                new xwz { file = "Contoso.RestApi.UserController.cs", line = 137, level = LogLevel.Error, message = "UserController failed to retrieve user details.", exception = new DataException("Failed to retrieve user details.") },
                new xwz { file = "Contoso.RestApi.PaymentsController.cs", line = 210, level = LogLevel.Error, message = "Payment processing error occurred.", exception = new InvalidOperationException("Payment processing failed.") },
                new xwz { file = "Contoso.RestApi.DataController.cs", line = 300, level = LogLevel.Error, message = "DataController encountered a database error.", exception = new InvalidOperationException("A database error occurred during data retrieval.") },
                new xwz { file = "Contoso.RestApi.AuthController.cs", line = 155, level = LogLevel.Error, message = "User authentication failed due to expired token.", exception = new SecurityException("The provided security token has expired.") },
                new xwz { file = "Contoso.RestApi.ReportsController.cs", line = 340, level = LogLevel.Error, message = "Report generation failed due to missing data.", exception = new InvalidOperationException("Required data for report generation is missing.") },
                new xwz { file = "Contoso.RestApi.NotificationsController.cs", line = 240, level = LogLevel.Error, message = "Failed to send notification to user.", exception = new InvalidOperationException("Failed to send notification to the user.") },
                new xwz { file = "Contoso.RestApi.UserController.cs", line = 150, level = LogLevel.Error, message = "Failed to update user profile.", exception = new InvalidOperationException("Failed to update the user profile.") },
                new xwz { file = "Contoso.RestApi.ImagesController.cs", line = 620, level = LogLevel.Error, message = "Image delete operation failed.", exception = new IOException("Image deletion failed.") },
                new xwz { file = "Contoso.RestApi.ImagesController.cs", line = 635, level = LogLevel.Error, message = "Error occurred during image processing.", exception = new InvalidOperationException("An error occurred during image processing.") },
                new xwz { file = "Contoso.RestApi.HealthCheckController.cs", line = 69, level = LogLevel.Error, message = "Health check reported unhealthy status.", exception = new InvalidOperationException("The system health check reported an unhealthy status.") },
                new xwz { file = "Contoso.RestApi.LoggingController.cs", line = 230, level = LogLevel.Error, message = "Failed to create log entry.", exception = new InvalidOperationException("Failed to create a log entry.") }
            }
        }
    }
    from location in @case.locations
    select new Location(@case.source, location.file, location.line, location.level.ToString().ToLower(), location.message, location.exception?.GetType().Name);


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