﻿using System;
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

namespace RadialReview.Utilities.Serializers {
	/**
     *  add "&transform=true" to url to see untransformed.
     * 
     * *///
	public class AngularSerialization : JsonConverter {
		//public JsonConverter Backing { get; set; }

		public bool RemoveExtraProperties { get; set; }

		public AngularSerialization(bool removeExtraProperties =false) {
			RemoveExtraProperties = removeExtraProperties;
		}

		//public override 

		public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer) {
			if (value is IAngular) {
				serializer.Serialize(writer, AngularSerializer.Serialize((IAngular)value));
				return;
			}
			throw new Exception();
			//serializer.Serialize(writer, AngularSerializer.Serialize((IAngular)value));
		}

		public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer) {
			throw new Exception();
		}

		public override bool CanRead {
			get { return false; }
		}

		public override bool CanConvert(Type objectType) {
			return (typeof(IAngular).IsAssignableFrom(objectType));
		}
	}

	public class AngularSerializer {
		public static Dictionary<string, object> Serialize(IAngular item) {
			var lookup = new Dictionary<string, object>();
			var output = new Dictionary<string, object>();
			_Serialize(item, output, lookup, DateTime.UtcNow);

			if (output.ContainsKey("Lookup"))
				output["Lookup"] = Merge((IDictionary)output["Lookup"], lookup);
			else
				output["Lookup"] = lookup;

			return output;
		}

		private static void _Serialize(object item, Dictionary<string, object> parent, Dictionary<string, object> lookup, DateTime now) {
			/*if (item.GetType().GetInterfaces().Any(x =>x.IsGenericType &&x.GetGenericTypeDefinition() == typeof(IAngularizer<>))){
				//var generic = item.GetType().GetInterface("IAngularizer`1").GetGenericArguments()[0];
				//dynamic converted= Convert.ChangeType(item, generic);
				dynamic converted = CastEntity(item);
				var angularizer= Angularizer.Create(converted);
				
				converted.Angularize(angularizer);
				foreach (var key in angularizer.ToSerialize.Keys){
					var output = new Dictionary<string, object>();
					parent[key] = _SerializeProperty(key, angularizer.ToSerialize[key], parent, lookup, now);
				}
			}else{*/
			var properties = GetProperties(item);
			foreach (var p in properties) {
				var name = p.Name;
				var value = p.GetValue(item, null);
				if (name == "_ExtraProperties" && value != null) {
					var dict = (Dictionary<string, object>)value;
					foreach (var k in dict.Keys) {
						var serialized = _SerializeProperty(k, dict[k], parent, lookup, now, item);
						if (serialized != null) {
							parent[k] = serialized;
						}
					}
				} else {
					var serialized = _SerializeProperty(name, value, parent, lookup, now, item);
					if (serialized != null) {
						parent[name] = serialized;
					}
				}
			}
			//}
		}
		protected static T CastEntity<T>(T entity) {
			var proxy = entity as INHibernateProxy;
			if (proxy != null) {
				return (T)proxy.HibernateLazyInitializer.GetImplementation();
			} else {
				return (T)entity;
			}
		}

		private static object _SerializeProperty(string name, object value, Dictionary<string, object> parent, Dictionary<string, object> lookup, DateTime now, object owner) {
			/*if (value != null && value.GetType().GetInterfaces().Any(x => x.IsGenericType && x.GetGenericTypeDefinition() == typeof (IAngularizer<>))){
				var output = new Dictionary<string, object>();
				_Serialize(value, output, lookup, now);
				return output;
			}
			else*/
			if (value is DateTime && (DateTime)value == Removed.Date()) {
				parent[name] = Removed.DELETED_KEY;
				return parent[name];
			}
			if (value is Decimal && (Decimal)value == Removed.Decimal()) {
				parent[name] = Removed.DELETED_KEY;
				return parent[name];
			}
			if (value is IAngularIgnore) {
				return null;
			}

            if (value is Enum && GetAttributeFrom<JsonConverterAttribute>(owner, name).NotNull(x => x.ConverterType) == typeof(StringEnumConverter)) {
                return value + "";
            }

			if (value is IAngularId) {
				var sub = new Dictionary<string, object>();
				var resolved = (IAngularId)value;
				_Serialize(value, sub, lookup, now);
				Merge(lookup, resolved, sub); //lookup[resolved.GetKey()] = sub;
				var output = new AngularPointer(resolved, now/*, false*/);
				if (value is Removed || resolved.GetAngularId() as long? == Removed.Long() || resolved.GetAngularId() as string == Removed.String() ) {
					parent[name] = Removed.DELETED_KEY;
					return parent[name];
				}
				return output;
			} else if (value is IEnumerable && GenericImplementsType((IEnumerable)value, typeof(IAngularId))) {
				var keyList = new List<AngularPointer>();
				var resolved = value as IEnumerable;

				foreach (var v in resolved) {
					var sub = new Dictionary<string, object>();
					var vResolved = (IAngularId)v;
					_Serialize(v, sub, lookup, now);
					Merge(lookup, vResolved, sub); //lookup[vResolved.GetKey()] = sub;
					keyList.Add(new AngularPointer(vResolved, now/*, false*/));
				}

				if (value is IAngularList) {
					return new { UpdateMethod = ((IAngularList)value).UpdateMethod.ToString(), AngularList = keyList };
				}
				return keyList;
			} else if (value is IDictionary && GenericImplementsValueType((IDictionary)value, typeof(IAngularId))) {
				var keyList = new Dictionary<string, object>();
				var resolved = value as IDictionary;
				foreach (var vKey in resolved.Keys) {
					var v = resolved[vKey];
					var sub = new Dictionary<string, object>();
					var vResolved = (IAngularId)v;
					_Serialize(v, sub, lookup, now);
					Merge(lookup, vResolved, sub); //lookup[vResolved.GetKey()] = sub;
					keyList.Add(vResolved.GetKey(), new AngularPointer(vResolved, now/*, false*/));
				}
				if (!parent.ContainsKey(name))
					parent[name] = keyList;
				else if (parent[name] == null || parent[name] is IDictionary)
					return Merge(parent[name] as IDictionary, keyList);
				else
					throw new Exception("Property already exists and is not a dictionary: " + name);
			} else if (value is Enum || (value != null && Nullable.GetUnderlyingType(value.GetType()) != null && Nullable.GetUnderlyingType(value.GetType()).IsEnum)) {
				return value.ToString();
			}

			//Well nothing to convert
			return value;
		}

		private static void CopyValues<T>(T target, T source) {
			var t = typeof(T);

			var properties = t.GetProperties().Where(prop => prop.CanRead && prop.CanWrite);

			foreach (var prop in properties) {
				var value = prop.GetValue(source, null);
				if (value != null)
					prop.SetValue(target, value, null);
			}
		}
		private static void Merge(Dictionary<string, object> lookup, IAngularId key, Dictionary<string, object> value) {
			var keyStr = key.GetKey();
			if (lookup.ContainsKey(keyStr)) {
				var old = lookup[keyStr];
				if (old == null) {
					lookup[keyStr] = value;
				} else if (value == null) {
					lookup[keyStr] = old;
				} else if (lookup[keyStr] is IDictionary) {
					var existing = lookup[keyStr] as IDictionary;
					lookup[keyStr] = Merge(existing, value);
				}
			} else {
				lookup[keyStr] = value;
			}
		}

		private static Dictionary<string, object> Merge(IDictionary first, Dictionary<string, object> second) {
			var existing = first;
			if (first != null && second != null) {
				var newDict = new Dictionary<string, object>();
				foreach (var k in existing.Keys)
					newDict[(string)k] = existing[k];
				foreach (var k in second.Keys)
					if (second[k] != null)
						newDict[k] = second[k];
				return newDict;
			}
			if (first == null && second != null)
				return second.ToDictionary(x => x.Key, x => x.Value);
			if (first != null && second == null) {
				var newDict = new Dictionary<string, object>();
				foreach (var k in first.Keys) {
					newDict[(string)k] = (object)first[k];
				}
				return newDict;
			}
			return null;
		}

		private static bool GenericImplementsType(IEnumerable objects, Type baseType) {
			foreach (Type type in objects.GetType().GetInterfaces()) {
				if (type.IsGenericType) {
					if (type.GetGenericTypeDefinition() == typeof(IEnumerable<>)) {
						if (baseType.IsAssignableFrom(type.GetGenericArguments()[0]))
							return true;
					}
				}
			}
			return false;
		}
		private static bool GenericImplementsValueType(IDictionary objects, Type baseType) {
			foreach (Type type in objects.GetType().GetInterfaces()) {
				if (type.IsGenericType) {
					if (type.GetGenericTypeDefinition() == typeof(IDictionary<,>)) {
						if (baseType.IsAssignableFrom(type.GetGenericArguments()[1]))
							return true;
					}
				}
			}
			return false;
		}

        private static T GetAttributeFrom<T>(object instance, string propertyName) where T : Attribute {
            var attrType = typeof(T);
            var property = instance.GetType().GetProperty(propertyName);
            return (T)property.GetCustomAttributes(attrType, false).FirstOrDefault();
        }

        private static IEnumerable<PropertyInfo> GetProperties(object obj) {
			return obj.GetType().GetProperties();
		}


		private readonly Assembly _assembly;
		private readonly IContractResolver _camelCaseContractResolver;
		private readonly IContractResolver _defaultContractSerializer;


	}
}