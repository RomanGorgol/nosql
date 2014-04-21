using System;
using ServiceStack.Redis;
using System.Web.UI;
using Tweets.ModelBuilding;
using Tweets.Models;

namespace Tweets.Repositories
{
    public class UserRepository : IUserRepository
    {
        private readonly RedisClient redisClient;
        private readonly IMapper<User, UserDocument> userDocumentMapper;
        private readonly IMapper<UserDocument, User> userMapper;

        public UserRepository(RedisClient redisClient, IMapper<User, UserDocument> userDocumentMapper, IMapper<UserDocument, User> userMapper)
        {
            this.redisClient = redisClient;
            this.userDocumentMapper = userDocumentMapper;
            this.userMapper = userMapper;
        }

        public void Save(User user)
        {
            //TODO: Здесь нужно реализовать сохранение пользователя в Redis
            redisClient.As<UserDocument>();
            var userDocument = userDocumentMapper.Map(user);
            redisClient.Set(userDocument.Id, userDocument);
            redisClient.Save();
        }

        public User Get(string userName)
        {
            //TODO: Здесь нужно доставать пользователя из Redis
            var result = redisClient.Get<UserDocument>(userName);
            if (result == null) return null;
            return  userMapper.Map(result);
		}
    }
}