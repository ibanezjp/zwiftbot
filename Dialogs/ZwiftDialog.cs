using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Dialogs.Choices;

namespace CoreBot.Dialogs
{
    public class ZwiftDialog : ComponentDialog
    {
        private readonly IStatePropertyAccessor<UserProfile> _userProfileAccessor;

        public ZwiftDialog(UserState userState, 
                           UploadDialog uploadDialog, 
                           PendingDialog pendingDialog, 
                           AddManualDialog addManualDialog, 
                           RemoveManualDialog removeManualDialog) 
            : base(nameof(ZwiftDialog))
        {
            _userProfileAccessor = userState.CreateProperty<UserProfile>("UserProfile");

            // This array defines how the Waterfall will execute.
            var waterfallInitSteps = new WaterfallStep[]
            {
                ActionStepAsync,
                NameStepAsync,
                NameConfirmStepAsync,
                DoMoreStepAsync,
                LoopStepAsync
            };

            // Add named dialogs to the DialogSet. These names are saved in the dialog state.
            AddDialog(new WaterfallDialog($"{nameof(WaterfallDialog)}", waterfallInitSteps));
            AddDialog(new TextPrompt(nameof(TextPrompt)));
            AddDialog(new ChoicePrompt(nameof(ChoicePrompt)));
            AddDialog(new ConfirmPrompt(nameof(ConfirmPrompt)));
            AddDialog(uploadDialog);
            AddDialog(pendingDialog);
            AddDialog(addManualDialog);
            AddDialog(removeManualDialog);

            // The initial child Dialog to run.
            InitialDialogId = nameof(WaterfallDialog);
        }

        private static async Task<DialogTurnResult> ActionStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            if (stepContext.Options != null)
            {
                stepContext.Values["username"] = stepContext.Options.ToString();
                return await stepContext.PromptAsync(nameof(ChoicePrompt),
                    new PromptOptions
                    {
                        Prompt = MessageFactory.Text("En que más te puedo ayudar?"),
                        Choices = ChoiceFactory.ToChoices(new List<string>
                        {
                            "Subir nuevas chapas.", 
                            "Buscar chapas pendintes.",
                            "Agregar chapa manualmente.",
                            "Quitar chapa manualmente."
                        })
                    }, cancellationToken);
            }

            return await stepContext.PromptAsync(nameof(ChoicePrompt),
                new PromptOptions
                {
                    Prompt = MessageFactory.Text("Buenas! En qué te puedo ayudar?"),
                    Choices = ChoiceFactory.ToChoices(new List<string>
                    {
                        "Subir nuevas chapas.",
                        "Buscar chapas pendintes.",
                        "Agregar chapa manualmente.",
                        "Quitar chapa manualmente."
                    })
                }, cancellationToken);
        }

        private static async Task<DialogTurnResult> NameStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            stepContext.Values["action"] = ((FoundChoice)stepContext.Result).Value;

            if (!stepContext.Values.ContainsKey("username"))
                return await stepContext.PromptAsync(nameof(TextPrompt),
                    new PromptOptions {Prompt = MessageFactory.Text("Decime tu usuario:")}, cancellationToken);

            switch (stepContext.Values["action"])
            {
                case "Subir nuevas chapas.":
                    return await stepContext.BeginDialogAsync(nameof(UploadDialog), stepContext.Values["username"], cancellationToken);
                case "Buscar chapas pendintes.":
                    return await stepContext.BeginDialogAsync(nameof(PendingDialog), stepContext.Values["username"], cancellationToken);
                case "Agregar chapa manualmente.":
                    return await stepContext.BeginDialogAsync(nameof(AddManualDialog), stepContext.Values["username"], cancellationToken);
                case "Quitar chapa manualmente.":
                    return await stepContext.BeginDialogAsync(nameof(RemoveManualDialog), stepContext.Values["username"], cancellationToken);
                default:
                    throw new ArgumentException();
            }
        }

        private async Task<DialogTurnResult> NameConfirmStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            if (stepContext.Values.ContainsKey("username"))
                return await stepContext.ContinueDialogAsync(cancellationToken);

            stepContext.Values["username"] = (string)stepContext.Result;

            switch (stepContext.Values["action"])
            {
                case "Subir nuevas chapas.":
                    return await stepContext.BeginDialogAsync(nameof(UploadDialog), stepContext.Values["username"], cancellationToken);
                case "Buscar chapas pendintes.":
                    return await stepContext.BeginDialogAsync(nameof(PendingDialog), stepContext.Values["username"], cancellationToken);
                case "Agregar chapa manualmente.":
                    return await stepContext.BeginDialogAsync(nameof(AddManualDialog), stepContext.Values["username"], cancellationToken);
                case "Quitar chapa manualmente.":
                    return await stepContext.BeginDialogAsync(nameof(RemoveManualDialog), stepContext.Values["username"], cancellationToken);
                default:
                    throw new ArgumentException();
            }
        }

        private async Task<DialogTurnResult> DoMoreStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            return await stepContext.PromptAsync(nameof(ConfirmPrompt), new PromptOptions { Prompt = MessageFactory.Text("Te puedo ayudar en algo más?") }, cancellationToken);
        }

        private async Task<DialogTurnResult> LoopStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            return (bool)stepContext.Result
                ? await stepContext.ReplaceDialogAsync(nameof(WaterfallDialog), stepContext.Values["username"], cancellationToken)
                : await stepContext.EndDialogAsync(cancellationToken: cancellationToken);
        }
    }
}
