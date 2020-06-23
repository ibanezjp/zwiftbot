using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Newtonsoft.Json;

namespace CoreBot.Dialogs
{
    public class PendingDialog : ComponentDialog
    {
        public PendingDialog()
            : base(nameof(PendingDialog))
        {
            {
                // This array defines how the Waterfall will execute.
                var waterfallInitSteps = new WaterfallStep[]
                {
                    AskMinStepAsync,
                    AskMaxStepAsync,
                    AskPartnerStepAsync,
                    ResultStepAsync
                };

                // Add named dialogs to the DialogSet. These names are saved in the dialog state.
                AddDialog(new WaterfallDialog($"{nameof(WaterfallDialog)}", waterfallInitSteps));
                AddDialog(new TextPrompt(nameof(TextPrompt)));
                //AddDialog(new ChoicePrompt(nameof(ChoicePrompt)));
                //AddDialog(new ConfirmPrompt(nameof(ConfirmPrompt)));
                //AddDialog(uploadDialog);
            }
        }

        private static async Task<DialogTurnResult> AskMinStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            return await stepContext.PromptAsync(nameof(TextPrompt),
                new PromptOptions { Prompt = MessageFactory.Text("Te voy a ayudar a buscar un circuito, decime la cantidad mínima de KMs que querés que tenga?") }, cancellationToken);
        }

        private async Task<DialogTurnResult> AskMaxStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            stepContext.Values["min"] = (string)stepContext.Result;

            return await stepContext.PromptAsync(nameof(TextPrompt),
                new PromptOptions { Prompt = MessageFactory.Text("Ahora decime la cantidad máxima de KMs que querés que tenga?") }, cancellationToken);
        }

        private async Task<DialogTurnResult> AskPartnerStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            stepContext.Values["max"] = (string)stepContext.Result;

            return await stepContext.PromptAsync(nameof(TextPrompt),
                new PromptOptions { Prompt = MessageFactory.Text("Con quien vas a rodar?") }, cancellationToken);
        }

        private async Task<DialogTurnResult> ResultStepAsync(WaterfallStepContext stepContext,
            CancellationToken cancellationToken)
        {
            stepContext.Values["ids"] = (string) stepContext.Result;

            await stepContext.Context.SendActivityAsync("Estoy buscandote algunos circuitos...", null, null,
                cancellationToken);

            var ids = $"{stepContext.Options},{stepContext.Values["ids"]}";

            var url = string.Format("https://zwiftapi.azurewebsites.net/api/GetPendingRoutes?ids={0}&min={1}&max={2}",ids, stepContext.Values["min"], stepContext.Values["max"]);

            List<Route> routes;

            using (var httpClient = new HttpClient())
            {
                var response = await httpClient.GetStringAsync(new Uri(url));
                routes = JsonConvert.DeserializeObject<List<Route>>(response);
            }

            var tmp = routes.Where(x => x.EventOnly == false && 
                                             x.HasAward && 
                                            (x.AllowedSports & Sports.Cycling) == Sports.Cycling)
                .OrderBy(x => x.Distance).ToList();


            var stringBuilder = new StringBuilder();

            foreach (var routesByWorld in tmp.GroupBy(x => x.World))
            {
                stringBuilder.AppendLine(routesByWorld.Key);
                stringBuilder.AppendLine("==============");
                foreach (var route in routesByWorld)
                {
                    stringBuilder.AppendLine(route.ToString());
                }
            }

            await stepContext.Context.SendActivityAsync(stringBuilder.ToString(), null, null,
                cancellationToken);

            return await stepContext.EndDialogAsync(cancellationToken: cancellationToken);
        }
    }
}
