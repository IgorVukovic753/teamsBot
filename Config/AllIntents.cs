using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace TeamsAuth.Config
{
    public class RequiredEntity
    {
        public RequiredEntity(string name, string prompt = null, string retryPrompt = null)
            : this(name, new PromptOptions
            {
                Prompt = MessageFactory.Text(prompt),
                RetryPrompt = MessageFactory.Text(retryPrompt),
            })
        {
        }

        public RequiredEntity(string name, PromptOptions options)
        {
            Name = name;

            Options = options;
        }
        public RequiredEntity()
        {
            //Options = new PromptOptions
            //{
            //    Prompt = MessageFactory.Text(Prompt),
            //    RetryPrompt = MessageFactory.Text(RetryPrompt),
            //};
        }
        public int Order { get; set; }
        public string Name { get; set; }
        public string Prompt { get; set; }
        public string RetryPrompt { get; set; }
        public bool Mandatory { get; set; }
        public object Value { get; set; }
        public string ValueStr
        {
            get
            {
                if (Value != null)
                    return Value.ToString();
                return "";
            }
        }
        public PromptOptions Options { get; set; }
    }

    public class Intent
    {
        public string IntentName { get; set; }
        //public string DialogHandler { get; set; }
        public string APIEndpointHandler { get; set; }
        public string ConfirmationText { get; set; }
        public string ProcessLogicFunction { get; set; }

        public List<APIParameter> APIParameters { get; set; }
        public List<RequiredEntity> RequiredEntities { get; set; }
        public TimeSpan Offset { get; set; }
    }

    public class APIParameter
    {
        public string Name { get; set; }
        public string Value { get; set; }
    }
    public class Intents
    {
        public List<Intent> AllIntents { get; set; }
    }
}
