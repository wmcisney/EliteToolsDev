﻿using RadialReview.Areas.People.Engines.Surveys.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using RadialReview.Models.Interfaces;
using RadialReview.Accessors;
using RadialReview.Models.Askables;
using RadialReview.Areas.People.Models.Survey;
using RadialReview.Models.Components;
using RadialReview.Models.Enums;
using RadialReview.Utilities.DataTypes;

namespace RadialReview.Areas.People.Engines.Surveys.Impl.QuarterlyConversation.Sections {
    public class ValueSection : ISectionInitializer {

		public static string ValueCommentHeading = "Core Values Comments";

		public IEnumerable<IItemInitializer> GetAllPossibleItemBuilders(IEnumerable<IByAbout> byAbouts) {
#pragma warning disable CS0618 // Type or member is obsolete
			yield return new ValueItem();
#pragma warning restore CS0618 // Type or member is obsolete
		}

        public IEnumerable<IItemInitializer> GetItemBuilders(IItemInitializerData data) {

            var dict = data.Lookup.GetOrAdd("ValueSectionAlreadyGenerated", (_str) => new DefaultDictionary<string, bool>(x=>false));
            var byAboutKey = data.By.ToKey() + "-" + data.About.ToKey();
            if (data.About.Is<SurveyUserNode>()) {
                byAboutKey = data.By.ToKey() + "-" + ((SurveyUserNode)data.About).User.ToKey();
            }
            var alreadyGenerated =  dict[byAboutKey];

            if (!alreadyGenerated) {
                dict[byAboutKey] =true;
                //only ask if they are not our manager
                if (data.SurveyContainer.GetSurveyType() == SurveyType.QuarterlyConversation && data.About.Is<SurveyUserNode>()) {

                    if (data.SurveyContainer.GetCreator().ToKey() == ((SurveyUserNode)data.About).User.ToKey())
                        return new List<IItemInitializer>();

                    if ((data.About as SurveyUserNode)._Relationship[data.By.ToKey()] == AboutType.Manager)
                        return new List<IItemInitializer>();
                }

                var values = data.Lookup.GetList<CompanyValueModel>();
                var genComments = new InputItemIntializer(ValueCommentHeading, SurveyQuestionIdentifier.GeneralComment);
                var items = values.Select(x => (IItemInitializer)new ValueItem(x)).ToList();
                items.Add(genComments);
                return items;
            }
            return new IItemInitializer[] { };
        }

        public ISection InitializeSection(ISectionInitializerData data) {
            return new SurveySection(data, "Core Values", SurveySectionType.Values,"mk-values");
        }

        public void Prelookup(IInitializerLookupData data) {
            data.Lookup.AddList(OrganizationAccessor.GetCompanyValues_Unsafe(data.Session, data.OrgId));
        }
    }

    public class ValueItem : IItemInitializer {
        private CompanyValueModel CompanyValue;

        [Obsolete("Use other constructor")]
        public ValueItem() {
        }

        public ValueItem(CompanyValueModel value) {
            CompanyValue = value;
        }

        public IItemFormatRegistry GetItemFormat(IItemFormatInitializerCtx ctx) {
            var options = new Dictionary<string, string> {
				{"often"    ,"Most of the time they live this value"          },
                {"sometimes","Some of the time they live this value"          },
				{"not-often","Most of the time they do not live this value"   },
			};
            return ctx.RegistrationItemFormat(true, () => SurveyItemFormat.GenerateRadio(ctx, SurveyQuestionIdentifier.Value, options));
        }

        public bool HasResponse(IResponseInitializerCtx data) {
            return true;
        }

        public IItem InitializeItem(IItemInitializerData data) {
			var forModel = ForModel.Create(CompanyValue);
			return new SurveyItem(data, CompanyValue.CompanyValue, forModel,forModel.ToKey()) {
                Help = CompanyValue.CompanyValueDetails
            };
        }

        public IResponse InitializeResponse(IResponseInitializerCtx ctx, IItemFormat format) {
            return new SurveyResponse(ctx, format);
        }

        public void Prelookup(IInitializerLookupData data) {
            //not needed
        }
    }
}