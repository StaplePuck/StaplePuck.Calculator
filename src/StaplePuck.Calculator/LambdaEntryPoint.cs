using Amazon.Lambda.Core;
using Amazon.Lambda.SNSEvents;
using Amazon.Lambda.SQSEvents;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]
namespace StaplePuck.Calculator
{
    public class LambdaEntryPoint
    {
        public LambdaEntryPoint()
        {
        }

        public async Task ProcessRequest(LeagueRequest request, ILambdaContext context)
        {
            await Updater.UpdateLeague(request);
        }

        public async Task HandlerSNSEvent(SNSEvent evnt, ILambdaContext context)
        {
            foreach (var message in evnt.Records)
            {
                await ProcessMessageAsync(message, context);
            }
        }

        private async Task ProcessMessageAsync(SNSEvent.SNSRecord message, ILambdaContext context)
        {
            var body = message.Sns.Message;
            context.Logger.LogLine($"Processed message {body}");

            try
            {
                var request = JsonConvert.DeserializeObject<LeagueRequest>(body);
                if (request == null)
                {
                    throw new Exception($"Failed to parse request");
                }
                await ProcessRequest(request, context);
            }
            catch (Exception e)
            {
                context.Logger.LogLine($"Failed to process message: {body}");
                context.Logger.LogLine($"ERROR: {e.Message}. {e.StackTrace}");
            }
        }

        public async Task HandleSQSEvent(SQSEvent evnt, ILambdaContext context)
        {
            foreach (var message in evnt.Records)
            {
                await ProcessMessageAsync(message, context);
            }
        }

        private async Task ProcessMessageAsync(SQSEvent.SQSMessage message, ILambdaContext context)
        {
            context.Logger.LogLine($"Processed message {message.Body}");
            
            try
            {
                var request = JsonConvert.DeserializeObject<LeagueRequest>(message.Body);
                if (request == null)
                {
                    throw new Exception($"Failed to parse request");
                }
                await ProcessRequest(request, context);
            }
            catch (Exception e)
            {
                context.Logger.LogLine($"Failed to process message: {message.Body}");
                context.Logger.LogLine($"ERROR: {e.Message}. {e.StackTrace}");
            }
        }
    }
}
