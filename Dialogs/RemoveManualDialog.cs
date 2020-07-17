using ZwiftBot.Dialogs.BaseDialogs;

namespace ZwiftBot.Dialogs
{
    public class RemoveManualDialog : BaseManualDialog
    {
        public RemoveManualDialog() : base(nameof(RemoveManualDialog))
        {

        }

        protected override string GetSuccessMessage()
        {
            return "Listo! Te quité esa ruta!";
        }

        protected override string GetActionUrl()
        {
            return "https://zwiftapi.azurewebsites.net/api/RemoveCompletedRoute";
        }

        protected override RoutesList GetRoutes(User user)
        {
            return user.CompletedRoutes;
        }

        protected override string GetSelectMessage()
        {
            return "Seleccioná la ruta que querés quitar:";
        }
    }
}
