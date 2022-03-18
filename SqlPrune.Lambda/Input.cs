namespace SqlPrune.Lambda;

public class Input
{
    /// <summary>
    /// Default constructor.
    /// </summary>
    public Input()
    {
        FileExtensions = new[] {"*.bak,", "*.bak.7z", "*.sql", "*.sql.gz"};
    }

    /// <summary>
    /// The bucket name
    /// </summary>
    public string BucketName { get; set; }

    /// <summary>
    /// The file extensions to restrict to when listing files in the bucket.
    /// </summary>
    public string[] FileExtensions { get; set; }
}