using Microsoft.Bot.Builder.AI.Luis;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace TeamsAuth
{
    public class BotServices : IBotServices
    {
        public BotServices(IConfiguration configuration)
        {
            // Read the setting for cognitive services (LUIS, QnA) from the appsettings.json
            LuisRecognizer = new LuisRecognizer(new LuisApplication(
                configuration["LuisAppId"],
                configuration["LuisAPIKey"],
                $"https://{configuration["LuisAPIHostName"]}.api.cognitive.microsoft.com"),
                new LuisPredictionOptions { IncludeAllIntents = true, IncludeInstanceData = true },
                true);

            //SampleQnA = new QnAMaker(new QnAMakerEndpoint
            //{
            //    KnowledgeBaseId = configuration["QnAKnowledgebaseId"],
            //    EndpointKey = configuration["QnAEndpointKey"],
            //    Host = configuration["QnAEndpointHostName"]
            //});
        }

        public LuisRecognizer LuisRecognizer { get; private set; }
    }
}
