using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Dialogs.Choices;
using Newtonsoft.Json;

namespace CoreBot.Dialogs
{
    public class RemoveManualDialog : ComponentDialog
    {
        public RemoveManualDialog()
            : base(nameof(RemoveManualDialog))
        {
            {
                // This array defines how the Waterfall will execute.
                var waterfallInitSteps = new WaterfallStep[]
                {
                    AddStepAsync,
                    ResultStepAsync
                };

                // Add named dialogs to the DialogSet. These names are saved in the dialog state.
                AddDialog(new WaterfallDialog($"{nameof(WaterfallDialog)}", waterfallInitSteps));
                AddDialog(new ChoicePrompt(nameof(ChoicePrompt)));
            }
        }

        private async Task<DialogTurnResult> AddStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
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
                    Prompt = MessageFactory.Text("Seleccioná la ruta que querés quitar:"),
                    Choices = ChoiceFactory.ToChoices(user.CompletedRoutes
                        .Where(x => x.HasAward && !x.EventOnly && (x.AllowedSports & Sports.Cycling) == Sports.Cycling)
                        .Select(x => x.Name).ToList()),
                    Style = ListStyle.List
                }, cancellationToken);
        }

        private async Task<DialogTurnResult> ResultStepAsync(WaterfallStepContext stepContext,
            CancellationToken cancellationToken)
        {
            var selectedRoute = ((FoundChoice)stepContext.Result).Value;

            var user = (User) stepContext.Values["user"];

            var route = user.PendingRoutes.FindRoute(selectedRoute);

            HttpResponseMessage httpResponseMessage;

            using (var httpClient = new HttpClient())
            {
                var httpRequestMessage = new HttpRequestMessage(HttpMethod.Post, "https://zwiftapi.azurewebsites.net/api/RemoveCompletedRoute")
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
                await stepContext.Context.SendActivityAsync("Listo! Te quité esa ruta!", null, null, cancellationToken);
            }
            else
            {
                await stepContext.Context.SendActivityAsync("Ups! No puede!", null, null, cancellationToken);
            }

            return await stepContext.EndDialogAsync(cancellationToken: cancellationToken);
        }
    }
}
