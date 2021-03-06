﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Dialogs.Choices;
using Newtonsoft.Json;

namespace ZwiftBot.Dialogs
{
    public class PendingDialog : ComponentDialog
    {
        public PendingDialog()
            : base(nameof(PendingDialog))
        {
            {
                var waterfallInitSteps = new WaterfallStep[]
                {
                    AskMinStepAsync,
                    AskMaxStepAsync,
                    AskAloneStepAsync,
                    AskPartnerStepAsync,
                    ResultStepAsync
                };

                AddDialog(new WaterfallDialog($"{nameof(WaterfallDialog)}", waterfallInitSteps));
                AddDialog(new TextPrompt($"{nameof(TextPrompt)}_Min_Km", ValidatorMin));
                AddDialog(new TextPrompt($"{nameof(TextPrompt)}_Max_Km", ValidatorMax));
                AddDialog(new TextPrompt($"{nameof(TextPrompt)}"));
                AddDialog(new ChoicePrompt(nameof(ChoicePrompt)));
            }
        }

        private Task<bool> ValidatorMin(PromptValidatorContext<string> promptValidatorContext, CancellationToken cancellationtoken)
        {
            if (!promptValidatorContext.Recognized.Succeeded) 
                return Task.FromResult(false);

            return !int.TryParse(promptValidatorContext.Recognized.Value, out var km) ? 
                Task.FromResult(false) : 
                Task.FromResult(km >= 0);
        }

        private Task<bool> ValidatorMax(PromptValidatorContext<string> promptValidatorContext, CancellationToken cancellationtoken)
        {
            if (!promptValidatorContext.Recognized.Succeeded)
                return Task.FromResult(false);

            return !int.TryParse(promptValidatorContext.Recognized.Value, out var km) ?
                Task.FromResult(false) :
                Task.FromResult(km >= 1);
        }

        private static async Task<DialogTurnResult> AskMinStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            return await stepContext.PromptAsync($"{nameof(TextPrompt)}_Min_Km",
                new PromptOptions { Prompt = MessageFactory.Text("Te voy a ayudar a buscar un circuito, decime la cantidad mínima de KMs que querés que tenga?") }, cancellationToken);
        }

        private async Task<DialogTurnResult> AskMaxStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            stepContext.Values["min"] = (string)stepContext.Result;

            return await stepContext.PromptAsync($"{nameof(TextPrompt)}_Max_Km",
                new PromptOptions { Prompt = MessageFactory.Text("Ahora decime la cantidad máxima de KMs que querés que tenga?") }, cancellationToken);
        }

        private async Task<DialogTurnResult> AskAloneStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            stepContext.Values["max"] = (string)stepContext.Result;

            return await stepContext.PromptAsync(nameof(ChoicePrompt),
                new PromptOptions
                {
                    Prompt = MessageFactory.Text("Cómo vas a rodar?"),
                    Choices = ChoiceFactory.ToChoices(new List<string> { "Sólo.", "Con amigos." }),
                    Style = ListStyle.List
                }, cancellationToken);
        }

        private async Task<DialogTurnResult> AskPartnerStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            stepContext.Values["mode"] = ((FoundChoice)stepContext.Result).Value;

            if(!stepContext.Values["mode"].ToString().Equals("Sólo.", StringComparison.InvariantCultureIgnoreCase))
                return await stepContext.PromptAsync(nameof(TextPrompt),
                    new PromptOptions {Prompt = MessageFactory.Text("Con quien vas a rodar?")}, cancellationToken);

            return await stepContext.ContinueDialogAsync(cancellationToken);
        }

        private async Task<DialogTurnResult> ResultStepAsync(WaterfallStepContext stepContext,
            CancellationToken cancellationToken)
        {
            await stepContext.Context.SendActivityAsync("Estoy buscándote algunos circuitos...", null, null,
                cancellationToken);

            var username = stepContext.Options.ToString().ToLowerInvariant();

            string url;

            if (stepContext.Reason == DialogReason.ContinueCalled)
            {
                url = $"https://zwiftapi.azurewebsites.net/api/GetPendingRoutes?ids={username}&min={stepContext.Values["min"]}&max={stepContext.Values["max"]}";
            }
            else
            {
                var users = stepContext.Result.ToString().Split(',');
                var ids = $"{username},{string.Join(',',users.Where(x => !string.IsNullOrEmpty(x)).Select(x => x.Trim().ToLowerInvariant()))}";
                url =$"https://zwiftapi.azurewebsites.net/api/GetPendingRoutes?ids={ids}&min={stepContext.Values["min"]}&max={stepContext.Values["max"]}";
            }

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


            if (tmp.Count > 0)
            {
                var stringBuilder = new StringBuilder();

                foreach (var routesByWorld in tmp.GroupBy(x => x.World))
                {
                    stringBuilder.AppendLine($"# {routesByWorld.Key}");
                    foreach (var route in routesByWorld)
                    {
                        stringBuilder.AppendLine($"{route}\n");
                    }
                }

                await stepContext.Context.SendActivityAsync(stringBuilder.ToString(), null, null,
                    cancellationToken);
            }
            else
            {
                await stepContext.Context.SendActivityAsync("No encontré ninguna ruta que coincida con tus parámetros de búsqueda!", null, null,
                    cancellationToken);
            }

            return await stepContext.EndDialogAsync(cancellationToken: cancellationToken);
        }
    }
}
