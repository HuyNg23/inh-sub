namespace IBox.ChatBot.DB.Models.FPT
{
    public class ListTokenModel
    {
        public List<TokenBotModel>? List { get; set; }
    }

    public class TokenBotModel
    {
        public string? BOTName { get; set; }
        public string? Token { get; set; }
    }
}