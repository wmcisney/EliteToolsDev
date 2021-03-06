﻿using FluentNHibernate.Mapping;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RadialReview.Areas.People.Engines.Surveys.Interfaces;
using RadialReview.Models.Interfaces;
using RadialReview.Utilities;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Web;
using System.Web.Script.Serialization;

namespace RadialReview.Areas.People.Models.Survey {
    public class KV {
        public string Key { get; set; }
        public object Value { get; set; }
        public KV(string key, object value) {
            Key = key;
            Value = value;
        }
        public KeyValuePair<string, object> ToKeyValuePair() {
            return new KeyValuePair<string, object>(Key, Value);
        }
    }

    public class SurveyItemFormat : ILongIdentifiable, IHistorical, IItemFormat {
        public virtual long Id { get; set; }
        public virtual DateTime CreateTime { get; set; }
        public virtual DateTime? DeleteTime { get; set; }
        public virtual long SurveyContainerId { get; set; }
        public virtual long OrgId { get; set; }
        public virtual String Settings { get; set; }
		public virtual SurveyItemType ItemType { get; set; }
		public virtual SurveyQuestionIdentifier QuestionIdentifier { get; set; }

		//#region Do not use
		//[Obsolete("Do not use. Not Saved.")]
		//public virtual string Name { get; set; }
		//[Obsolete("Do not use. Not Saved.")]
		//public virtual string Help { get; set; }
		//[Obsolete("Do not use. Not Saved.")]
		//public virtual int Ordering { get; set; }
		//[Obsolete("For SurveyEngine. Not Saved.")]
		//public virtual bool ShouldInitialize { get; set; }
		//#endregion

		#region Static Initializers
		public static SurveyItemFormat GenerateRadio(IItemFormatInitializerCtx ctx,SurveyQuestionIdentifier questionIdentifier, IDictionary<string, string> options, params KV[] settings) {
            var format = new SurveyItemFormat(ctx, questionIdentifier, SurveyItemType.Radio, settings);
            format.AddSetting("options", options);
            return format;
        }
        public static SurveyItemFormat GenerateText(IItemFormatInitializerCtx ctx, SurveyQuestionIdentifier questionIdentifier, params KV[] settings) {
            var format = new SurveyItemFormat(ctx, questionIdentifier, SurveyItemType.Text, settings);
            return format;
        }
        #endregion

        #region Constructors
        public SurveyItemFormat(IItemFormatInitializerCtx ctx, SurveyQuestionIdentifier questionIdentifier, SurveyItemType type, params KV[] settings) : this(ctx, questionIdentifier, type, settings.ToList()) {
        }

#pragma warning disable CS0618 // Type or member is obsolete
		public SurveyItemFormat(IItemFormatInitializerCtx ctx, SurveyQuestionIdentifier questionIdentifier, SurveyItemType type, IEnumerable<KV> settings) : this() {
#pragma warning restore CS0618 // Type or member is obsolete
			ItemType = type;
            foreach (var kv in settings) {
                var key = kv.Key;
                var value = kv.Value;
                if (value != null) {
                    AddSetting(key, value);
                }
            }
			QuestionIdentifier = questionIdentifier;
            SurveyContainerId = ctx.SurveyContainer.Id;
            OrgId = ctx.OrgId;
			CreateTime = ctx.Now;
		}

        private static int CtorCalls = 0;
        [Obsolete("Use other constructor.")]
        public SurveyItemFormat() {
            CtorCalls += 1;
            CreateTime = DateTime.UtcNow;
            Settings = "{}";
        }
        #endregion

        public virtual IItemFormat AddSetting(string key, object value) {
            var serializer = new JavaScriptSerializer();
            var dict = serializer.Deserialize<Dictionary<string, object>>(Settings);
            dict[key] = value;
            Settings = serializer.Serialize(dict);
            return this;
        }
        public virtual T GetSetting<T>(string key) {
            var serializer = new JavaScriptSerializer();
            try {
                var dict = serializer.Deserialize<Dictionary<string, object>>(Settings);
                if (dict.ContainsKey(key) && dict[key] is T) {
                    return (T)dict[key];
                }
                return default(T);
            } catch (Exception) {
                return default(T);
            }
        }

        private Guid guid = Guid.NewGuid();
        public virtual string ToPrettyString() {
            return "Format: [Id:" + Id + ", Guid:" + guid + "] ( Type:" + ItemType + ",  Settings:" + Settings + "  )";
        }

        public virtual SurveyItemType GetItemType() {
            return ItemType;
        }
        public virtual string GetName() {
            return null;
        }
        public virtual string GetHelp() {
            return null;
        }
        public virtual int GetOrdering() {
            return 0;
        }

        public virtual IDictionary<string, object> GetSettings() {
            var serializer = new JavaScriptSerializer();
            var dict = serializer.Deserialize<Dictionary<string, object>>(Settings);
            return dict;
        }

		public virtual SurveyQuestionIdentifier GetQuestionIdentifier() {
			return QuestionIdentifier;
		}

		public class Map : ClassMap<SurveyItemFormat> {
            public Map() {
                Id(x => x.Id);
                Map(x => x.CreateTime);
                Map(x => x.DeleteTime);
                Map(x => x.SurveyContainerId);
                Map(x => x.OrgId);
                Map(x => x.Settings).Length(8000);
                Map(x => x.ItemType);
				Map(x => x.QuestionIdentifier);

			}
        }
    }
}