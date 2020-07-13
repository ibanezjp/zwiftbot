using System;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Dialogs.Choices;
using Newtonsoft.Json;

namespace CoreBot.Dialogs.BaseDialogs
{
    public abstract class BaseManualDialog : ComponentDialog
    {
        protected BaseManualDialog(string dialogId) : base(dialogId)
        {
            {
                // This array defines how the Waterfall will execute.
                var waterfallInitSteps = new WaterfallStep[]
                {
                    SelectStepAsync,
                    ProcessStepAsync
                };

                // Add named dialogs to the DialogSet. These names are saved in the dialog state.
                AddDialog(new WaterfallDialog($"{nameof(WaterfallDialog)}", waterfallInitSteps));
                AddDialog(new ChoicePrompt(nameof(ChoicePrompt), Validator));
            }
        }

        private async Task<bool> Validator(PromptValidatorContext<FoundChoice> promptContext, CancellationToken cancellationToken)
        {
            if (promptContext.Recognized.Succeeded)
                return true;

            if (promptContext.AttemptCount <= 3) 
                return false;

            await promptContext.Context.SendActivityAsync("No elegiste ninguna opción correcta!", cancellationToken: cancellationToken);
            promptContext.Recognized.Value = new FoundChoice { Value = "CANCEL" };
            return true;
        }

        private async Task<DialogTurnResult> SelectStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            User user;

            using (var httpClient = new HttpClient())
            {
                var data = await httpClient.GetStringAsync($"https://zwiftapi.azurewebsites.net/api/GetUser/{stepContext.Options}");
                user = JsonConvert.DeserializeObject<User>(data);
            }

            stepContext.Values["user"] = user;

            return await stepContext.PromptAsync(nameof(ChoicePrompt),
                new PromptOptions
                {
                    Prompt = MessageFactory.Text(GetSelectMessage()),
                    Choices = ChoiceFactory.ToChoices(GetRoutes(user)
                        //.Where(x => x.HasAward && !x.EventOnly && (x.AllowedSports & Sports.Cycling) == Sports.Cycling)
                        .Where(x => (x.AllowedSports & Sports.Cycling) == Sports.Cycling)
                        .Select(x => x.Name).ToList()),
                    Style = ListStyle.List
                }, cancellationToken);
        }

        private async Task<DialogTurnResult> ProcessStepAsync(WaterfallStepContext stepContext,
            CancellationToken cancellationToken)
        {
            var selectedRoute = ((FoundChoice)stepContext.Result).Value;

            if(selectedRoute.Equals("CANCEL",StringComparison.InvariantCultureIgnoreCase))
                return await stepContext.EndDialogAsync(cancellationToken: cancellationToken);

            var user = (User) stepContext.Values["user"];

            var route = user.PendingRoutes.FindRoute(selectedRoute);

            HttpResponseMessage httpResponseMessage;

            using (var httpClient = new HttpClient())
            {
                var httpRequestMessage = new HttpRequestMessage(HttpMethod.Post, GetActionUrl())
                {
                    Content = new StringContent(JsonConvert.SerializeObject(new
                    {
                        UserId = user.Id,
                        RouteId = route.Id
                    }), Encoding.UTF8, "application/json")
                };
                httpResponseMessage = await httpClient.SendAsync(httpRequestMessage, cancellationToken);
            }

            if (httpResponseMessage.IsSuccessStatusCode)
            {
                await stepContext.Context.SendActivityAsync(GetSuccessMessage(), null, null, cancellationToken);
            }
            else
            {
                await stepContext.Context.SendActivityAsync("Ups! No puede!", null, null, cancellationToken);
            }

            return await stepContext.EndDialogAsync(cancellationToken: cancellationToken);
        }

        protected abstract string GetSuccessMessage();
        protected abstract string GetActionUrl();
        protected abstract RoutesList GetRoutes(User user);
        protected abstract string GetSelectMessage();
    }
}
