namespace DevKit.Models
{
    public class MessageModel
    {
        public string Content { get; set; }
        public byte[] Bytes { get; set; }
        public string Time { get; set; }
        public bool IsSend { get; set; }
    }
}