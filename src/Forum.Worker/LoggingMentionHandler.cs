using Forum.Core;

namespace Forum.Worker;

public sealed class LoggingMentionHandler(ILogger<LoggingMentionHandler> logger) : IMentionHandler
{
    public Task HandleAsync(ForumMessage message, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(message);
        ct.ThrowIfCancellationRequested();
        logger.LogInformation(
            "Mention in post {PostId} message {MessageId}: {Content}",
            message.Post.PostId,
            message.Ref.MessageId,
            LogScrubber.Redact(message.Content));
        return Task.CompletedTask;
    }
}
