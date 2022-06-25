using System;
using System.Threading.Tasks;
using EngineerUpdateProfile.Model;

namespace EngineerUpdateProfile.Business
{
    public interface IEngineerUpdateProfileBusiness
    {
        Task<bool> UpdateUserProfileBusiness(int userId, UserExpertiseLevel userExpertiseLevel, DateTime updatedDateTime);
    }
}
