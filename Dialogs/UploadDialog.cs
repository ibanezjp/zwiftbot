using System.Collections.Generic;
using System.Linq;
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
            return (bool) stepContext.Result
                ? await stepContext.ReplaceDialogAsync(nameof(WaterfallDialog), cancellationToken: cancellationToken)
                : await stepContext.EndDialogAsync(cancellationToken: cancellationToken);
        }

        private async Task ProcessFile(Attachment attachment, WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            await stepContext.Context.SendActivityAsync("Estoy procesando tu imagen!", null, null, cancellationToken);
            await Task.Delay(5000, cancellationToken);
            await stepContext.Context.SendActivityAsync("Conseguiste 3 chapas!", null, null, cancellationToken);
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
