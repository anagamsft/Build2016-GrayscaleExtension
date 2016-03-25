using System;
using Windows.ApplicationModel.AppService;
using Windows.ApplicationModel.Background;
using Windows.Foundation.Collections;

namespace GrayscaleService
{
    public sealed class Service : IBackgroundTask
    {
        private BackgroundTaskDeferral backgroundTaskDeferral;
        private AppServiceConnection appServiceconnection;

        public void Run(IBackgroundTaskInstance taskInstance)
        {
            this.backgroundTaskDeferral = taskInstance.GetDeferral(); // Get a deferral so that the service isn&#39;t terminated.
            taskInstance.Canceled += OnTaskCanceled; // Associate a cancellation handler with the background task.

            // Retrieve the app service connection and set up a listener for incoming app service requests.
            var details = taskInstance.TriggerDetails as AppServiceTriggerDetails;
            appServiceconnection = details.AppServiceConnection;
            appServiceconnection.RequestReceived += OnRequestReceived;
        }

        private async void OnRequestReceived(AppServiceConnection sender, AppServiceRequestReceivedEventArgs args)
        {
            // Get a deferral because we use an awaitable API below to respond to the message
            // and we dont want this call to get cancelled while we are waiting.
            var messageDeferral = args.GetDeferral();

            ValueSet message = args.Request.Message;
            ValueSet returnData = new ValueSet();

            string command = message["Command"] as string;

            switch (command)
            {
                case "Grayscale":
                    {
                        returnData = GrayscaleByteArray(message);   
                        break;
                    }
                // load extension is called
                case "Load":
                    {
                        returnData = GrayscaleByteArray(message);
                        break;
                    }
                default:
                    {
                        break;
                    }
            }

            await args.Request.SendResponseAsync(returnData); // Return the data to the caller.
            messageDeferral.Complete(); // Complete the deferral so that the platform knows that we're done responding to the app service call.
        }

        private ValueSet GrayscaleByteArray(ValueSet message)
        {
            ValueSet returnData = new ValueSet();

            // get byte array in bgra8 format and size of the image
            if (message.ContainsKey("Pixels") &&
                message.ContainsKey("Height") &&
                message.ContainsKey("Width"))
            {
                byte[] pixels = message["Pixels"] as byte[];

                // grayscale the byte array, which is in Bgra8 format
                for (int i = 0; i < pixels.Length; i += 4)
                {
                    int gscale = (pixels[i] + pixels[i + 1] + pixels[i + 2]) / 3;
                    pixels[i] = (byte)gscale;
                    pixels[i + 1] = (byte)gscale;
                    pixels[i + 2] = (byte)gscale;
                }

                // return the modified pixels
                returnData.Add("Pixels", pixels);
                returnData.Add("Height", message["Height"]);
                returnData.Add("Width", message["Width"]);
            }

            return returnData;
        }

        private void OnTaskCanceled(IBackgroundTaskInstance sender, BackgroundTaskCancellationReason reason)
        {
            if (this.backgroundTaskDeferral != null)
            {
                // Complete the service deferral.
                this.backgroundTaskDeferral.Complete();
            }
        }
    }
}
