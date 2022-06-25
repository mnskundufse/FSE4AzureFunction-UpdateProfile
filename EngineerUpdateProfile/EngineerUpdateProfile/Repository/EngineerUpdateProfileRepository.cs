using System;
using System.Threading.Tasks;
using EngineerUpdateProfile.CustomException;
using EngineerUpdateProfile.Model;
using Microsoft.Extensions.Configuration;
using MongoDB.Driver;

namespace EngineerUpdateProfile.Repository
{
    public class EngineerUpdateProfileRepository : IEngineerUpdateProfileRepository
    {
        private readonly MongoClient _mongoClient;
        private readonly IMongoDatabase _database;
        private readonly IMongoCollection<UserProfile> _userProfile;

        public EngineerUpdateProfileRepository(MongoClient mongoClient, IConfiguration configuration)
        {
            _mongoClient = mongoClient;
            _database = _mongoClient.GetDatabase(configuration["DatabaseName"]);
            _userProfile = _database.GetCollection<UserProfile>(configuration["CollectionName"]);
        }

        public async Task<bool> UpdateUserProfileRepository(int userId, UserExpertiseLevel userExpertiseLevel, DateTime updatedDateTime)
        {
            bool updateFlag = false;
            var userItem = await _userProfile
                            .Find(
                                filter: f => f.UserId == userId
                            ).FirstOrDefaultAsync();

            if (userItem != null)
            {
                if ((DateTime.Now - userItem.UpdatedDate).Days <= 10)
                {
                    //Update of Profile must be allowed only after 10 days of adding profile or last change, else throw a custom exception
                    throw new UpdateProfileAfterValidDateException();
                }
                else
                {
                    userItem.TechnicalSkillExpertiseLevel = userExpertiseLevel.TechnicalSkillExpertiseLevel;
                    userItem.NonTechnicalSkillExpertiseLevel = userExpertiseLevel.NonTechnicalSkillExpertiseLevel;
                    userItem.UpdatedDate = updatedDateTime;

                    ReplaceOneResult updatedResult = await _userProfile.ReplaceOneAsync(
                        filter: f => f.UserId == userId,
                        replacement: userItem
                        );

                    if (updatedResult != null && updatedResult.IsAcknowledged && updatedResult.ModifiedCount > 0)
                    {
                        updateFlag = true;
                    }
                }
            }
            else
            {
                //If invalid UserId is provided, it must throw a custom exception
                throw new InvalidUserIdException();
            }
            return updateFlag;
        }
    }
}
