using System;
using System.Reflection;
using System.Web.UI;
using CorrugatedIron;
using CorrugatedIron.Models;
using Tweets.Attributes;
using Tweets.ModelBuilding;
using Tweets.Models;

namespace Tweets.Repositories
{
    public class UserRepository : IUserRepository
    {
        private readonly string bucketName;
        private readonly IRiakClient riakClient;
        private readonly IMapper<User, UserDocument> userDocumentMapper;
        private readonly IMapper<UserDocument, User> userMapper;

        public UserRepository(IRiakClient riakClient, IMapper<User, UserDocument> userDocumentMapper, IMapper<UserDocument, User> userMapper)
        {
            this.riakClient = riakClient;
            this.userDocumentMapper = userDocumentMapper;
            this.userMapper = userMapper;
            bucketName = typeof (UserDocument).GetCustomAttribute<BucketNameAttribute>().BucketName;
        }

        public void Save(User user)
        {
            var userDocument = userDocumentMapper.Map(user);
            var o = new RiakObject(bucketName, userDocument.Id,userDocument);
            riakClient.Put(o);
            //TODO: Здесь нужно реализовать сохранение пользователя в Riak
        }

        public User Get(string userName)
        {
            //TODO: Здесь нужно доставать пользователя из Riak
            var result = riakClient.Get(bucketName, userName);
            if (result.IsSuccess)
            {
                var value =  result.Value.GetObject<UserDocument>();
                return userMapper.Map(value);
            }
            return null;
            
        }
    }
}