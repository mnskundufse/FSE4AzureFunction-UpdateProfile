using System;
using System.Reflection;
using System.Threading.Tasks;
using EngineerUpdateProfile.CustomException;
using EngineerUpdateProfile.Model;
using EngineerUpdateProfile.Repository;

namespace EngineerUpdateProfile.Business
{
    public class EngineerUpdateProfileBusiness : IEngineerUpdateProfileBusiness
    {
        public readonly IEngineerUpdateProfileRepository _repo;
        public EngineerUpdateProfileBusiness(IEngineerUpdateProfileRepository repo)
        {
            _repo = repo;
        }

        /// <summary>
        /// Update User Profile (Business)
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="userExpertiseLevel"></param>
        /// <param name="updatedDateTime"></param>
        /// <returns></returns>
        public async Task<bool> UpdateUserProfileBusiness(int userId, UserExpertiseLevel userExpertiseLevel, DateTime updatedDateTime)
        {
            bool isUpdated = false;

            if (userExpertiseLevel != null &&
                userExpertiseLevel.TechnicalSkillExpertiseLevel != null &&
                userExpertiseLevel.NonTechnicalSkillExpertiseLevel != null)
            {
                //Validate Expertise Level (For Technical Skills)
                ValidateExpertiseLevel(userExpertiseLevel.TechnicalSkillExpertiseLevel);

                //Validate Expertise Level (For Non-Technical Skills)
                ValidateExpertiseLevel(userExpertiseLevel.NonTechnicalSkillExpertiseLevel);

                isUpdated = await _repo.UpdateUserProfileRepository(userId, userExpertiseLevel, updatedDateTime);
            }
            return isUpdated;
        }

        /// <summary>
        /// Validate Expertise level
        /// </summary>
        /// <param name="myObject"></param>
        private void ValidateExpertiseLevel(object myObject)
        {
            foreach (PropertyInfo pi in myObject.GetType().GetProperties())
            {
                if (pi.PropertyType == typeof(string))
                {
                    string value = (string)pi.GetValue(myObject);
                    if (string.IsNullOrEmpty(value) || !(int.TryParse(value, out int numericValue)))
                    {
                        //Expertise level for each skill must be Not null and numeric
                        throw new InvalidExpertiseLevelException(value);
                    }
                    else
                    {
                        if (numericValue < 0 || numericValue > 20)
                        {
                            //Expertise level for each skill must range between 0-20 
                            throw new InvalidExpertiseLevelException(numericValue);
                        }
                    }
                }
            }
        }
    }
}
