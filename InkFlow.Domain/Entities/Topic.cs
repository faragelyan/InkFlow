namespace InkFlow.Domain;

public class Topic
{
    public Guid TopicId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public ICollection<ArticleTopic> ArticleTopics { get; set; } = new List<ArticleTopic>();
}
