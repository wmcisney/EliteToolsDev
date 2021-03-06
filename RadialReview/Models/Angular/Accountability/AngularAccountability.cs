﻿using RadialReview.Models.Accountability;
using RadialReview.Models.Angular.Base;
using RadialReview.Models.Angular.Positions;
using RadialReview.Models.Angular.Roles;
using RadialReview.Models.Angular.Users;
using RadialReview.Models.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Web;

namespace RadialReview.Models.Angular.Accountability {

	public class AngularAccountabilityChartSearch : BaseAngular {
		public AngularAccountabilityChartSearch(long id) : base(id) {			
		}
		public Dictionary<long, string> searchPos { get; set; }
	}

	public class AngularAccountabilityChart : BaseAngular {

#pragma warning disable CS0618 // Type or member is obsolete
		public AngularAccountabilityChart() {
		}
#pragma warning restore CS0618 // Type or member is obsolete
		public AngularAccountabilityChart(long id) : base(id) {
			search = new AngularAccountabilityChartSearch(id);
		}
		public AngularAccountabilityNode Root { get; set; }
		public IEnumerable<AngularUser> AllUsers { get; set; }
		public long? CenterNode { get; set; }
		public long? ShowNode { get; set; }
		public long? ExpandNode { get; set; }
		public bool? CanReorganize { get; set; }
		public AngularAccountabilityChartSearch search { get; set; }

		public void Dive(Action<AngularAccountabilityNode> action) {
			Dive(action, Root);
		}
		protected void Dive(Action<AngularAccountabilityNode> action, AngularAccountabilityNode node) {
			if (node != null) {
				action(node);

				var children = node.GetDirectChildren();
				if (children != null) {
					foreach (var c in children) {
						Dive(action, c);
					}
				}
			}
		}
	}
	[SwaggerName(Name = "Seat")]
	public class AngularAccountabilityNode : AngularTreeNode<AngularAccountabilityNode> {
		public AngularAccountabilityNode() {
		}
		public AngularAccountabilityNode(long id) : base(id) {
		}

		//public bool? Editable { get; set; }

		public AngularAccountabilityNode(AccountabilityNode node, bool collapse = false, bool? editable = null) : base(node.Id) {
			User = node.User.NotNull(x => AngularUser.CreateUser(node.User));
			Editable = editable ?? node._Editable;
			Group = node.AccountabilityRolesGroup.NotNull(x => new AngularAccountabilityGroup(x, editable: x._Editable ?? Editable));

			var childrens = node._Children.NotNull(x => x.Select(y =>
				new AngularAccountabilityNode(y, editable: y._Editable ?? Editable)
			).ToList());

			order = node.Ordering;

			__children = childrens;
			collapsed = collapse;
			Name = node._Name ?? User.NotNull(x => x.Name);

			//         if (collapse)
			//             _children = childrens;
			//         else
			//             children = childrens;

		}


		public string Name { get; set; }
		public AngularAccountabilityGroup Group { get; set; }
		public bool? Highlight;


		private AngularUser _User;
		public AngularUser User {
			get {
				if (_User != null && _User.Id == AngularUser.NoUser().Id)
					return null;
				return _User;
			}
			set { _User = value; }
		}



		public bool HasChildren() {
			return __children != null && __children.Any();
		}

		[IgnoreDataMember]
		public bool? _hasParent;
		[IgnoreDataMember]
		public bool _hidePdf;

	}
	public class AngularAccountabilityGroup : BaseAngular {
#pragma warning disable CS0618 // Type or member is obsolete
		public AngularAccountabilityGroup() {
		}
#pragma warning restore CS0618 // Type or member is obsolete
		public AngularAccountabilityGroup(long id) : base(id) {
		}
		public AngularAccountabilityGroup(AccountabilityRolesGroup group, bool? editable = null) : base(group.Id) {
			Editable = editable ?? group._Editable;
			RoleGroups = group._Roles.NotNull(x =>
				x.Select(y => new AngularRoleGroup(
					new Attach(y.AttachType, y.AttachId, y.AttachName),
					y.Roles.Select(z => new AngularRole(z)).ToList(),
					editable: Editable
				)).ToList());
			Position = group.Position.NotNull(x => new AngularPosition(x));
		}

		public AngularPosition Position { get; set; }
		public bool? Editable { get; set; }
		public IEnumerable<AngularRoleGroup> RoleGroups { get; set; }


	}
}