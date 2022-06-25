using System;
using System.Threading.Tasks;
using EngineerUpdateProfile.Model;

namespace EngineerUpdateProfile.Repository
{
    public interface IEngineerUpdateProfileRepository
    {
        Task<bool> UpdateUserProfileRepository(int userId, UserExpertiseLevel userExpertiseLevel, DateTime updatedDateTime);
    }
}
