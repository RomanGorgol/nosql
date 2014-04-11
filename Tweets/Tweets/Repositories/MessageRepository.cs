using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net.Mime;
using Hammock.Serialization;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using System.Text.RegularExpressions;
using MongoDB.Driver.Builders;
using Newtonsoft.Json.Linq;
using Tweets.ModelBuilding;
using Tweets.Models;

namespace Tweets.Repositories
{
    public class MessageRepository : IMessageRepository
    {
        private readonly IMapper<Message, MessageDocument> messageDocumentMapper;
        private readonly MongoCollection<MessageDocument> messagesCollection;

        public MessageRepository(IMapper<Message, MessageDocument> messageDocumentMapper)
        {
            this.messageDocumentMapper = messageDocumentMapper;
            var connectionString = ConfigurationManager.ConnectionStrings["MongoDb"].ConnectionString;
            var databaseName = MongoUrl.Create(connectionString).DatabaseName;
            messagesCollection =
                new MongoClient(connectionString).GetServer().GetDatabase(databaseName).GetCollection<MessageDocument>(MessageDocument.CollectionName);
        }

        public void Save(Message message)
        {
            var messageDocument = messageDocumentMapper.Map(message);
            messagesCollection.Insert(messageDocument);
            //TODO: Здесь нужно реализовать вставку сообщения в базу
        }

        public void Like(Guid messageId, User user)
        {
            var likeDocument = new LikeDocument {UserName = user.Name, CreateDate = DateTime.UtcNow};

            if (
                messagesCollection.FindOne(Query.And(Query<MessageDocument>.EQ(s=>s.Id,messageId),Query<MessageDocument>.ElemMatch(d => d.Likes,
                    builder => builder.EQ(s => s.UserName, user.Name)))) == null)
            {
                messagesCollection.Update(Query<MessageDocument>.EQ(m => m.Id, messageId),Update<MessageDocument>.Push(s => s.Likes, likeDocument));   
            }
            //TODO: Здесь нужно реализовать вставку одобрения в базу
        }

        public void Dislike(Guid messageId, User user)
        {
            var likeDocument = new LikeDocument {UserName = user.Name};
            messagesCollection.Update(Query<MessageDocument>.EQ(m => m.Id, messageId),
                Update<MessageDocument>.Pull(m => m.Likes,builder=>builder.EQ(s=>s.UserName,user.Name)));
            //TODO: Здесь нужно реализовать удаление одобрения из базы
        }

        public IEnumerable<Message> GetPopularMessages()
        {
            //TODO: Здесь нужно возвращать 10 самых популярных сообщений
            //TODO: Важно сортировку выполнять на сервере
            //TODO: Тут будет полезен AggregationFramework
            var condition = new BsonDocument
            {
                {
                    "$or", new BsonArray
                    {
                        new BsonDocument
                        {
                            {"$eq", new BsonArray {"$likes", new BsonArray()}}
                        },
                        new BsonDocument
                        {
                            {"$eq", new BsonArray {"$likes", BsonNull.Value}}
                        }
                    }
                }
            };
            var project1 = new BsonDocument
            {
                {"$project", new BsonDocument
                {
                    {"likes",1},
                    {"text",1},
                    {"createDate",1},
                    {"userName",1},
                    {"full_likes",new BsonDocument
                    {
                        {"$cond",new BsonArray()
                        {
                            condition,
                            new BsonArray(){new BsonDocument{}},
                            "$likes",
                        }}
                    }},
                    {"item",new BsonDocument
                    {
                        {"$cond",new BsonArray()
                        {
                            condition,
                            0,
                            1,
                        }}
                    }}
                }}
            };
                
            var unwind = new BsonDocument 
                { 
                    {"$unwind","$full_likes"} 
                };
            var group = new BsonDocument
            {
                {"$group",
                    new BsonDocument
                    {
                        {"_id",new BsonDocument
                            {
                                {"_id","$_id"},
                                {"text","$text"},
                                {"createDate","$createDate"},
                                {"userName","$userName"},
                                {"likes","$likes"}
                            }
                        },
                        {
                            "count_likes", new BsonDocument
                            {
                                {"$sum", "$item"}
                            }
                        }
                    }
                }
            };
            var sort = new BsonDocument 
                { 
                    { 
                        "$sort", 
                        new BsonDocument 
                            { 
                                {"count_likes",new BsonInt32(-1)}, 
                                {"_id.createDate",new BsonInt32(-1)} 
                            } 
                    } 
                };
            var limit = new BsonDocument 
                { 
                    {"$limit",10} 
                };
            var project2 = new BsonDocument
            {
                {"$project",new BsonDocument
                    {
                        {"_id","$_id._id"},
                        {"text","$_id.text"},
                        {"createDate","$_id.createDate"},
                        {"userName","$_id.userName"},
                        {"likes","$_id.likes"}
                    }
                }
            };
            var pipeline = new[] {project1,unwind,group,sort,limit,project2};
            var result = messagesCollection.Aggregate(pipeline).ResultDocuments.ToList();
            return result.Select(BsonSerializer.Deserialize<MessageDocument>).Select(d => new Message()
            {
                Id = d.Id,
                Text = d.Text,
                CreateDate = d.CreateDate,
                User = new User() { Name = d.UserName },
                Likes = d.Likes==null ? 0 : d.Likes.Count()
            });
        }

        public IEnumerable<UserMessage> GetMessages(User user)
        {
            //TODO: Здесь нужно получать все сообщения конкретного пользователя
            var messages = messagesCollection.Find(Query<MessageDocument>.EQ(s => s.UserName, user.Name));
            return messages.Select(s => new UserMessage()
            {
                User = user,
                CreateDate = s.CreateDate,
                Id = s.Id,
                Text = s.Text,
                Likes = s.Likes==null ? 0 : s.Likes.Count(),
                Liked = s.Likes != null && s.Likes.Select(d => d.UserName).Contains(user.Name)
            }).OrderByDescending(s=>s.CreateDate);
        }
    }
}