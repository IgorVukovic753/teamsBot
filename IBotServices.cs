using Microsoft.Bot.Builder.AI.Luis;

namespace TeamsAuth
{
    public interface IBotServices
    {
        LuisRecognizer LuisRecognizer { get; }
    }
}
