using MessagePack;


namespace Model
{
    [MessagePackObject]
    public class Mail
    {
        [Key(0)]
        public int Id { get; set; }

        [Key(1)]
        public string Title { get; set; }

        [Key(2)]
        public string Content { get; set; }
    }
}
