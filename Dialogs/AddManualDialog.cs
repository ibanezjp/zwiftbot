using CoreBot.Dialogs.BaseDialogs;

namespace CoreBot.Dialogs
{
    public class AddManualDialog : BaseManualDialog
    {
        public AddManualDialog() : base(nameof(AddManualDialog))
        {
            
        }

        protected override string GetSuccessMessage()
        {
            return "Listo! Te agregué esa ruta!";
        }

        protected override string GetActionUrl()
        {
            return "https://zwiftapi.azurewebsites.net/api/AddCompletedRoute";
        }

        protected override RoutesList GetRoutes(User user)
        {
            return user.PendingRoutes;
        }

        protected override string GetSelectMessage()
        {
            return "Seleccioná la ruta que querés agregar:";
        }
    }
}
