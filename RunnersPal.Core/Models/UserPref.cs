using System;
using System.Collections.Generic;
using System.Linq;
using Massive;
using RunnersPal.Core.Data;
using RunnersPal.Core.Extensions;

namespace RunnersPal.Core.Models
{
    public class UserPref : DynamicModel
    {
        public UserPref() : base(MassiveDB.ConnectionStringName, "UserPref", "Id", primaryKeyFieldSequence: "Id", connectionStringProvider: MassiveDB.ConnectionStringProvider) { }
    }

    public static class UserPrefExtensions
    {
        public static dynamic Latest(this IEnumerable<dynamic> userPrefs)
        {
            return userPrefs.LastOrDefault();
        }
        public static dynamic Latest(this IEnumerable<dynamic> userPrefs, DateTime onDate)
        {
            var lastValidPref = userPrefs.FirstOrDefault(p =>
            {
                DateTime? validTo = ((object)p.ValidTo).ToDateTime();
                return validTo != null && validTo >= onDate;
            });
            return lastValidPref ?? Latest(userPrefs);
        }
    }
}
