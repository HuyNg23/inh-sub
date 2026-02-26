namespace IBox.ChatBot.DB.Models.CreateFile.IBox
{
    public class CountDataModel
    {
        private int total = 0;
        private string parth = "";

        public int Total { get => total; set => total = value; }
        public string Parth { get => parth; set => parth = value; }
    }
}