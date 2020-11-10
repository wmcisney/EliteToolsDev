using System;
using System.Collections;
using System.Collections.Generic;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Web;
using System.Web.Mvc;
using System.Xml;
using System.Xml.Serialization;
using Microsoft.AspNet.SignalR.Infrastructure;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using NHibernate;
using NHibernate.Proxy;
using RadialReview.Models.Angular;
using RadialReview.Models.Angular.Base;
using System.Linq.Expressions;
using Newtonsoft.Json.Converters;
using RadialReview.Crosscutting.AttachedPermission;
using RadialReview.Utilities;

namespace RadialReview.Utilities.Serializers
{
    public class AttachedPermissionSerializer : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return typeof(IAttachedPermission).IsAssignableFrom(objectType);            
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            if (value is IAttachedPermission)
            {
                using (var s = HibernateSession.GetCurrentSession())
                {
                    using (var tx = s.BeginTransaction())
                    {
                        //var perms = PermissionsUtility.Create(s, caller);
                        //PermissionRegistry.AttachPermission(s, perms, (IAttachedPermission)value);
                        //tx.Commit();
                        //s.Flush();
                    }
                }
                serializer.Serialize(writer, (IAttachedPermission)value);
                return;
            }
            //throw new NotImplementedException();
        }
        public override bool CanRead => false; 
    }
}