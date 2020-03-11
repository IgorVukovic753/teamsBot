// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Teams;
using Microsoft.Bot.Schema;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using TeamsAuth;
using TeamsAuth.Config;

namespace Microsoft.BotBuilderSamples
{
    // This IBot implementation can run any type of Dialog. The use of type parameterization is to allows multiple different bots
    // to be run at different endpoints within the same project. This can be achieved by defining distinct Controller types
    // each with dependency on distinct IBot types, this way ASP Dependency Injection can glue everything together without ambiguity.
    // The ConversationState is used by the Dialog system. The UserState isn't, however, it might have been used in a Dialog implementation,
    // and the requirement is that all BotState objects are saved at the end of a turn.
    public class DialogBot : TeamsActivityHandler // where T : Dialog
    {
        protected readonly BotState ConversationState;
        protected readonly Dialog Dialog;
        protected readonly ILogger Logger;
        protected IConfiguration Configuration;
        protected readonly BotState UserState;
        private IBotServices _botServices;

        protected readonly Intents Intents;
        private readonly IStatePropertyAccessor<AuthenticatedUser> _AuthenticatedUserAccessor;

        public DialogBot(IBotServices botServices, ConversationState conversationState, UserState userState, IConfiguration configuration)
        {
            ConversationState = conversationState;
            UserState = userState;
            //Dialog = dialog;
            //Logger = logger;
            _botServices = botServices;
            Configuration = configuration;
            _AuthenticatedUserAccessor = UserState.CreateProperty<AuthenticatedUser>(nameof(AuthenticatedUser));
            Intents = Configuration.Get<Intents>();
            this.Dialog = new MainDialog(configuration, _AuthenticatedUserAccessor, _botServices, Intents);
        }

        public override async Task OnTurnAsync(ITurnContext turnContext, CancellationToken cancellationToken = default(CancellationToken))
        {
            await base.OnTurnAsync(turnContext, cancellationToken);

            if (turnContext.Activity.Type == ActivityTypes.Message)
            {
                var authenticatedUserIsAuthenticated = await _AuthenticatedUserAccessor.GetAsync(turnContext, () => new AuthenticatedUser());
                await Dialog.RunAsync(turnContext, ConversationState.CreateProperty<DialogState>(nameof(DialogState)), cancellationToken);
            }
            else
            {
                if (turnContext.Activity.Type == ActivityTypes.ConversationUpdate && turnContext.Activity.MembersAdded.Any(x => x.Id == turnContext.Activity.Recipient.Id))
                {
                    await turnContext.SendActivityAsync(MessageFactory.Text("Hello! I'm Amy, your virtual productivity assistant. :D I'm here to help you keep up with your calendar activities and get in touch with your colleagues more easily."), cancellationToken);
                    var authenticatedUserIsAuthenticated = await _AuthenticatedUserAccessor.GetAsync(turnContext, () => new AuthenticatedUser());
                    await Dialog.RunAsync(turnContext, ConversationState.CreateProperty<DialogState>(nameof(DialogState)), cancellationToken);
                }
            }

            // Save any state changes that might have occured during the turn.
            await ConversationState.SaveChangesAsync(turnContext, false, cancellationToken);
            await UserState.SaveChangesAsync(turnContext, false, cancellationToken);
        }

        protected override async Task OnMessageActivityAsync(ITurnContext<IMessageActivity> turnContext, CancellationToken cancellationToken)
        {
            //Logger.LogInformation("Running dialog with Message Activity.");

            // Run the Dialog with the new message Activity.
            await Dialog.RunAsync(turnContext, ConversationState.CreateProperty<DialogState>(nameof(DialogState)), cancellationToken);
        }
    }
}
