using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using TeamsAuth.Config;

namespace TeamsAuth.Dialogs
{
    public class IntentProcessingDialog : Dialog
    {
        private Intent Intent;
        private IStatePropertyAccessor<AuthenticatedUser> _bagAccessor;
        private const string PersistedValues = "values";
        private const string SlotName = "slot";

        public IntentProcessingDialog(string dialogId, Intent intent, IStatePropertyAccessor<AuthenticatedUser> bagAccessor)
            : base(dialogId)
        {
            Intent = intent;
            _bagAccessor = bagAccessor;
        }

        public override async Task<DialogTurnResult> BeginDialogAsync(DialogContext dialogContext, object options = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (dialogContext == null)
            {
                throw new ArgumentNullException(nameof(dialogContext));
            }

            // Don't do anything for non-message activities.
            if (dialogContext.Context.Activity.Type != ActivityTypes.Message)
            {
                return await dialogContext.EndDialogAsync(new Dictionary<string, object>());
            }

            if (options != null)
            {
                Newtonsoft.Json.Linq.JObject entities = (Newtonsoft.Json.Linq.JObject)options;

                foreach (RequiredEntity entity1 in Intent.RequiredEntities)
                {
                    JToken token = entities[entity1.Name.Replace(".", "_")];
                    if (token != null)
                    {
                        entity1.Value = token[0].ToString();
                    }
                }
            }

            SetPersistedValue(dialogContext.ActiveDialog, Intent);

            // Run prompt
            return await RunPromptAsync(dialogContext, cancellationToken);
        }

        private static void SetPersistedValue(DialogInstance dialogInstance, Intent intent)
        {
            object obj;

            if (dialogInstance.State.TryGetValue(PersistedValues, out obj))
            {
                dialogInstance.State[PersistedValues] = intent;
            }
            else
            {
                dialogInstance.State.Add(PersistedValues, intent);
            }
        }

        private static Intent GetPersistedValues(DialogInstance dialogInstance)
        {
            object obj;
            if (dialogInstance.State.TryGetValue(PersistedValues, out obj))
            {
                return (Intent)obj;
            }

            return null;
        }

        private Task<DialogTurnResult> RunPromptAsync(DialogContext dialogContext, CancellationToken cancellationToken)
        {
            Intent state = GetPersistedValues(dialogContext.ActiveDialog);

            //// Run through the list  until we find one that hasn't been filled yet.
            ///

            MethodInfo method = this.GetType().GetMethods().ToList().Find(k => k.Name.Equals(Intent.ProcessLogicFunction));

            return (Task<DialogTurnResult>)method.Invoke(this, new object[] { state, dialogContext, cancellationToken });

            // return ProcessLogicCalendarCreateCalendarEntry(state, dialogContext, cancellationToken);

        }

        public Task<DialogTurnResult> ProcessLogic_CalendarFindCalendarEntry(Intent intent, DialogContext dialogContext, CancellationToken cancellationToken)
        {
            var unfilledEntity = intent.RequiredEntities.Find(k => k.Value == null && k.Mandatory == true);

            if (unfilledEntity != null)
            {
                dialogContext.ActiveDialog.State[SlotName] = unfilledEntity.Name;
                return dialogContext.BeginDialogAsync(unfilledEntity.Name, new PromptOptions() { Prompt = MessageFactory.Text(unfilledEntity.Prompt) }, cancellationToken);
            }
            else
            {

                return dialogContext.EndDialogAsync(intent);
            }
        }
    }
}
