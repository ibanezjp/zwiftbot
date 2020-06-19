using Microsoft.Bot.Schema;

namespace CoreBot
{
    public class UserProfile
    {
        public string UserName { get; set; }

        public Attachment Picture { get; set; }
    }
}
