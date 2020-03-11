// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using TeamsAuth;

namespace Microsoft.BotBuilderSamples
{
    // This bot is derived (view DialogBot<T>) from the TeamsACtivityHandler class currently included as part of this sample.

    public class TeamsBot : DialogBot//<T> where T : Dialog
    {
        public TeamsBot(IBotServices botServices, ConversationState conversationState, UserState userState, IConfiguration configuration)
            : base(botServices, conversationState, userState, configuration)
        {
        }

        protected override async Task OnMembersAddedAsync(IList<ChannelAccount> membersAdded, ITurnContext<IConversationUpdateActivity> turnContext, CancellationToken cancellationToken)
        {
            foreach (var member in turnContext.Activity.MembersAdded)
            {
                if (member.Id != turnContext.Activity.Recipient.Id)
                {
                    await turnContext.SendActivityAsync(MessageFactory.Text("Welcome to AuthenticationBot. Type anything to get logged in. Type 'logout' to sign-out."), cancellationToken);
                }
            }
        }

        protected override async Task OnTeamsSigninVerifyStateAsync(ITurnContext<IInvokeActivity> turnContext, CancellationToken cancellationToken)
        {
            Logger.LogInformation("Running dialog with signin/verifystate from an Invoke Activity.");

            // The OAuth Prompt needs to see the Invoke Activity in order to complete the login process.

            // Run the Dialog with the new Invoke Activity.
            await Dialog.RunAsync(turnContext, ConversationState.CreateProperty<DialogState>(nameof(DialogState)), cancellationToken);
        }
    }
}
