using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;

namespace CoreBot.Dialogs
{
    public class UploadDialog : ComponentDialog
    {
        public UploadDialog()
            : base(nameof(UploadDialog))
        {
            AddDialog(new AttachmentPrompt(nameof(AttachmentPrompt), PicturePromptValidatorAsync));
            AddDialog(new ConfirmPrompt(nameof(ConfirmPrompt)));
            AddDialog(new WaterfallDialog(nameof(WaterfallDialog), new WaterfallStep[]
            {
                FileUploadStepAsync,
                ProcessStepAsync,
                ConfirmStepAsync
            }));

            // The initial child Dialog to run.
            InitialDialogId = nameof(WaterfallDialog);
        }

        private static async Task<DialogTurnResult> FileUploadStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var promptOptions = new PromptOptions
            {
                Prompt = MessageFactory.Text("Mandame la captura de Zwift donde se vea la chapa nueva!"),
                RetryPrompt = MessageFactory.Text("The attachment must be a jpeg/png image file."),
            };

            return await stepContext.PromptAsync(nameof(AttachmentPrompt), promptOptions, cancellationToken);
        }

        private async Task<DialogTurnResult> ProcessStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            await ProcessFile(((IList<Attachment>)stepContext.Result)?.FirstOrDefault(), stepContext, cancellationToken);

            return await stepContext.PromptAsync(nameof(ConfirmPrompt), new PromptOptions { Prompt = MessageFactory.Text("Querés subir otra captura?") }, cancellationToken);
        }

        private async Task<DialogTurnResult> ConfirmStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            return (bool)stepContext.Result
                ? await stepContext.ReplaceDialogAsync(nameof(WaterfallDialog), cancellationToken: cancellationToken, options: stepContext.Options)
                : await stepContext.EndDialogAsync(cancellationToken: cancellationToken);
        }

        private async Task ProcessFile(Attachment attachment, WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            await stepContext.Context.SendActivityAsync("Estoy enviando tu imagen para procesar!", null, null, cancellationToken);

            var url = $"https://zwiftapi.azurewebsites.net/api/UploadRoutes/{stepContext.Options}";

            using var webClient = new WebClient();
            var screenshot = await webClient.DownloadDataTaskAsync(new Uri($"{attachment.ContentUrl}"));

            HttpResponseMessage httpResponseMessage;

            using (var httpClient = new HttpClient())
            {
                var httpRequestMessage = new HttpRequestMessage(HttpMethod.Post, url)
                {
                    Content = new ByteArrayContent(screenshot)
                };
                httpResponseMessage = await httpClient.SendAsync(httpRequestMessage, cancellationToken);
            }

            if(httpResponseMessage != null && httpResponseMessage.IsSuccessStatusCode)
                await stepContext.Context.SendActivityAsync("Recibí tu imagen! La estoy procesando!", null, null, cancellationToken);
            else
                await stepContext.Context.SendActivityAsync("Ufff se me complicó! No puede recibir tu imagen!", null, null, cancellationToken);
        }

        private static async Task<bool> PicturePromptValidatorAsync(PromptValidatorContext<IList<Attachment>> promptContext, CancellationToken cancellationToken)
        {
            if (promptContext.Recognized.Succeeded)
            {
                var attachments = promptContext.Recognized.Value;
                var validImages = new List<Attachment>();

                foreach (var attachment in attachments)
                {
                    if (attachment.ContentType == "image/jpeg" || attachment.ContentType == "image/png")
                    {
                        validImages.Add(attachment);
                    }
                }

                promptContext.Recognized.Value = validImages;

                // If none of the attachments are valid images, the retry prompt should be sent.
                return validImages.Any();
            }
            else
            {
                await promptContext.Context.SendActivityAsync("Tenés que mandarme la captura!");

                // We can return true from a validator function even if Recognized.Succeeded is false.
                return false;
            }
        }

    }
}
