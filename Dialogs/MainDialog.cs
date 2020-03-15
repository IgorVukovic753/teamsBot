// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using TeamsAuth;
using TeamsAuth.APIHandlers;
using TeamsAuth.Config;
using TeamsAuth.Dialogs;

namespace Microsoft.BotBuilderSamples
{
    public class MainDialog : LogoutDialog
    {
        protected IConfiguration Configuration;
        private IStatePropertyAccessor<AuthenticatedUser> _AuthenticatedUserAccessor;
        private IBotServices _botServices;
        protected readonly Intents Intents;

        public MainDialog(IConfiguration configuration, IStatePropertyAccessor<AuthenticatedUser> AuthenticatedUserAccessor, IBotServices botServices, Intents intents)
             : base(nameof(MainDialog), configuration["ConnectionName"])
        {
            Configuration = configuration;
            _AuthenticatedUserAccessor = AuthenticatedUserAccessor;
            _botServices = botServices;
            Intents = intents;
            

            AddDialog(new OAuthPrompt(
                nameof(OAuthPrompt),
                new OAuthPromptSettings
                {
                    ConnectionName = ConnectionName,
                    Text = "Before we start, I would need from you to verify your account.",
                    Title = "Verify Account",
                    Timeout = 300000, // User has 5 minutes to login (1000 * 60 * 5)
                }));

            //AddDialog(new ConfirmPrompt(nameof(ConfirmPrompt)));

            AddAllPossibleDialogs();

            AddDialog(new WaterfallDialog(nameof(WaterfallDialog), new WaterfallStep[]
            {
                PromptStepAsync,
                LoginStepAsync,
                StartProcessingIntents,
                EndProcessingIntents
            }));

            // The initial child Dialog to run.
            InitialDialogId = nameof(WaterfallDialog);
        }

        private async Task<DialogTurnResult> PromptStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var authenticatedUserIsAuthenticated = await _AuthenticatedUserAccessor.GetAsync(stepContext.Context, () => new AuthenticatedUser());

            if (authenticatedUserIsAuthenticated.IsAuthenticated)
            {
                return await stepContext.ContinueDialogAsync();
            }
            else
            {
                return await stepContext.BeginDialogAsync(nameof(OAuthPrompt), null, cancellationToken);
            }

            //return await stepContext.BeginDialogAsync(nameof(OAuthPrompt), null, cancellationToken);
        }
        private async Task<DialogTurnResult> LoginStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {

            var authenticatedUserIsAuthenticated = await _AuthenticatedUserAccessor.GetAsync(stepContext.Context, () => new AuthenticatedUser());

            if (authenticatedUserIsAuthenticated.IsAuthenticated)
            {
                return await stepContext.ContinueDialogAsync();
            }

            // Get the token from the previous step. Note that we could also have gotten the
            // token directly from the prompt itself. There is an example of this in the next method.
            var tokenResponse = (TokenResponse)stepContext.Result;
            if (tokenResponse?.Token != null)
            {
                AuthenticatedUser AuthenticatedUser = new AuthenticatedUser();
                AuthenticatedUser.IsAuthenticated = true;
                AuthenticatedUser.JwtSecurityToken = tokenResponse.Token;

                await _AuthenticatedUserAccessor.SetAsync(stepContext.Context, AuthenticatedUser);

                // Pull in the data from the Microsoft Graph.
                var client = new SimpleGraphClient(tokenResponse.Token);
                var me = await client.GetMeAsync();
                var title = !string.IsNullOrEmpty(me.JobTitle) ?
                            me.JobTitle : "Unknown";

                await stepContext.Context.SendActivityAsync($"You're logged in as {me.DisplayName} ({me.UserPrincipalName}); you job title is: {title}");

                await stepContext.Context.SendActivityAsync("Your intent please:");

                //return await stepContext.PromptAsync(nameof(ConfirmPrompt), new PromptOptions { Prompt = MessageFactory.Text("Your intent please:") }, cancellationToken);
            }

            //await stepContext.Context.SendActivityAsync(MessageFactory.Text("Login was not successful please try again."), cancellationToken);

            return await stepContext.EndDialogAsync(cancellationToken: cancellationToken);
        }
        private async Task<DialogTurnResult> StartProcessingIntents(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var authenticatedUserIsAuthenticated = await _AuthenticatedUserAccessor.GetAsync(stepContext.Context, () => new AuthenticatedUser());

            // start processing

            // Don't do anything for non-message activities.
            if (stepContext.Context.Activity.Type != ActivityTypes.Message)
            {

            }
            else
            {
                // First, we use the dispatch model to determine which cognitive service (LUIS or QnA) to use.
                RecognizerResult recognizerResult = await _botServices.LuisRecognizer.RecognizeAsync(stepContext.Context, cancellationToken);

                // Top intent tell us which cognitive service to use.
                var topIntent = recognizerResult.GetTopScoringIntent();

                // Next, we call the dispatcher with the top intent.
                 return await DispatchToTopIntentAsync(stepContext, topIntent.intent, recognizerResult, cancellationToken);
            }

            return await stepContext.EndDialogAsync();
        }

        private async Task<DialogTurnResult> EndProcessingIntents(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var authenticatedUserIsAuthenticated = await _AuthenticatedUserAccessor.GetAsync(stepContext.Context, () => new AuthenticatedUser());
            // var apiResults = await _AuthenticatedUserAccessor.GetAsync(stepContext.Context, () => new APIResults());

            if (stepContext.Result == null || stepContext.Result.GetType() != typeof(Intent))
            {
                return await stepContext.EndDialogAsync();
            }

            Intent executingIntent = (Intent)stepContext.Result;

            // now call graph API
            // bur check if token expired
            if (authenticatedUserIsAuthenticated.IsAuthenticated /*&& authenticatedUserIsAuthenticated.Expiration > DateTime.UtcNow*/)
            {
                executingIntent.Offset = stepContext.Context.Activity.LocalTimestamp.Value.Offset;
                Type T = System.Reflection.Assembly.GetExecutingAssembly().GetType(executingIntent.APIEndpointHandler);
                APIHandler apiHandler = Activator.CreateInstance(T, new object[] { authenticatedUserIsAuthenticated.JwtSecurityToken }) as APIHandler;
                APIResult result = await apiHandler.ExecuteAPI(executingIntent);

                // await stepContext.Context.SendActivityAsync(MessageFactory.Text(string.Format(executingIntent.ConfirmationText, executingIntent.RequiredEntities.Select(entity => entity.Value).ToArray())), cancellationToken);

                if (result.Code == APIResultCode.Ok)
                {
                    if (authenticatedUserIsAuthenticated.APIResults == null)
                    {
                        authenticatedUserIsAuthenticated.APIResults = new APIResults();
                    }

                    authenticatedUserIsAuthenticated.APIResults.Add(result);

                    await stepContext.Context.SendActivityAsync(MessageFactory.Text(result.ResultText), cancellationToken);
                }
                else
                {
                    await stepContext.Context.SendActivityAsync(MessageFactory.Text(result.ErrorText), cancellationToken);
                    //Logger.LogInformation(result.ErrorText);
                }

                await stepContext.Context.SendActivityAsync(MessageFactory.Text("You can make your other request"), cancellationToken);
            }
            else
            {

                // log in again 

                authenticatedUserIsAuthenticated.IsAuthenticated = false;

                await _AuthenticatedUserAccessor.SetAsync(stepContext.Context, authenticatedUserIsAuthenticated, cancellationToken);

                await stepContext.Context.SendActivityAsync(MessageFactory.Text($"Session expired. Please, login again."), cancellationToken);

                await stepContext.EndDialogAsync();
                return await stepContext.BeginDialogAsync(this.InitialDialogId);
                // return await PromptStepAsync(stepContext, cancellationToken);
            }

            return await stepContext.EndDialogAsync();
        }



        private async Task<DialogTurnResult> DispatchToTopIntentAsync(WaterfallStepContext stepContext, string intent, RecognizerResult recognizerResult, CancellationToken cancellationToken)
        {
            // find proper Dialog for that intent

            Intent foundIntent = Intents.AllIntents.Find(k => k.IntentName.Equals(intent));

            if (foundIntent != null)
            {
                return await stepContext.BeginDialogAsync(foundIntent.IntentName, recognizerResult.Entities, cancellationToken);
            }
            else
            {
                await stepContext.Context.SendActivityAsync(MessageFactory.Text($"Sorry. Could not understand your request."), cancellationToken);

                return await stepContext.ContinueDialogAsync();
            }
        }
        private void AddAllPossibleDialogs()
        {
            List<string> dialogIds = new List<string>();
            foreach (Intent foundIntent in Intents.AllIntents)
            {
                AddDialog(new IntentProcessingDialog(foundIntent.IntentName, foundIntent, _AuthenticatedUserAccessor));

                foreach (RequiredEntity entity in foundIntent.RequiredEntities)
                {
                    if (!dialogIds.Contains(entity.Name))
                    {
                        dialogIds.Add(entity.Name);
                        AddDialog(new TextPrompt(entity.Name));
                    }
                }
            }
        }
    }
}
