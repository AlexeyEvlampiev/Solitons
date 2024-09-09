
using Azure.Identity;
using Solitons.Azure.Diagnostics;

namespace PanelReview;

internal class Class1
{
    async Task Demo()
    {
        var logger = EventGridAsyncLogger
            .Create(
                "[topic endpoint]",
                new DefaultAzureCredential(), options => options
                    .BufferWarnings(TimeSpan.FromMicroseconds(300), 100)
                    .BufferInfo(TimeSpan.FromMicroseconds(1000), 500));



        await logger
            .ErrorAsync("Demo message");
    }
}