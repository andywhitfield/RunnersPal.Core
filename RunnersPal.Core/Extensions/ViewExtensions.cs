using System;
using System.Linq;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using RunnersPal.Core.Data;
using RunnersPal.Core.Models;

namespace RunnersPal.Core.Extensions
{
    public static class ViewExtensions
    {
        public static string UnitsToString(this DistanceUnits distanceUnit, string format)
        {
            switch (format.ToLower())
            {
                case "a": // plural...i.e. x miles
                case "abbrv":
                    switch (distanceUnit)
                    {
                        case DistanceUnits.Miles:
                            return "miles";
                        case DistanceUnits.Kilometers:
                            return "km";
                    }
                    break;
                case "a.s": // singular...i.e. min/mile
                case "abbrv.s":
                    switch (distanceUnit)
                    {
                        case DistanceUnits.Miles:
                            return "mile";
                        case DistanceUnits.Kilometers:
                            return "km";
                    }
                    break;
            }
            return distanceUnit.ToString();
        }

        public static string UserDistanceUnits(this HttpContext context, string format)
        {
            return UnitsToString(context.UserDistanceUnits(), format);
        }

        public static DistanceUnits UserDistanceUnits(this HttpContext context)
        {
            var distanceUnits = DistanceUnits.Miles;
            if (context.HasValidUserAccount())
            {
                object userUnits = context.UserAccount().DistanceUnits;
                if (Enum.IsDefined(typeof(DistanceUnits), userUnits))
                    distanceUnits = (DistanceUnits)userUnits;
            }
            else
            {
                // save/retrieve from the session
                if (!context.Session.TryGetValue("rp_UserDistanceUnits", out var sessionDistanceUnits))
                    context.Session.Set("rp_UserDistanceUnits", BitConverter.GetBytes((int)distanceUnits));
                else
                    distanceUnits = (DistanceUnits)BitConverter.ToInt32(sessionDistanceUnits);
            }
            return distanceUnits;
        }

        public static bool HasValidUserAccount(this HttpContext context)
        {
            var userAccount = UserAccount(context);
            return userAccount != null && userAccount.UserType != "N"; // 'New' - user is not quite a user until they give us their DisplayName.
        }

        public static dynamic UserAccount(this HttpContext context)
        {
            dynamic userAccount = null;
            if (context.Session.Keys.Contains("rp_UserAccount"))
                userAccount = context.Session.Get<UserAccount>("rp_UserAccount");

            if (userAccount == null)
            {
                var userCookie = context.Request.Cookies["rp_UserAccount"];
                if (userCookie != null)
                {
                    long userId;
                    if (long.TryParse(Secure.DecryptValue(userCookie, null), out userId))
                    {
                        userAccount = MassiveDB.Current.FindUser(userId);
                        if (userAccount != null)
                        {
                            context.Session.Set<UserAccount>("rp_UserAccount", (UserAccount)userAccount);
                        }
                    }
                }
            }

            return userAccount;
        }

        public static bool HasUserAccountWeight(this HttpContext context)
        {
            object userAccount = UserAccount(context);
            if (userAccount == null) return false;
            return userAccount.UserPrefs().Any();
        }

        public static void Set<T>(this ISession session, string key, T value)
        {
            session.SetString(key, JsonSerializer.Serialize(value));
        }

        public static T Get<T>(this ISession session, string key)
        {
            var value = session.GetString(key);

            return value == null ? default(T) : (T)JsonSerializer.Deserialize(value, typeof(T));
        }
    }
}