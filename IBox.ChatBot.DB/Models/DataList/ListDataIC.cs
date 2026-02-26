namespace IBox.ChatBot.DB.Models.DataList
{
    public class ListDataIC
    {
        public List<ObjectIC>? LstDataIC { get; set; }
    }

    public class ObjectIC
    {
        public int Num { get; set; }
        public string? Data { get; set; }
        public bool CheckInputIC { get; set; }
    }
}