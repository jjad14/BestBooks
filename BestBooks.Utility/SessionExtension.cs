using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace BestBooks.Utility
{
    // This is just a generic implementation of session to convert it into an object and store it
    // and we'll retrieve it convert it back and displayed.
    // In Session we will store a number of items in the shopping cart
    public static class SessionExtension
    {
        // "this" keyword represents the object itself.
        // So we'll have the session here then we'll have the string of key and an object as a value in here.
        public static void SetObject(this ISession session, string key, object value)
        {
            // This will set the session
            session.SetString(key, JsonConvert.SerializeObject(value));
        }

        public static T GetObject<T>(this ISession session, string key)
        {
            var value = session.GetString(key);

            return value == null ? default(T) : JsonConvert.DeserializeObject<T>(value);
        }


    }
}
