namespace InkFlow.Domain;

public class ArticleTopic
{
    public Guid ArticleId { get; set; }
    public Article Article { get; set; } = null!;

    public Guid TopicId { get; set; }
    public Topic Topic { get; set; } = null!;
}
