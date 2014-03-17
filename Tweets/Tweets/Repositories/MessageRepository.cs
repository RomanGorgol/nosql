using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.Linq.Mapping;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using Tweets.ModelBuilding;
using Tweets.Models;

namespace Tweets.Repositories
{
    public class MessageRepository : IMessageRepository
    {
        private readonly string connectionString;
        private readonly AttributeMappingSource mappingSource;
        private readonly IMapper<Message, MessageDocument> messageDocumentMapper;

        public MessageRepository(IMapper<Message, MessageDocument> messageDocumentMapper)
        {
            this.messageDocumentMapper = messageDocumentMapper;
            mappingSource = new AttributeMappingSource();
            connectionString = ConfigurationManager.ConnectionStrings["SqlConnectionString"].ConnectionString;
        }

        public void Save(Message message)
        {
            var messageDocument = messageDocumentMapper.Map(message);
            using (var db = new TweetsDataContext(connectionString))
            {
                db.GetTable<MessageDocument>().InsertOnSubmit(messageDocument);
                db.SubmitChanges();
            }
            //TODO: Здесь нужно реализовать вставку сообщения в базу
        }

        public void Like(Guid messageId, User user)
        {
            var likeDocument = new LikeDocument {MessageId = messageId, UserName = user.Name, CreateDate = DateTime.UtcNow};
            using (var db = new TweetsDataContext(connectionString))
            {
                db.GetTable<LikeDocument>().InsertOnSubmit(likeDocument);
                db.SubmitChanges();
            }
            //TODO: Здесь нужно реализовать вставку одобрения в базу
        }

        public void Dislike(Guid messageId, User user)
        {
            using (var db = new TweetsDataContext(connectionString))
            {
                var likeDocument =
                    db.GetTable<LikeDocument>()
                        .FirstOrDefault(like => like.MessageId == messageId && like.UserName == user.Name);
                if (likeDocument != null)
                {
                    db.GetTable<LikeDocument>().DeleteOnSubmit(likeDocument);
                    db.SubmitChanges();
                }
            }
            //TODO: Здесь нужно реализовать удаление одобрения из базы
        }

        public IEnumerable<Message> GetPopularMessages()
        {
            //TODO: Здесь нужно возвращать 10 самых популярных сообщений
            using (var db = new TweetsDataContext(connectionString))
            {
                return (from message in db.GetTable<MessageDocument>()
                    join like in db.GetTable<LikeDocument>() on message.Id equals like.MessageId into messageLike
                    from m in messageLike.DefaultIfEmpty()
                    select new
                    {
                        message,
                        userName = m != null ? m.UserName : null
                    }
                    )
                    .GroupBy(s=>s.message)
                    .Select(group => new Message()
                    {
                        Id = group.Key.Id,
                        CreateDate = group.Key.CreateDate,
                        Text = group.Key.Text,
                        Likes = group.Count(s=>s.userName!=null),
                        User = new User() {Name = group.Key.UserName}
                    })
                    .OrderByDescending(s=>s.Likes)
                    .Take(10)
                    .ToArray();
             }
            return Enumerable.Empty<Message>();
        }

        public IEnumerable<UserMessage> GetMessages(User user)
        {
            //TODO: Здесь нужно получать все сообщения конкретного пользователя
            using (var db = new TweetsDataContext(connectionString))
            {
                return (from message in db.GetTable<MessageDocument>()
                        where message.UserName==user.Name
                        join like in db.GetTable<LikeDocument>() on message.Id equals like.MessageId into messageLike
                        from m in messageLike.DefaultIfEmpty()
                        select new
                        {
                            message,
                            userName = m != null ? m.UserName : null
                        }
                       )
                       .GroupBy(s => s.message)
                       .Select(group => new UserMessage()
                       {
                           Id = group.Key.Id,
                           CreateDate = group.Key.CreateDate,
                           Text = group.Key.Text,
                           Likes = group.Count(s => s.userName != null),
                           Liked = group.Any(s=>s.userName==s.message.UserName),
                           User = new User() { Name = group.Key.UserName }
                       })
                       .OrderByDescending(s=>s.CreateDate)
                        .ToArray();
            }
            return Enumerable.Empty<UserMessage>();
        }
    }
}