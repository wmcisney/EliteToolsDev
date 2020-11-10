using FluentNHibernate.Mapping;
using RadialReview.Models.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace RadialReview.Models.Payments {
	public class InvoiceUserItemModel : ILongIdentifiable, IHistorical {
		public virtual long Id { get; set; }
		public virtual long OrgId { get; set; }
		public virtual long InvoiceId { get; set; }
		public virtual long UserOrganizationId { get; set; }

		public virtual string Email { get; set; }
		public virtual string Name { get; set; }
		public virtual string Description { get; set; }
		public virtual DateTime? UserAttachTime { get; set; }

		public virtual DateTime CreateTime { get; set; }
		public virtual DateTime? DeleteTime { get; set; }

		public InvoiceUserItemModel() {
			CreateTime = DateTime.UtcNow;
		}

		public class Map : ClassMap<InvoiceUserItemModel> {
			public Map() {
				Id(x => x.Id);
				Map(x => x.OrgId);
				Map(x => x.InvoiceId);
				Map(x => x.CreateTime);
				Map(x => x.DeleteTime);

				Map(x => x.UserOrganizationId);
				Map(x => x.Name);
				Map(x => x.Email);
				Map(x => x.UserAttachTime);
				Map(x => x.Description);
			}
		}
	}
}